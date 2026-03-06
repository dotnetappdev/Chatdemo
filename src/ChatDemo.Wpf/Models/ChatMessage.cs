using CommunityToolkit.Mvvm.ComponentModel;

namespace ChatDemo.Wpf.Models;

/// <summary>A single chat message displayed in the chat area.</summary>
public partial class ChatMessage : ObservableObject
{
    [ObservableProperty] private Guid _id = Guid.NewGuid();
    [ObservableProperty] private string _senderId = string.Empty;
    [ObservableProperty] private string _senderName = string.Empty;
    [ObservableProperty] private string _content = string.Empty;
    [ObservableProperty] private DateTime _timestamp = DateTime.Now;
    [ObservableProperty] private bool _isOwn;   // true = right-aligned bubble
    [ObservableProperty] private bool _isDelivered;
}
