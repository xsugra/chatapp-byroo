using ChatApp.Data;
using ChatApp.Data.Entities;
using ChatApp.Server.Hubs;
using ChatApp.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace ChatApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController(ChatDbContext db, IHubContext<ChatHub> hubContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> GetRooms()
    {
        var rooms = await db.Rooms
            .Select(r => new RoomDto(
                r.Id,
                r.Name,
                r.Members.Count,
                r.CreatedByUserId))
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpPost]
    public async Task<ActionResult<RoomDto>> CreateRoom(CreateRoomRequest request, [FromQuery] Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Room name is required.");

        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        db.Rooms.Add(room);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyError(ex))
        {
            return Conflict("A room with this name already exists.");
        }

        return CreatedAtAction(
            nameof(GetRooms),
            new RoomDto(room.Id, room.Name, 0, room.CreatedByUserId));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<RoomDto>> RenameRoom(Guid id, RenameRoomRequest request, [FromQuery] Guid userId)
    {
        var room = await db.Rooms.FindAsync(id);
        if (room is null) return NotFound();

        if (room.CreatedByUserId != userId)
            return StatusCode(403, "Only the room's creator can rename it.");

        if (string.IsNullOrWhiteSpace(request.NewName))
            return BadRequest("Room name is required.");

        room.Name = request.NewName.Trim();

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyError(ex))
        {
            return Conflict("A room with this name already exists.");
        }

        var memberCount = await db.RoomMembers.CountAsync(rm => rm.RoomId == id);
        var dto = new RoomDto(room.Id, room.Name, memberCount, room.CreatedByUserId);

        await hubContext.Clients.Group(id.ToString()).SendAsync("RoomRenamed", dto);

        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRoom(Guid id, [FromQuery] Guid userId)
    {
        var room = await db.Rooms.FindAsync(id);
        if (room is null) return NotFound();

        if (room.CreatedByUserId != userId)
            return StatusCode(403, "Only the room's creator can delete it.");

        await hubContext.Clients.Group(id.ToString()).SendAsync("RoomDeleted", id);

        db.Rooms.Remove(room);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private static bool IsDuplicateKeyError(DbUpdateException ex) =>
        ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry };
}
