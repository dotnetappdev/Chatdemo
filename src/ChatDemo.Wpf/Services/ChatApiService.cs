using System.Net.Http;
using System.Net.Http.Json;
using ChatDemo.Wpf.Models;

namespace ChatDemo.Wpf.Services;

// ── DTOs mirroring the API ─────────────────────────────────────────────────────

public record RegisterUserRequest(string Username, string DisplayName, string GrpcAddress);
public record UpdatePresenceRequest(bool IsOnline, string GrpcAddress);
public record SaveMessageRequest(Guid SenderId, Guid ReceiverId, string Content);

public record UserDto(Guid Id, string Username, string DisplayName,
    string GrpcAddress, bool IsOnline, DateTime LastSeenAt);

public record MessageDto(Guid Id, Guid SenderId, string SenderName,
    Guid ReceiverId, string ReceiverName, string Content,
    DateTime SentAt, bool IsDelivered);

// ── Service ────────────────────────────────────────────────────────────────────

/// <summary>HTTP client for the ChatDemo REST API.</summary>
public class ChatApiService : IDisposable
{
    // A single HttpClient per service instance avoids socket exhaustion.
    // The service itself should be created once and reused.
    private readonly HttpClient _http;
    private bool _disposed;

    public ChatApiService(string baseAddress)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseAddress) };
    }

    // ── Users ──────────────────────────────────────────────────────────────────

    public async Task<UserDto?> RegisterAsync(string username, string displayName, string grpcAddress)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/users/register",
                new RegisterUserRequest(username, displayName, grpcAddress));
            if (resp.IsSuccessStatusCode)
                return await resp.Content.ReadFromJsonAsync<UserDto>();
        }
        catch { }
        return null;
    }

    public async Task<IList<UserDto>> GetUsersAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<UserDto>>("api/users") ?? [];
        }
        catch { return []; }
    }

    public async Task UpdatePresenceAsync(Guid userId, bool isOnline, string grpcAddress)
    {
        try
        {
            await _http.PutAsJsonAsync($"api/users/{userId}/presence",
                new UpdatePresenceRequest(isOnline, grpcAddress));
        }
        catch { }
    }

    // ── Messages ───────────────────────────────────────────────────────────────

    public async Task<IList<MessageDto>> GetConversationAsync(Guid userA, Guid userB)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<MessageDto>>(
                $"api/messages/conversation?userA={userA}&userB={userB}") ?? [];
        }
        catch { return []; }
    }

    public async Task SaveMessageAsync(Guid senderId, Guid receiverId, string content)
    {
        try
        {
            await _http.PostAsJsonAsync("api/messages",
                new SaveMessageRequest(senderId, receiverId, content));
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _http.Dispose();
    }
}
