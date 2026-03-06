using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatDemo.Api.Data;
using ChatDemo.Api.Models;

namespace ChatDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController(ChatDbContext db) : ControllerBase
{
    // GET api/messages/conversation?userA=...&userB=...
    [HttpGet("conversation")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetConversation(
        [FromQuery] Guid userA,
        [FromQuery] Guid userB,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 200) pageSize = 50;

        var messages = await db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m =>
                (m.SenderId == userA && m.ReceiverId == userB) ||
                (m.SenderId == userB && m.ReceiverId == userA))
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => MapToDto(m))
            .ToListAsync();

        // Return in ascending order for display
        return Ok(messages.OrderBy(m => m.SentAt));
    }

    // POST api/messages
    [HttpPost]
    public async Task<ActionResult<MessageDto>> Save([FromBody] SaveMessageRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Content))
            return BadRequest("Content is required.");

        var sender = await db.Users.FindAsync(req.SenderId);
        if (sender is null) return BadRequest("Sender not found.");

        var receiver = await db.Users.FindAsync(req.ReceiverId);
        if (receiver is null) return BadRequest("Receiver not found.");

        var message = new Message
        {
            SenderId = req.SenderId,
            ReceiverId = req.ReceiverId,
            Content = req.Content,
            IsDelivered = true
        };
        db.Messages.Add(message);
        await db.SaveChangesAsync();

        // Re-load navigation props
        message.Sender = sender;
        message.Receiver = receiver;

        return CreatedAtAction(nameof(GetConversation),
            new { userA = req.SenderId, userB = req.ReceiverId },
            MapToDto(message));
    }

    // DELETE api/messages/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var message = await db.Messages.FindAsync(id);
        if (message is null) return NotFound();
        db.Messages.Remove(message);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static MessageDto MapToDto(Message m) =>
        new(m.Id,
            m.SenderId,
            m.Sender?.DisplayName ?? string.Empty,
            m.ReceiverId,
            m.Receiver?.DisplayName ?? string.Empty,
            m.Content,
            m.SentAt,
            m.IsDelivered);
}
