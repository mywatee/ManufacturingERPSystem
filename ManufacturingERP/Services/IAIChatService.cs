using System.Collections.Generic;
using System.Threading.Tasks;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public interface IAIChatService
{
    Task<string> SendMessageAsync(string message, List<ChatMessage> history);
    IAsyncEnumerable<string> StreamSendMessageAsync(string message, List<ChatMessage> history, CancellationToken cancellationToken = default);
}
