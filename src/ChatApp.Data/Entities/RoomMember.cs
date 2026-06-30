namespace ChatApp.Data.Entities;

public class RoomMember
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public DateTime JoinedAt { get; set; }
}