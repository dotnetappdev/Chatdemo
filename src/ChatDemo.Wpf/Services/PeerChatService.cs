using System.Collections.Concurrent;
using Grpc.Core;
using ChatDemo.Protos;

namespace ChatDemo.Wpf.Services;

/// <summary>
/// gRPC service that runs inside each WPF client.
/// Other peers call this service to deliver messages.
/// </summary>
public class PeerChatService : ChatService.ChatServiceBase
{
    // Registered in-process subscribers waiting for messages
    private readonly ConcurrentDictionary<string, IServerStreamWriter<ChatMessageRequest>> _subscribers = new();

    // Event raised on the UI thread when a message arrives
    public event Action<ChatMessageRequest>? MessageReceived;

    public string LocalUserId { get; set; } = string.Empty;
    public string LocalUserName { get; set; } = string.Empty;

    // ── Unary: one peer sends a message directly ──────────────────────────────
    public override Task<ChatMessageReply> SendMessage(
        ChatMessageRequest request, ServerCallContext context)
    {
        // Notify any in-process subscriber (e.g. UI)
        MessageReceived?.Invoke(request);

        // Push to any active streaming subscribers for this user
        if (_subscribers.TryGetValue(request.ReceiverId, out var writer))
        {
            _ = TryWriteAsync(writer, request);
        }

        return Task.FromResult(new ChatMessageReply { Success = true, Message = "Delivered" });
    }

    // ── Server streaming: client subscribes and receives pushed messages ───────
    public override async Task SubscribeToMessages(
        SubscribeRequest request,
        IServerStreamWriter<ChatMessageRequest> responseStream,
        ServerCallContext context)
    {
        _subscribers[request.UserId] = responseStream;
        try
        {
            // Keep alive until client disconnects
            await Task.Delay(Timeout.Infinite, context.CancellationToken);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _subscribers.TryRemove(request.UserId, out _);
        }
    }

    // ── Bidirectional streaming: real-time chat session ───────────────────────
    public override async Task Chat(
        IAsyncStreamReader<ChatMessageRequest> requestStream,
        IServerStreamWriter<ChatMessageRequest> responseStream,
        ServerCallContext context)
    {
        await foreach (var msg in requestStream.ReadAllAsync(context.CancellationToken))
        {
            MessageReceived?.Invoke(msg);

            // Echo back an ack system message
            var ack = new ChatMessageRequest
            {
                MessageId = Guid.NewGuid().ToString(),
                SenderId = LocalUserId,
                SenderName = LocalUserName,
                ReceiverId = msg.SenderId,
                Content = msg.Content,
                Timestamp = DateTime.UtcNow.ToString("O"),
                Type = MessageType.System
            };
            await responseStream.WriteAsync(ack);
        }
    }

    // ── Ping/health check ─────────────────────────────────────────────────────
    public override Task<PingReply> Ping(PingRequest request, ServerCallContext context)
    {
        return Task.FromResult(new PingReply
        {
            ResponderId = LocalUserId,
            ResponderName = LocalUserName,
            Online = true
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    private static async Task TryWriteAsync(
        IServerStreamWriter<ChatMessageRequest> writer,
        ChatMessageRequest message)
    {
        try { await writer.WriteAsync(message); }
        catch { /* subscriber disconnected */ }
    }
}
