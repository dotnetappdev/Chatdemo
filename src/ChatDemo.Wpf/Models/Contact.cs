using CommunityToolkit.Mvvm.ComponentModel;

namespace ChatDemo.Wpf.Models;

/// <summary>Represents a contact (another peer) in the contact list.</summary>
public partial class Contact : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _grpcAddress = string.Empty;
    [ObservableProperty] private bool _isOnline;
    [ObservableProperty] private int _unreadCount;
    [ObservableProperty] private string _lastMessage = string.Empty;
}
