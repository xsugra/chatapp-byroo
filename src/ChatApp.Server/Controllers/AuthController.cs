using ChatApp.Data;
using ChatApp.Data.Entities;
using ChatApp.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ChatDbContext db) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest("Username is required.");

        var username = request.Username.Trim();

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        return Ok(new LoginResponse(user.Id, user.Username));
    }
}