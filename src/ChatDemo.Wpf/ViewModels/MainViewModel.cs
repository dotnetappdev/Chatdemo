using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatDemo.Protos;
using ChatDemo.Wpf.Models;
using ChatDemo.Wpf.Services;

namespace ChatDemo.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly PeerChatService _peerService;
    private ChatApiService? _chatApiService;

    // Peer clients keyed by address
    private readonly Dictionary<string, GrpcPeerClient> _clients = [];

    // ── Bound properties ───────────────────────────────────────────────────────

    [ObservableProperty] private string _localUserId = Guid.NewGuid().ToString();
    [ObservableProperty] private string _localUserName = "Me";
    [ObservableProperty] private string _localGrpcPort = "5100";
    [ObservableProperty] private string _statusText = "Offline";
    [ObservableProperty] private bool _isConnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedContact))]
    private Contact? _selectedContact;

    [ObservableProperty] private string _messageInput = string.Empty;

    // Connect-to-peer dialog
    [ObservableProperty] private string _connectAddress = "localhost:5101";
    [ObservableProperty] private string _apiBaseUrl = "http://localhost:5000";

    public bool HasSelectedContact => SelectedContact is not null;

    public ObservableCollection<Contact> Contacts { get; } = [];
    public ObservableCollection<ChatMessage> Messages { get; } = [];

    // ── Constructor ────────────────────────────────────────────────────────────

    public MainViewModel(PeerChatService peerService)
    {
        _peerService = peerService;
        _peerService.MessageReceived += OnMessageReceived;
    }

    // ── Commands ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task StartAsync()
    {
        // Update gRPC service identity
        _peerService.LocalUserId = LocalUserId;
        _peerService.LocalUserName = LocalUserName;

        StatusText = $"Listening on port {LocalGrpcPort} as \"{LocalUserName}\"";
        IsConnected = true;

        // Try to register with REST API
        if (!string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            _chatApiService = new ChatApiService(
                ApiBaseUrl.EndsWith('/') ? ApiBaseUrl : ApiBaseUrl + "/");

            var dto = await _chatApiService.RegisterAsync(
                LocalUserName.ToLowerInvariant().Replace(" ", "_"),
                LocalUserName,
                $"localhost:{LocalGrpcPort}");

            if (dto is not null)
            {
                LocalUserId = dto.Id.ToString();
                _peerService.LocalUserId = LocalUserId;
                StatusText = $"Registered with API · Listening on port {LocalGrpcPort}";
                await RefreshContactsFromApiAsync();
            }
        }
    }

    [RelayCommand]
    private async Task ConnectToPeerAsync()
    {
        var address = ConnectAddress.Trim();
        if (string.IsNullOrEmpty(address)) return;

        if (_clients.ContainsKey(address))
        {
            StatusText = $"Already connected to {address}";
            return;
        }

        var client = new GrpcPeerClient(address);
        var ping = await client.PingAsync(LocalUserId, LocalUserName);

        if (ping is null)
        {
            StatusText = $"Could not reach peer at {address}";
            client.Dispose();
            return;
        }

        _clients[address] = client;

        // Add to contacts if not already present
        if (!Contacts.Any(c => c.GrpcAddress == address))
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Contacts.Add(new Contact
                {
                    Id = Guid.TryParse(ping.ResponderId, out var g) ? g : Guid.NewGuid(),
                    DisplayName = ping.ResponderName,
                    Username = ping.ResponderName,
                    GrpcAddress = address,
                    IsOnline = ping.Online
                });
            });
        }

        StatusText = $"Connected to {ping.ResponderName} @ {address}";
    }

    [RelayCommand]
    private void SelectContact(Contact contact)
    {
        SelectedContact = contact;
        contact.UnreadCount = 0;
        Messages.Clear();
        StatusText = $"Chat with {contact.DisplayName}";
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        var text = MessageInput.Trim();
        if (string.IsNullOrEmpty(text) || SelectedContact is null) return;

        MessageInput = string.Empty;

        var msg = new ChatMessageRequest
        {
            MessageId = Guid.NewGuid().ToString(),
            SenderId = LocalUserId,
            SenderName = LocalUserName,
            ReceiverId = SelectedContact.Id.ToString(),
            Content = text,
            Timestamp = DateTime.UtcNow.ToString("O"),
            Type = MessageType.Text
        };

        // Add to local UI immediately
        Application.Current.Dispatcher.Invoke(() =>
        {
            Messages.Add(new ChatMessage
            {
                Id = Guid.TryParse(msg.MessageId, out var g) ? g : Guid.NewGuid(),
                SenderId = LocalUserId,
                SenderName = LocalUserName,
                Content = text,
                Timestamp = DateTime.Now,
                IsOwn = true
            });
        });

        // Send via gRPC
        var address = SelectedContact.GrpcAddress;
        if (!_clients.TryGetValue(address, out var client))
        {
            client = new GrpcPeerClient(address);
            _clients[address] = client;
        }

        var sent = await client.SendMessageAsync(msg);

        if (sent && _chatApiService is not null &&
            Guid.TryParse(SelectedContact.Id.ToString(), out var receiverId))
        {
            // Persist via REST API; log failures but don't block the UI
            var senderId = Guid.TryParse(LocalUserId, out var sid) ? sid : Guid.NewGuid();
            await _chatApiService.SaveMessageAsync(senderId, receiverId, text)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        Application.Current.Dispatcher.Invoke(() =>
                            StatusText = "⚠ Could not save message to API");
                }, TaskScheduler.Default);
        }

        SelectedContact.LastMessage = text;
        if (!sent) StatusText = $"⚠ Message may not have been delivered to {SelectedContact.DisplayName}";
    }

    [RelayCommand]
    private async Task RefreshContactsAsync()
    {
        await RefreshContactsFromApiAsync();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task RefreshContactsFromApiAsync()
    {
        if (_chatApiService is null) return;

        var users = await _chatApiService.GetUsersAsync();
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var u in users)
            {
                if (u.Id.ToString() == LocalUserId) continue;

                var existing = Contacts.FirstOrDefault(c => c.Id == u.Id);
                if (existing is not null)
                {
                    existing.IsOnline = u.IsOnline;
                    existing.GrpcAddress = u.GrpcAddress;
                    existing.DisplayName = u.DisplayName;
                }
                else
                {
                    Contacts.Add(new Contact
                    {
                        Id = u.Id,
                        Username = u.Username,
                        DisplayName = u.DisplayName,
                        GrpcAddress = u.GrpcAddress,
                        IsOnline = u.IsOnline
                    });
                }
            }
        });
    }

    private void OnMessageReceived(ChatMessageRequest msg)
    {
        if (msg.Type == MessageType.System) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            var chatMsg = new ChatMessage
            {
                Id = Guid.TryParse(msg.MessageId, out var g) ? g : Guid.NewGuid(),
                SenderId = msg.SenderId,
                SenderName = msg.SenderName,
                Content = msg.Content,
                Timestamp = DateTime.TryParse(msg.Timestamp, out var ts) ? ts : DateTime.Now,
                IsOwn = false
            };

            // If the sender is the currently selected contact, show immediately
            if (SelectedContact?.Id.ToString() == msg.SenderId)
            {
                Messages.Add(chatMsg);
            }
            else
            {
                // Increment unread badge
                var contact = Contacts.FirstOrDefault(c => c.Id.ToString() == msg.SenderId);
                if (contact is not null)
                {
                    contact.UnreadCount++;
                    contact.LastMessage = msg.Content;
                }
                else
                {
                    // Unknown sender - add as new contact
                    Contacts.Add(new Contact
                    {
                        Id = Guid.TryParse(msg.SenderId, out var cg) ? cg : Guid.NewGuid(),
                        DisplayName = msg.SenderName,
                        Username = msg.SenderName,
                        GrpcAddress = string.Empty,
                        IsOnline = true,
                        UnreadCount = 1,
                        LastMessage = msg.Content
                    });
                }
            }
        });
    }

    public void Cleanup()
    {
        foreach (var c in _clients.Values) c.Dispose();
        _clients.Clear();
        _chatApiService?.Dispose();
    }
}
