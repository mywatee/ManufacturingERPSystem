namespace ManufacturingERP.Models;

public class Document
{
    public int DocumentId { get; set; }
    public string FileName { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string ContentType { get; set; } = ""; // pdf, docx, txt
    public long FileSize { get; set; }
    public string FilePath { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.Now;
    public int UploadedByUserId { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; } // SOP, contract, manual, etc.
    public int ChunkCount { get; set; }

    public User? UploadedBy { get; set; }
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
