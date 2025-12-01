namespace SecretSantaBot.Models;

public class Assignment
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public long SantaTelegramId { get; set; } // Кто дарит
    public long RecipientTelegramId { get; set; } // Кому дарят
    public DateTime CreatedAt { get; set; }
}

