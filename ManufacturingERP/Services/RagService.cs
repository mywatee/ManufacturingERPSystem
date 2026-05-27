using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public class RagService : IRagService
{
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;
    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentProcessingService _documentProcessing;

    public RagService(
        IDbContextFactory<ManufacturingContext> contextFactory,
        IEmbeddingService embeddingService,
        IDocumentProcessingService documentProcessing)
    {
        _contextFactory = contextFactory;
        _embeddingService = embeddingService;
        _documentProcessing = documentProcessing;
    }

    public async Task IndexDocumentAsync(int documentId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var document = await context.Documents
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null) return;

        // Remove old chunks
        context.DocumentChunks.RemoveRange(document.Chunks);
        document.Chunks.Clear();

        // Extract text
        var pageTexts = _documentProcessing.ExtractText(document.FilePath, document.ContentType);
        var fullText = string.Join("\n\n", pageTexts);

        // Chunk
        var chunkTexts = _documentProcessing.ChunkText(fullText);

        // Generate embeddings in batches
        var chunks = new List<DocumentChunk>();
        for (int i = 0; i < chunkTexts.Count; i++)
        {
            var chunk = new DocumentChunk
            {
                DocumentId = documentId,
                Content = chunkTexts[i],
                ChunkIndex = i
            };
            chunks.Add(chunk);
        }

        // Generate embeddings (batch call)
        try
        {
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunkTexts);
            for (int i = 0; i < chunks.Count && i < embeddings.Count; i++)
            {
                chunks[i].EmbeddingJson = JsonSerializer.Serialize(embeddings[i]);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Embedding failed: {ex.Message}");
        }

        context.DocumentChunks.AddRange(chunks);
        document.ChunkCount = chunks.Count;

        await context.SaveChangesAsync();
    }

    public async Task<List<RagSearchResult>> SearchAsync(string query, int topK = 5, double minScore = 0.5)
    {
        float[] queryEmbedding;
        try
        {
            queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
        }
        catch
        {
            return new List<RagSearchResult>();
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var chunks = await context.DocumentChunks
            .Include(c => c.Document)
            .Where(c => c.EmbeddingJson != null)
            .ToListAsync();

        var results = new List<(RagSearchResult Result, double Score)>();

        foreach (var chunk in chunks)
        {
            if (string.IsNullOrWhiteSpace(chunk.EmbeddingJson)) continue;

            try
            {
                var chunkEmbedding = JsonSerializer.Deserialize<float[]>(chunk.EmbeddingJson);
                if (chunkEmbedding == null) continue;

                var score = CosineSimilarity(queryEmbedding, chunkEmbedding);
                if (score >= minScore)
                {
                    results.Add((new RagSearchResult
                    {
                        Content = chunk.Content,
                        FileName = chunk.Document.OriginalFileName,
                        Score = score,
                        DocumentId = chunk.DocumentId
                    }, score));
                }
            }
            catch { }
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .Select(r => r.Result)
            .ToList();
    }

    public async Task DeleteDocumentChunksAsync(int documentId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var chunks = await context.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .ToListAsync();
        context.DocumentChunks.RemoveRange(chunks);
        await context.SaveChangesAsync();
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;

        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0) return 0;
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
