using System.Text.RegularExpressions;

namespace ChatApp.Data.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigacne properties - EF Core cez ne robi join-y
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<RoomMember> RoomMemberships { get; set; } = [];
}