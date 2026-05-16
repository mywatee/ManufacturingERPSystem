using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public class QualityControlService : IQualityControlService
{
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;
    private readonly IWarehouseService _warehouseService;
    private readonly IMasterDataService _masterDataService;

    public QualityControlService(
        IDbContextFactory<ManufacturingContext> contextFactory,
        IWarehouseService warehouseService,
        IMasterDataService masterDataService)
    {
        _contextFactory = contextFactory;
        _warehouseService = warehouseService;
        _masterDataService = masterDataService;
    }

    public async Task<List<QualityControl>> GetRecentRecordsAsync(int count)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.QualityControls
            .Include(q => q.Wo)
                .ThenInclude(w => w.WorkOrderItems)
                    .ThenInclude(i => i.Product)
            .Include(q => q.Inspector).ThenInclude(u => u.Employee)
            .OrderByDescending(q => q.InspectionDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<bool> AddRecordAsync(QualityControl record)
    {
        try
        {
            var context = await _contextFactory.CreateDbContextAsync();
            
            // VALIDATION: Kiểm tra nghiêm ngặt - Số lượng QC không được vượt quá số lượng đã báo cáo sản xuất
            if (record.Woid.HasValue)
            {
                var order = await context.WorkOrders
                    .Include(w => w.WorkOrderProgresses)
                    .Include(w => w.QualityControls)
                    .FirstOrDefaultAsync(w => w.Woid == record.Woid);

                if (order != null)
                {
                    // Lấy số lượng đã báo cáo ở các công đoạn sản xuất (loại trừ QC)
                    // Chúng ta lấy Max của ProducedQty ở các công đoạn để biết có bao nhiêu sản phẩm đã đi qua xưởng
                    var stages = order.WorkOrderProgresses?
                        .Where(p => p.StageName != "Kiểm tra chất lượng (QC)" && p.StageName != "CẢNH BÁO HỆ THỐNG")
                        .GroupBy(p => p.StageName)
                        .Select(g => g.Sum(p => p.ProducedQty ?? 0))
                        .ToList();

                    int totalProduced = stages != null && stages.Any() ? stages.Max() : 0;
                        
                    int totalInspected = order.QualityControls?.Sum(qc => (qc.PassedQty ?? 0) + (qc.FailedQty ?? 0)) ?? 0;
                    int incomingQC = (record.PassedQty ?? 0) + (record.FailedQty ?? 0);

                    if (totalInspected + incomingQC > totalProduced)
                    {
                        throw new InvalidOperationException($"Không thể kiểm tra {incomingQC} mẫu. Hiện tại xưởng mới chỉ báo cáo hoàn thành xong {totalProduced} sản phẩm (Đã kiểm tra: {totalInspected}). Vui lòng cập nhật tiến độ sản xuất trước.");
                    }
                }
            }

            record.InspectionDate = DateTime.Now;
            context.QualityControls.Add(record);
            await context.SaveChangesAsync();

            // XỬ LÝ ĐỒNG BỘ VỀ LỆNH SẢN XUẤT (WorkOrder)
            if (record.Woid.HasValue)
            {
                var order = await context.WorkOrders
                    .Include(w => w.WorkOrderItems)
                    .FirstOrDefaultAsync(w => w.Woid == record.Woid);

                if (order != null)
                {
                    // 1. Cập nhật số lượng đạt chuẩn (ActualQty) vào header của lệnh
                    order.ActualQty = (order.ActualQty ?? 0) + (record.PassedQty ?? 0);
                    
                    // 2. Cập nhật chi tiết từng sản phẩm trong lệnh và ghi nhận tiến độ
                    var targetItem = order.WorkOrderItems.FirstOrDefault(i => i.ItemId == record.WorkOrderItemId) 
                                   ?? order.WorkOrderItems.FirstOrDefault();

                    if (targetItem != null)
                    {
                        targetItem.ActualQty += (record.PassedQty ?? 0);

                        // Thêm bản ghi tiến độ để hệ thống tự động tính toán FailedQty
                        context.WorkOrderProgresses.Add(new WorkOrderProgress
                        {
                            Woid = order.Woid,
                            WorkOrderItemId = targetItem.ItemId,
                            ProducedQty = record.PassedQty,
                            DefectQty = record.FailedQty,
                            StageName = "Kiểm tra chất lượng (QC)",
                            RecordedBy = "Hệ thống QC",
                            EndTime = DateTime.Now,
                            Notes = record.DefectReason ?? "Kiểm tra định kỳ"
                        });
                    }

                }
            }


            // XỬ LÝ KHO: Tự động nhập kho hàng ĐẠT QC
            if (record.PassedQty > 0 && record.Woid.HasValue)
            {
                var targetItem = await context.WorkOrderItems
                    .Include(i => i.WorkOrder)
                    .FirstOrDefaultAsync(i => i.ItemId == record.WorkOrderItemId);
                
                if (targetItem != null && targetItem.ProductId.HasValue)
                {
                    var warehouses = await _warehouseService.GetWarehousesAsync();
                    // Ưu tiên chọn kho có tên "Thành phẩm" hoặc "Kho tổng", nếu không lấy kho đầu tiên
                    var targetWh = warehouses.FirstOrDefault(w => w.Name.Contains("Thành phẩm") || w.Name.Contains("Thành Phẩm")) 
                                ?? warehouses.FirstOrDefault();

                    if (targetWh != null)
                    {
                        await _warehouseService.AddStockTransactionAsync(new StockTransaction
                        {
                            MaterialId = targetItem.ProductId,
                            WarehouseId = int.Parse(targetWh.Id),
                            Quantity = (decimal)record.PassedQty.Value,

                            Type = "Nhập kho",
                            ReferenceCode = $"QC-PASS-{targetItem.WorkOrder?.Wocode ?? "N/A"}",
                            TransDate = DateTime.Now,
                            TransBy = record.InspectorId
                        });
                    }
                }
            }


            // D - CẢNH BÁO TỶ LỆ LỖI VÀ TỰ ĐỘNG TẠM DỪNG (Threshold: 10%)
            double batchTotal = (record.PassedQty ?? 0) + (record.FailedQty ?? 0);
            if (batchTotal > 0)
            {
                double defectRate = (double)(record.FailedQty ?? 0) / batchTotal;
                if (defectRate > 0.1) // Ngưỡng 10%
                {
                    var order = await context.WorkOrders.FirstOrDefaultAsync(w => w.Woid == record.Woid);
                    if (order != null && (order.Status == "Running" || order.Status == "Đang sản xuất"))
                    {
                        order.Status = "Paused";
                        
                        // Ghi log lý do tạm dừng vào tiến độ
                        context.WorkOrderProgresses.Add(new WorkOrderProgress
                        {
                            Woid = order.Woid,
                            StageName = "CẢNH BÁO HỆ THỐNG",
                            Status = "Tạm dừng",
                            RecordedBy = "QC Auto-Guard",
                            EndTime = DateTime.Now,
                            Notes = $"Tự động tạm dừng do tỷ lệ lỗi lô hàng ({defectRate:P1}) vượt ngưỡng an toàn (10%)"
                        });
                    }
                }
            }

            await context.SaveChangesAsync();

            return true;

        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<List<WorkOrder>> GetWorkOrdersAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // Lấy danh sách các lệnh có trạng thái phù hợp
        var validStatuses = new[] { "Running", "Đang sản xuất", "Completed", "Hoàn thành", "Paused", "Tạm dừng" };
        
        var allOrders = await context.WorkOrders
            .Include(w => w.WorkOrderItems)
            .Include(w => w.QualityControls)
            .Include(w => w.WorkOrderProgresses)
            .Where(w => validStatuses.Contains(w.Status))
            .ToListAsync();


        // ẨN THÔNG MINH: Lọc bỏ những lệnh đã kiểm tra đủ sản lượng mục tiêu
        var filteredOrders = allOrders.Where(w => {
            int targetQty = w.TargetQty ?? 0;
            int inspectedQty = w.QualityControls?.Sum(qc => (qc.PassedQty ?? 0) + (qc.FailedQty ?? 0)) ?? 0;
            
            return inspectedQty < targetQty;
        }).ToList();

        return filteredOrders;
    }

    public async Task<List<WorkOrder>> GetPendingInspectionOrdersAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // Lấy các lệnh sản xuất đang chạy hoặc đã hoàn thành
        var validStatuses = new[] { "Running", "Đang sản xuất", "Completed", "Hoàn thành", "Paused", "Tạm dừng" };
        var allOrders = await context.WorkOrders
            .Include(w => w.WorkOrderItems)
                .ThenInclude(i => i.Product)
            .Include(w => w.QualityControls)
            .Include(w => w.WorkOrderProgresses)
            .Where(w => validStatuses.Contains(w.Status))
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();



        // ẨN THÔNG MINH: 
        // 1. Luôn hiện các lệnh "Đang sản xuất" để có thể kiểm tra liên tục
        // 2. Các lệnh khác (Hoàn thành, Tạm dừng) chỉ hiện nếu chưa kiểm tra đủ số lượng
        return allOrders.Where(w => {
            if (w.Status == "Running" || w.Status == "Đang sản xuất") return true;
            
            int targetQty = w.TargetQty ?? 0;
            int inspectedQty = w.QualityControls?.Sum(qc => (qc.PassedQty ?? 0) + (qc.FailedQty ?? 0)) ?? 0;
            return inspectedQty < targetQty || targetQty == 0;
        }).ToList();

    }


    public async Task<List<TrendPoint>> GetDefectTrendAsync(string period, int count)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        DateTime startDate;
        
        // Căn chỉnh ngày bắt đầu dựa trên chu kỳ
        if (period == "Ngày") startDate = DateTime.Now.Date.AddDays(-count + 1);
        else if (period == "Tháng") startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-count + 1);
        else // Tuần
        {
            // Tìm Thứ 2 gần nhất
            int diff = (7 + (DateTime.Now.DayOfWeek - DayOfWeek.Monday)) % 7;
            startDate = DateTime.Now.Date.AddDays(-diff).AddDays(-(count - 1) * 7);
        }

        var records = await context.QualityControls
            .Where(q => q.InspectionDate >= startDate)
            .ToListAsync();

        var result = new List<TrendPoint>();

        for (int i = 0; i < count; i++)
        {
            DateTime pStart, pEnd;
            string label;

            if (period == "Ngày")
            {
                pStart = startDate.AddDays(i);
                pEnd = pStart.AddDays(1);
                
                string dayName = pStart.DayOfWeek switch {
                    DayOfWeek.Monday => "T2",
                    DayOfWeek.Tuesday => "T3",
                    DayOfWeek.Wednesday => "T4",
                    DayOfWeek.Thursday => "T5",
                    DayOfWeek.Friday => "T6",
                    DayOfWeek.Saturday => "T7",
                    DayOfWeek.Sunday => "CN",
                    _ => ""
                };
                label = $"{dayName} {pStart:dd/MM}";
            }
            else if (period == "Tháng")
            {
                pStart = startDate.AddMonths(i);
                pEnd = pStart.AddMonths(1);
                label = $"Th.{pStart:MM/yy}";
            }
            else // Tuần
            {
                pStart = startDate.AddDays(i * 7);
                pEnd = pStart.AddDays(7);
                
                // Lấy số tuần trong năm (Sử dụng ISOWeek nếu có, hoặc tính thủ công)
                int weekNum = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                    pStart, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                
                label = $"T{weekNum} ({pStart:dd/MM})";
            }

            var pRecords = records.Where(r => r.InspectionDate >= pStart && r.InspectionDate < pEnd).ToList();
            int total = pRecords.Sum(r => (r.PassedQty ?? 0) + (r.FailedQty ?? 0));
            int failed = pRecords.Sum(r => r.FailedQty ?? 0);
            double rate = total > 0 ? Math.Round((double)failed / total * 100, 1) : 0;

            result.Add(new TrendPoint {
                Label = label,
                DefectRate = rate,
                TotalSamples = total,
                FailedSamples = failed
            });
        }

        return result;
    }


    public async Task<(int Total, int Passed, int Failed, List<(string Category, int Count)> DefectStats)> GetStatisticsAsync(DateTime startDate, DateTime endDate)

    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // VÁ DỮ LIỆU TỰ ĐỘNG: Gắn các bản ghi QC chưa có WorkOrderItemId vào sản phẩm đầu tiên của lệnh
        var orphanedRecords = await context.QualityControls
            .Where(q => q.WorkOrderItemId == null && q.Woid != null)
            .ToListAsync();
        
        if (orphanedRecords.Any())
        {
            foreach (var rec in orphanedRecords)
            {
                var firstItem = await context.WorkOrderItems.FirstOrDefaultAsync(i => i.WorkOrderId == rec.Woid);
                if (firstItem != null) rec.WorkOrderItemId = firstItem.ItemId;
            }
            await context.SaveChangesAsync();
        }

        var records = await context.QualityControls
            .Where(q => q.InspectionDate >= startDate && q.InspectionDate <= endDate)
            .ToListAsync();

        int total = records.Sum(q => (q.PassedQty ?? 0) + (q.FailedQty ?? 0));
        int passed = records.Sum(q => q.PassedQty ?? 0);
        int failed = records.Sum(q => q.FailedQty ?? 0);

        // Giả lập phân loại lỗi từ DefectReason (Trong thực tế nên có bảng DefectCategories)
        var defectStats = records
            .Where(q => !string.IsNullOrEmpty(q.DefectReason))
            .GroupBy(q => q.DefectReason!)
            .Select(g => (Category: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        return (total, passed, failed, defectStats);
    }
}
