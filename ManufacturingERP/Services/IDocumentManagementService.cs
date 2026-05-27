using System.Collections.Generic;
using System.Threading.Tasks;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public interface IDocumentManagementService
{
    Task<List<Document>> GetDocumentsAsync();
    Task<Document?> GetDocumentAsync(int documentId);
    Task<Document> UploadDocumentAsync(string sourceFilePath, string originalFileName, string? description, string? category, int uploadedByUserId);
    Task<bool> DeleteDocumentAsync(int documentId);
    Task ReindexDocumentAsync(int documentId);
}
