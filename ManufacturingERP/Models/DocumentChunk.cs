namespace ManufacturingERP.Models;

public class DocumentChunk
{
    public int ChunkId { get; set; }
    public int DocumentId { get; set; }
    public string Content { get; set; } = "";
    public int ChunkIndex { get; set; }
    public string? EmbeddingJson { get; set; } // stored as JSON float array

    public Document Document { get; set; } = null!;
}
