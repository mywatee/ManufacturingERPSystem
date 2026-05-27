using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public class DocumentManagementService : IDocumentManagementService
{
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;
    private readonly IRagService _ragService;

    private static readonly string DocumentsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ManufacturingERP", "Documents");

    public DocumentManagementService(
        IDbContextFactory<ManufacturingContext> contextFactory,
        IRagService ragService)
    {
        _contextFactory = contextFactory;
        _ragService = ragService;
        Directory.CreateDirectory(DocumentsFolder);
    }

    public async Task<List<Document>> GetDocumentsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Documents
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<Document?> GetDocumentAsync(int documentId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Documents.FindAsync(documentId);
    }

    public async Task<Document> UploadDocumentAsync(
        string sourceFilePath, string originalFileName,
        string? description, string? category, int uploadedByUserId)
    {
        var ext = Path.GetExtension(originalFileName).TrimStart('.').ToLowerInvariant();
        var contentType = ext switch
        {
            "pdf" => "pdf",
            "docx" => "docx",
            "txt" => "txt",
            _ => "unknown"
        };

        var fileInfo = new FileInfo(sourceFilePath);
        var storedFileName = $"{Guid.NewGuid():N}.{ext}";
        var storedPath = Path.Combine(DocumentsFolder, storedFileName);

        File.Copy(sourceFilePath, storedPath, overwrite: true);

        var document = new Document
        {
            FileName = storedFileName,
            OriginalFileName = originalFileName,
            ContentType = contentType,
            FileSize = fileInfo.Length,
            FilePath = storedPath,
            UploadedAt = DateTime.Now,
            UploadedByUserId = uploadedByUserId,
            Description = description,
            Category = category
        };

        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        // Index for RAG
        await _ragService.IndexDocumentAsync(document.DocumentId);

        return document;
    }

    public async Task<bool> DeleteDocumentAsync(int documentId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var document = await context.Documents.FindAsync(documentId);
        if (document == null) return false;

        // Delete file
        try
        {
            if (File.Exists(document.FilePath))
                File.Delete(document.FilePath);
        }
        catch { }

        // Delete RAG chunks
        await _ragService.DeleteDocumentChunksAsync(documentId);

        context.Documents.Remove(document);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task ReindexDocumentAsync(int documentId)
    {
        await _ragService.IndexDocumentAsync(documentId);
    }
}
