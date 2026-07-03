namespace ChatApp.Data.Entities;

public class Message
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }

    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Foreign keys - hovoria - tato sprava patri tomuto pouzivatelovi v tejto room
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!; // null! = EF Core toto naplni, nie ja

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;
}