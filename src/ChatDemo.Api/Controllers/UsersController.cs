using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatDemo.Api.Data;
using ChatDemo.Api.Models;

namespace ChatDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(ChatDbContext db) : ControllerBase
{
    // GET api/users
    [HttpGet]
    public async Task<IEnumerable<UserDto>> GetAll()
    {
        return await db.Users
            .OrderBy(u => u.DisplayName)
            .Select(u => MapToDto(u))
            .ToListAsync();
    }

    // GET api/users/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        return user is null ? NotFound() : Ok(MapToDto(user));
    }

    // GET api/users/online
    [HttpGet("online")]
    public async Task<IEnumerable<UserDto>> GetOnline()
    {
        return await db.Users
            .Where(u => u.IsOnline)
            .OrderBy(u => u.DisplayName)
            .Select(u => MapToDto(u))
            .ToListAsync();
    }

    // POST api/users/register
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterUserRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username))
            return BadRequest("Username is required.");

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (existing is not null)
        {
            // Update presence on re-register
            existing.GrpcAddress = req.GrpcAddress;
            existing.IsOnline = true;
            existing.LastSeenAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Ok(MapToDto(existing));
        }

        var user = new User
        {
            Username = req.Username,
            DisplayName = req.DisplayName,
            GrpcAddress = req.GrpcAddress,
            IsOnline = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, MapToDto(user));
    }

    // PUT api/users/{id}/presence
    [HttpPut("{id:guid}/presence")]
    public async Task<IActionResult> UpdatePresence(Guid id, [FromBody] UpdatePresenceRequest req)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.IsOnline = req.IsOnline;
        user.GrpcAddress = req.GrpcAddress;
        user.LastSeenAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE api/users/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static UserDto MapToDto(User u) =>
        new(u.Id, u.Username, u.DisplayName, u.GrpcAddress, u.IsOnline, u.LastSeenAt);
}
