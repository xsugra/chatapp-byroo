namespace ChatApp.Data.Entities;

public class Room
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<RoomMember>  Members { get; set; } = [];
}