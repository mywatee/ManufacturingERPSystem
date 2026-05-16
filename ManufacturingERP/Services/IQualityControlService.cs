using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public struct TrendPoint
{
    public string Label { get; set; }
    public double DefectRate { get; set; }
    public int TotalSamples { get; set; }
    public int FailedSamples { get; set; }
}

public interface IQualityControlService
{
    Task<List<QualityControl>> GetRecentRecordsAsync(int count);
    Task<bool> AddRecordAsync(QualityControl record);
    Task<List<WorkOrder>> GetPendingInspectionOrdersAsync();
    Task<List<TrendPoint>> GetDefectTrendAsync(string period, int count);
    Task<(int Total, int Passed, int Failed, List<(string Category, int Count)> DefectStats)> GetStatisticsAsync(DateTime start, DateTime end);
}

