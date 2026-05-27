using CommunityToolkit.Mvvm.ComponentModel;

namespace ManufacturingERP.Models;

public partial class ChatMessage : ObservableObject
{
    [ObservableProperty] private string _content = "";
    [ObservableProperty] private bool _isUser;
    [ObservableProperty] private string _timestamp = DateTime.Now.ToString("HH:mm");
}
