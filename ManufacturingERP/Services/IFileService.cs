using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManufacturingERP.Services;

public interface IFileService
{
    /// <summary>
    /// Xuất danh sách dữ liệu ra file Excel
    /// </summary>
    Task<bool> ExportToExcelAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Sheet1", IEnumerable<string> columns = null, string reportTitle = "BÁO CÁO CHI TIẾT");

    /// <summary>
    /// Tạo file Excel mẫu để nhập liệu (không có tiêu đề báo cáo)
    /// </summary>
    Task<bool> GenerateImportTemplateAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Sheet1", Dictionary<string, string> customHeaders = null);

    /// <summary>
    /// Nhập dữ liệu từ file Excel
    /// </summary>
    Task<IEnumerable<T>> ImportFromExcelAsync<T>(string filePath) where T : new();

    /// <summary>
    /// Xuất dữ liệu ra file PDF
    /// </summary>
    Task<bool> ExportToPdfAsync<T>(IEnumerable<T> data, string filePath, string title, IEnumerable<string> columns = null);
}
