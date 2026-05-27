using System.Collections.Generic;

namespace ManufacturingERP.Services;

public interface IDocumentProcessingService
{
    List<string> ExtractText(string filePath, string contentType);
    List<string> ChunkText(string text, int chunkSize = 800, int overlap = 150);
}
