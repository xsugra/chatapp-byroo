using ChatApp.Data;
using ChatApp.Data.Entities;
using ChatApp.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController(ChatDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> GetRooms()
    {
        var rooms = await db.Rooms
            .Select(r => new RoomDto(
                r.Id,
                r.Name,
                r.Members.Count))
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpPost]
    public async Task<ActionResult<RoomDto>> CreateRoom(CreateRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Room name is required.");

        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetRooms),
            new RoomDto(room.Id, room.Name, 0));
    }
}