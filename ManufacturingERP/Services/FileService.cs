using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ManufacturingERP.Services;

public class FileService : IFileService
{
    static FileService()
    {
        // QuestPDF License setup
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<bool> ExportToExcelAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Sheet1", IEnumerable<string> columns = null, string reportTitle = "BÁO CÁO CHI TIẾT")
    {
        try
        {
            return await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(sheetName);

                var list = data.ToList();
                if (!list.Any()) return false;

                // Define Headers Mapping (Match UI names exactly)
                var headers = new Dictionary<string, string>
                {
                    { "Code", "Mã lệnh sản xuất" },
                    { "Id", "Mã lệnh sản xuất" },
                    { "Product", "Tên sản phẩm" },
                    { "TargetQty", "Số lượng mục tiêu" },
                    { "Progress", "Tiến độ thực tế" },
                    { "Status", "Trạng thái hiện tại" },
                    { "IsUrgent", "Cấp bách" },
                    { "StartDate", "Ngày bắt đầu" },
                    { "Deadline", "Hạn hoàn thành" },
                    { "EndDate", "Hạn hoàn thành" },
                    { "CreatedAt", "Thời gian tạo" },
                    { "CreatedBy", "Người tạo" },
                    // Activity Log Mapping
                    { "Content", "Nội dung chi tiết" },
                    { "User", "Người thực hiện" },
                    { "Time", "Thời gian" },
                    // Material Mapping
                    { "MaterialCode", "Mã vật tư" },
                    { "MaterialName", "Tên vật tư" },
                    { "Category", "Phân loại" },
                    { "Unit", "Đơn vị" },
                    // BOM Mapping
                    { "ParentCode", "Mã sản phẩm chính" },
                    { "ParentName", "Tên sản phẩm chính" },
                    { "ChildCode", "Mã linh kiện" },
                    { "ChildName", "Tên linh kiện" },
                    { "BomQuantity", "Số lượng định mức" },
                    // Routing Mapping
                    { "ProductCode", "Mã sản phẩm" },
                    { "ProductName", "Tên sản phẩm" },
                    { "StepNumber", "Thứ tự bước" },
                    { "StepName", "Tên công đoạn" },
                    { "WorkCenter", "Trung tâm làm việc" },
                    { "EstimatedTime", "Thời gian chuẩn (phút)" },
                    { "OutputDescription", "Mô tả đầu ra" },
                    // Warehouse Mapping
                    { "CurrentQty", "Số lượng tồn" },
                    { "Warehouse", "Kho" },
                    { "UnitPrice", "Đơn giá" },
                    { "TotalValue", "Giá trị ước tính" },
                    { "TransactionDate", "Ngày giao dịch" },
                    { "TransBy", "Người thực hiện" },
                    { "ShortageQuantity", "Số lượng thiếu" },
                    { "Type", "Loại" },
                    { "Quantity", "Số lượng" },
                    { "SourceWarehouse", "Kho nguồn" },
                    { "DestWarehouse", "Kho đích" },
                    { "MinStock", "Mức tối thiểu" },
                    { "AlertLevel", "Mức độ cảnh báo" },
                    // Schedule Mapping
                    { "MachineCode", "Chuyền sản xuất" },
                    // Financial & Cost Mapping
                    { "MaSP", "Mã sản phẩm" },
                    { "TenSP", "Tên sản phẩm" },
                    { "Nhom", "Nhóm sản phẩm" },
                    { "SoLuong", "Số lượng" },
                    { "GiaThanh", "Tổng giá thành" },
                    { "DonGia", "Đơn giá" },
                    { "CP_NguyenLieu", "Chi phí Nguyên vật liệu" },
                    { "CP_NhanCong", "Chi phí Nhân công" },
                    { "CP_SanXuatChung", "Chi phí Sản xuất chung" },
                    { "Ngay", "Ngày giao dịch" },
                    { "MaGD", "Mã giao dịch" },
                    { "Loai", "Loại giao dịch" },
                    { "SoTien", "Số tiền" },
                    { "HangMuc", "Hạng mục" },
                    { "MoTa", "Mô tả" },
                    { "PhuongThuc", "Phương thức" },
                    
                    // HR Mapping (Exact match with UI columns)
                    { "EmployeeCode", "Mã NV" },
                    { "FullName", "Họ tên" },
                    { "Department", "Phòng ban" },
                    { "Position", "Chức vụ" },
                    { "Phone", "SĐT" },
                    { "Email", "Email" },
                    { "JoinDate", "Ngày vào làm" },

                    { "MonthYear", "Tháng/Năm" },
                    { "Name", "Họ tên" },
                    { "BasicSalary", "Lương cơ bản" },
                    { "ProductionSalary", "Lương sản phẩm" },
                    { "Allowance", "Phụ cấp" },
                    { "Deduction", "Khấu trừ" },
                    { "TotalSalary", "Thực lĩnh" },

                    { "WorkDays", "Tổng ngày công" },
                    { "LateTimes", "Số lần đi muộn" },
                    { "AbsentDays", "Số ngày nghỉ" },
                    { "OvertimeHours", "Tổng tăng ca" },

                    // Schedule mappings
                    { "WorkDate", "Ngày" },
                    { "ShiftName", "Ca làm việc" },
                    { "StartTime", "Bắt đầu" },
                    { "EndTime", "Kết thúc" }
                };

                // Get properties
                var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var validProps = props.Where(p => headers.ContainsKey(p.Name)).ToList();

                // Filter properties based on the selected columns if provided
                if (columns != null && columns.Any())
                {
                    validProps = validProps.Where(p => columns.Contains(headers[p.Name], StringComparer.OrdinalIgnoreCase)).ToList();
                }

                int colCount = Math.Max(1, validProps.Count);

                // Title and Header Info (Now that we know colCount)
                worksheet.Cell(1, 1).Value = reportTitle.ToUpper();
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
                worksheet.Range(1, 1, 1, colCount).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                worksheet.Cell(2, 1).Style.Font.Italic = true;
                worksheet.Range(2, 1, 2, colCount).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Table Headers (Row 4)
                for (int i = 0; i < validProps.Count; i++)
                {
                    var cell = worksheet.Cell(4, i + 1);
                    cell.Value = headers[validProps[i].Name];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                // Data Rows
                for (int row = 0; row < list.Count; row++)
                {
                    for (int col = 0; col < validProps.Count; col++)
                    {
                        var cell = worksheet.Cell(row + 5, col + 1);
                        var prop = validProps[col];
                        var val = prop.GetValue(list[row]);

                        // Custom Formatting based on column name
                        var moneyCols = new[] { "UnitPrice", "TotalValue", "GiaThanh", "DonGia", "CP_NguyenLieu", "CP_NhanCong", "CP_SanXuatChung", "SoTien" };
                        
                        if (prop.Name == "Progress")
                        {
                            cell.Value = (double.Parse(val?.ToString() ?? "0") / 100);
                            cell.Style.NumberFormat.Format = "0%";
                        }
                        else if (moneyCols.Contains(prop.Name))
                        {
                            if (decimal.TryParse(val?.ToString(), out decimal moneyVal))
                            {
                                cell.Value = moneyVal;
                                cell.Style.NumberFormat.Format = "#,##0";
                            }
                            else
                            {
                                cell.Value = val?.ToString() ?? "";
                            }
                        }
                        else
                        {
                            cell.Value = val?.ToString() ?? "";
                        }
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
                return true;
            });
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> GenerateImportTemplateAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Sheet1", Dictionary<string, string> customHeaders = null)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(sheetName);
                var list = data.ToList();

                var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                // 1. Write Headers at Row 1
                for (int i = 0; i < props.Length; i++)
                {
                    var cell = worksheet.Cell(1, i + 1);
                    string headerName = props[i].Name;
                    
                    // Use custom header if provided
                    if (customHeaders != null && customHeaders.ContainsKey(headerName))
                        headerName = customHeaders[headerName];

                    cell.Value = headerName;
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                    cell.Style.Font.FontColor = XLColor.White;
                }

                // 2. Write Data starting from Row 2
                for (int row = 0; row < list.Count; row++)
                {
                    for (int col = 0; col < props.Length; col++)
                    {
                        var cell = worksheet.Cell(row + 2, col + 1);
                        var val = props[col].GetValue(list[row]);
                        cell.Value = val?.ToString() ?? "";
                    }
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
                return true;
            });
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<IEnumerable<T>> ImportFromExcelAsync<T>(string filePath) where T : new()
    {
        try
        {
            return await Task.Run(() =>
            {
                var list = new List<T>();
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(1);
                
                // Smart Detection: Find the header row (either row 1 or row 4)
                int headerRowIndex = 1;
                var row1Value = worksheet.Cell(1, 1).Value.ToString();
                if (row1Value.Contains("BÁO CÁO") || string.IsNullOrEmpty(row1Value))
                {
                    headerRowIndex = 4; // Likely a standard report format
                }

                var headerRow = worksheet.Row(headerRowIndex);
                var props = typeof(T).GetProperties();
                
                // Map headers to property names
                // We'll use a mapping dictionary to handle Vietnamese headers
                var reverseMapping = new Dictionary<string, string>
                {
                    { "Mã lệnh sản xuất", "Code" },
                    { "Mã vật tư", "MaterialCode" },
                    { "Tên vật tư", "MaterialName" },
                    { "Tên sản phẩm", "Product" },
                    { "Số lượng mục tiêu", "Quantity" },
                    { "Phân loại", "Category" },
                    { "Đơn vị tính", "Unit" },
                    { "Đơn vị", "Unit" },
                    { "Tồn tối thiểu", "MinStock" },
                    { "Trạng thái hiện tại", "Status" },
                    { "Hạn hoàn thành", "Deadline" },
                    { "Mã nhân viên", "EmployeeCode" },
                    { "Họ và Tên", "FullName" },
                    { "Ngày trực", "WorkDate" },
                    { "Ca làm việc", "ShiftName" },
                    { "Chuyền sản xuất", "MachineCode" }
                };

                var columnsMap = new Dictionary<int, PropertyInfo>();
                var cells = headerRow.CellsUsed();
                foreach (var cell in cells)
                {
                    string headerText = cell.Value.ToString().Trim();
                    string propName = headerText;

                    // If it's a known Vietnamese header, map to English property name
                    if (reverseMapping.ContainsKey(headerText))
                        propName = reverseMapping[headerText];

                    var prop = props.FirstOrDefault(p => p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
                    if (prop != null)
                    {
                        columnsMap.Add(cell.Address.ColumnNumber, prop);
                    }
                }

                var dataRows = worksheet.RowsUsed(r => r.RowNumber() > headerRowIndex);
                foreach (var row in dataRows)
                {
                    var item = new T();
                    bool hasData = false;
                    foreach (var entry in columnsMap)
                    {
                        var cellVal = row.Cell(entry.Key).Value;
                        if (!cellVal.IsBlank)
                        {
                            try {
                                var targetType = Nullable.GetUnderlyingType(entry.Value.PropertyType) ?? entry.Value.PropertyType;
                                var convertedValue = Convert.ChangeType(cellVal.ToString(), targetType);
                                entry.Value.SetValue(item, convertedValue);
                                hasData = true;
                            } catch { /* Skip invalid format */ }
                        }
                    }
                    if (hasData) list.Add(item);
                }
                return list.AsEnumerable();
            });
        }
        catch (Exception)
        {
            return Enumerable.Empty<T>();
        }
    }

    public async Task<bool> ExportToPdfAsync<T>(IEnumerable<T> data, string filePath, string title, IEnumerable<string> columns = null)
    {
        try
        {
            var list = data.ToList();
            if (!list.Any()) return false;

            // Define Headers Mapping (Same as Excel for consistency)
            var headersMapping = new Dictionary<string, string>
            {
                { "Code", "Mã lệnh" },
                { "Product", "Sản phẩm" },
                { "TargetQty", "Số lượng" },
                { "Progress", "Tiến độ" },
                { "Status", "Trạng thái" },
                { "Deadline", "Hạn hoàn thành" },
                { "EndDate", "Hạn hoàn thành" },
                { "Content", "Nội dung" },
                { "User", "Người thực hiện" },
                { "Time", "Thời gian" },
                // Material Mapping
                { "MaterialCode", "Mã vật tư" },
                { "MaterialName", "Tên vật tư" },
                { "Category", "Phân loại" },
                { "Unit", "Đơn vị" },
                // BOM Mapping
                { "ParentCode", "Mã sản phẩm chính" },
                { "ParentName", "Tên sản phẩm chính" },
                { "ChildCode", "Mã linh kiện" },
                { "ChildName", "Tên linh kiện" },
                { "BomQuantity", "Số lượng định mức" },
                // Routing Mapping
                { "ProductCode", "Mã sản phẩm" },
                { "ProductName", "Tên sản phẩm" },
                { "StepNumber", "Thứ tự bước" },
                { "StepName", "Tên công đoạn" },
                { "WorkCenter", "Trung tâm làm việc" },
                { "EstimatedTime", "Thời gian chuẩn (phút)" },
                { "OutputDescription", "Mô tả đầu ra" },
                // Warehouse Mapping
                { "CurrentQty", "Số lượng tồn" },
                { "Warehouse", "Kho" },
                { "UnitPrice", "Đơn giá" },
                { "TotalValue", "Giá trị ước tính" },
                { "TransactionDate", "Ngày giao dịch" },
                { "TransBy", "Người thực hiện" },
                { "ShortageQuantity", "Số lượng thiếu" },
                { "Type", "Loại" },
                { "Quantity", "Số lượng" },
                { "SourceWarehouse", "Kho nguồn" },
                { "DestWarehouse", "Kho đích" },
                { "MinStock", "Mức tối thiểu" },
                { "AlertLevel", "Mức độ cảnh báo" },
                { "MachineCode", "Chuyền sản xuất" },
                // Financial & Cost Mapping
                { "MaSP", "Mã sản phẩm" },
                { "TenSP", "Tên sản phẩm" },
                { "Nhom", "Nhóm sản phẩm" },
                { "SoLuong", "Số lượng" },
                { "GiaThanh", "Tổng giá thành" },
                { "DonGia", "Đơn giá" },
                { "CP_NguyenLieu", "Chi phí Nguyên vật liệu" },
                { "CP_NhanCong", "Chi phí Nhân công" },
                { "CP_SanXuatChung", "Chi phí Sản xuất chung" },
                { "Ngay", "Ngày giao dịch" },
                { "MaGD", "Mã giao dịch" },
                { "Loai", "Loại giao dịch" },
                { "SoTien", "Số tiền" },
                { "HangMuc", "Hạng mục" },
                { "MoTa", "Mô tả" },
                { "PhuongThuc", "Phương thức" },
                
                // HR Mapping (Exact match with UI columns)
                { "EmployeeCode", "Mã NV" },
                { "FullName", "Họ tên" },
                { "Department", "Phòng ban" },
                { "Position", "Chức vụ" },
                { "Phone", "SĐT" },
                { "Email", "Email" },
                { "JoinDate", "Ngày vào làm" },

                { "MonthYear", "Tháng/Năm" },
                { "Name", "Họ tên" },
                { "BasicSalary", "Lương cơ bản" },
                { "ProductionSalary", "Lương sản phẩm" },
                { "Allowance", "Phụ cấp" },
                { "Deduction", "Khấu trừ" },
                { "TotalSalary", "Thực lĩnh" },

                { "WorkDays", "Tổng ngày công" },
                { "LateTimes", "Số lần đi muộn" },
                { "AbsentDays", "Số ngày nghỉ" },
                { "OvertimeHours", "Tổng tăng ca" },

                // Schedule mappings
                { "WorkDate", "Ngày" },
                { "ShiftName", "Ca làm việc" },
                { "StartTime", "Bắt đầu" },
                { "EndTime", "Kết thúc" }
            };

            var props = typeof(T).GetProperties();
            var validProps = props.Where(p => headersMapping.ContainsKey(p.Name)).ToList();

            if (columns != null && columns.Any())
            {
                validProps = validProps.Where(p => columns.Contains(headersMapping[p.Name], StringComparer.OrdinalIgnoreCase)).ToList();
            }

            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(title.ToUpper()).FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                                col.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").Italic().FontSize(9);
                            });
                        });

                        page.Content().PaddingVertical(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                foreach (var prop in validProps)
                                {
                                    if (prop.Name == "Content") columns.RelativeColumn(3);
                                    else columns.RelativeColumn();
                                }
                            });

                            table.Header(header =>
                            {
                                foreach (var prop in validProps)
                                {
                                    header.Cell().Background(Colors.Blue.Medium).Padding(5).Text(headersMapping[prop.Name]).FontColor(Colors.White).SemiBold();
                                }
                            });

                            foreach (var item in list)
                            {
                                foreach (var prop in validProps)
                                {
                                    var valObj = prop.GetValue(item);
                                    string val = valObj?.ToString() ?? "";
                                    
                                    // Định dạng tiền tệ cho PDF
                                    var moneyCols = new[] { "UnitPrice", "TotalValue", "GiaThanh", "DonGia", "CP_NguyenLieu", "CP_NhanCong", "CP_SanXuatChung", "SoTien" };
                                    if (moneyCols.Contains(prop.Name) && decimal.TryParse(val, out decimal moneyVal))
                                    {
                                        val = moneyVal.ToString("N0");
                                    }

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(val);
                                }
                            }
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Trang ");
                            x.CurrentPageNumber();
                        });
                    });
                }).GeneratePdf(filePath);
            });

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
