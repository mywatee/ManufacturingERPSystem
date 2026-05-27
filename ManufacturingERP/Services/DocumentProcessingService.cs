using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ManufacturingERP.Services;

public class DocumentProcessingService : IDocumentProcessingService
{
    public List<string> ExtractText(string filePath, string contentType)
    {
        if (!File.Exists(filePath))
            return new List<string>();

        return contentType.ToLowerInvariant() switch
        {
            "pdf" => ExtractPdfText(filePath),
            "docx" => ExtractDocxText(filePath),
            "txt" => ExtractTxtText(filePath),
            _ => new List<string>
            {
                $"(Không hỗ trợ định dạng: {contentType})"
            }
        };
    }

    private static List<string> ExtractPdfText(string filePath)
    {
        var pages = new List<string>();
        using var pdf = PdfDocument.Open(filePath);
        foreach (var page in pdf.GetPages())
        {
            var text = page.Text;
            if (!string.IsNullOrWhiteSpace(text))
                pages.Add(text.Trim());
        }
        return pages;
    }

    private static List<string> ExtractDocxText(string filePath)
    {
        var paragraphs = new List<string>();
        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return paragraphs;

        var sb = new StringBuilder();
        foreach (var para in body.Elements<Paragraph>())
        {
            var text = para.InnerText?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
            }
        }

        if (sb.Length > 0)
            paragraphs.Add(sb.ToString().Trim());

        return paragraphs;
    }

    private static List<string> ExtractTxtText(string filePath)
    {
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        return string.IsNullOrWhiteSpace(content)
            ? new List<string>()
            : new List<string> { content.Trim() };
    }

    public List<string> ChunkText(string text, int chunkSize = 800, int overlap = 150)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var chunks = new List<string>();
        var paragraphs = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = new StringBuilder();
        foreach (var para in paragraphs)
        {
            var trimmed = para.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            if (currentChunk.Length + trimmed.Length > chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();

                if (chunks.Count > 0)
                {
                    var lastChunk = chunks[^1];
                    var overlapText = lastChunk.Length > overlap
                        ? lastChunk[^overlap..]
                        : lastChunk;
                    currentChunk.Append(overlapText);
                    currentChunk.AppendLine();
                }
            }

            currentChunk.AppendLine(trimmed);
        }

        if (currentChunk.Length > 0)
            chunks.Add(currentChunk.ToString().Trim());

        return chunks;
    }
}
