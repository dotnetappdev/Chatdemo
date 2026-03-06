namespace ChatDemo.Api.Models;

// ─── User DTOs ───────────────────────────────────────────────────────────────

public record RegisterUserRequest(
    string Username,
    string DisplayName,
    string GrpcAddress);

public record UpdatePresenceRequest(
    bool IsOnline,
    string GrpcAddress);

public record UserDto(
    Guid Id,
    string Username,
    string DisplayName,
    string GrpcAddress,
    bool IsOnline,
    DateTime LastSeenAt);

// ─── Message DTOs ─────────────────────────────────────────────────────────────

public record SaveMessageRequest(
    Guid SenderId,
    Guid ReceiverId,
    string Content);

public record MessageDto(
    Guid Id,
    Guid SenderId,
    string SenderName,
    Guid ReceiverId,
    string ReceiverName,
    string Content,
    DateTime SentAt,
    bool IsDelivered);
