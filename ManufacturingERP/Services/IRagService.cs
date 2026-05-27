using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManufacturingERP.Services;

public class RagSearchResult
{
    public string Content { get; set; } = "";
    public string FileName { get; set; } = "";
    public double Score { get; set; }
    public int DocumentId { get; set; }
}

public interface IRagService
{
    Task IndexDocumentAsync(int documentId);
    Task<List<RagSearchResult>> SearchAsync(string query, int topK = 5, double minScore = 0.5);
    Task DeleteDocumentChunksAsync(int documentId);
}
