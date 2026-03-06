using Grpc.Net.Client;
using ChatDemo.Protos;

namespace ChatDemo.Wpf.Services;

/// <summary>
/// Wraps a gRPC channel/stub to a specific peer address.
/// Supports unary send, bidirectional streaming, and ping.
/// </summary>
public class GrpcPeerClient : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly ChatService.ChatServiceClient _client;
    private bool _disposed;

    public string Address { get; }

    public GrpcPeerClient(string address)
    {
        Address = address;
        // Insecure channel for LAN demo (use TLS in production)
        _channel = GrpcChannel.ForAddress($"http://{address}", new GrpcChannelOptions
        {
            // Allow large messages
        });
        _client = new ChatService.ChatServiceClient(_channel);
    }

    /// <summary>Send a single message (unary RPC).</summary>
    public async Task<bool> SendMessageAsync(ChatMessageRequest message,
        CancellationToken ct = default)
    {
        try
        {
            var reply = await _client.SendMessageAsync(message, cancellationToken: ct);
            return reply.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Ping the peer to check if it is online.</summary>
    public async Task<PingReply?> PingAsync(string senderId, string senderName,
        CancellationToken ct = default)
    {
        try
        {
            return await _client.PingAsync(
                new PingRequest { SenderId = senderId, SenderName = senderName },
                cancellationToken: ct);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Opens a bidirectional streaming session with the peer.
    /// Returns the duplex call so the caller can send and receive concurrently.
    /// </summary>
    public Grpc.Core.AsyncDuplexStreamingCall<ChatMessageRequest, ChatMessageRequest>
        OpenChatStream(CancellationToken ct = default)
    {
        return _client.Chat(cancellationToken: ct);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _channel.Dispose();
    }
}
