using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Models;
using ManufacturingERP.Services;

namespace ManufacturingERP.ViewModels;

public partial class AIChatViewModel : ObservableObject
{
    private readonly IAIChatService _aiChatService;
    private readonly IAuthService _auth;

    private const int MaxContextCount = 20;
    private const int RecentMessageCount = 5;
    private const string AppDataFolder = "ManufacturingERP";
    private const string ChatFilePrefix = "chat_";

    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _messageText = "";

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public ObservableCollection<string> SuggestedQuestions { get; } =
    [
        "Xem danh sách lệnh sản xuất",
        "Kiểm tra tồn kho hiện tại",
        "Bảng lương tháng này",
        "Thống kê chất lượng gần đây",
        "Cảnh báo tồn kho thấp"
    ];

    public bool ShowSuggestions => Messages.Count <= 1;

    public AIChatViewModel(IAIChatService aiChatService, IAuthService auth)
    {
        _aiChatService = aiChatService;
        _auth = auth;

        LoadChatHistory();

        if (Messages.Count == 0)
        {
            Messages.Add(new ChatMessage
            {
                Content = "Xin chào! Tôi là trợ lý ERP. Bạn cần hỗ trợ gì?",
                IsUser = false
            });
        }
    }

    partial void OnIsOpenChanged(bool value)
    {
        if (value)
            OnPropertyChanged(nameof(ShowSuggestions));
    }

    [RelayCommand]
    private void Toggle() => IsOpen = !IsOpen;

    [RelayCommand]
    private void Close() => IsOpen = false;

    [RelayCommand]
    private async Task SendSuggestion(string question)
    {
        MessageText = question;
        await SendMessage();
    }

    [RelayCommand]
    private void CopyMessage(string content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            System.Windows.Clipboard.SetText(content);
        }
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        var text = MessageText?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        MessageText = "";
        IsLoading = true;

        var userMsg = new ChatMessage { Content = text, IsUser = true };
        Messages.Add(userMsg);
        OnPropertyChanged(nameof(ShowSuggestions));

        var aiMsg = new ChatMessage { Content = "", IsUser = false };
        Messages.Add(aiMsg);

        try
        {
            var history = BuildOptimizedHistory();

            await foreach (var token in _aiChatService.StreamSendMessageAsync(text, history))
            {
                aiMsg.Content += token;
            }
        }
        catch (Exception ex)
        {
            aiMsg.Content = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            SaveChatHistory();
        }
    }

    private List<ChatMessage> BuildOptimizedHistory()
    {
        var allMessages = Messages.Take(Messages.Count - 2).ToList();

        if (allMessages.Count <= MaxContextCount)
            return allMessages;

        var recent = allMessages.TakeLast(RecentMessageCount).ToList();
        var olderCount = allMessages.Count - RecentMessageCount;

        var olderMsgs = allMessages.Take(olderCount).ToList();
        var summaryText = new StringBuilder();
        summaryText.AppendLine($"[Lược bỏ {olderCount} tin nhắn cũ]");
        foreach (var m in olderMsgs.Take(4))
        {
            var prefix = m.IsUser ? "Người dùng: " : "Trợ lý: ";
            summaryText.AppendLine(prefix + (m.Content.Length > 100 ? m.Content[..100] + "..." : m.Content));
        }
        if (olderMsgs.Count > 4)
            summaryText.AppendLine($"... và {olderMsgs.Count - 4} tin nhắn khác");

        var summary = new List<ChatMessage>
        {
            new() { Content = summaryText.ToString().TrimEnd(), IsUser = false }
        };
        summary.AddRange(recent);
        return summary;
    }

    // ========== Persistence ==========

    private static string GetChatFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, AppDataFolder);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{ChatFilePrefix}history.json");
    }

    private void LoadChatHistory()
    {
        try
        {
            var path = GetChatFilePath();
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var messages = JsonSerializer.Deserialize<List<ChatMessageData>>(json);
            if (messages == null || messages.Count == 0) return;

            Messages.Clear();
            foreach (var m in messages)
            {
                Messages.Add(new ChatMessage
                {
                    Content = m.Content,
                    IsUser = m.IsUser,
                    Timestamp = m.Timestamp
                });
            }
        }
        catch { }
    }

    private void SaveChatHistory()
    {
        try
        {
            var path = GetChatFilePath();
            var data = Messages.Select(m => new ChatMessageData
            {
                Content = m.Content,
                IsUser = m.IsUser,
                Timestamp = m.Timestamp
            }).ToList();

            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(path, json);
        }
        catch { }
    }

    private class ChatMessageData
    {
        public string Content { get; set; } = "";
        public bool IsUser { get; set; }
        public string Timestamp { get; set; } = "";
    }
}
