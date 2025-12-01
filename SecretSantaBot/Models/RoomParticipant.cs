namespace SecretSantaBot.Models;

public class RoomParticipant
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public long TelegramId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? WishList { get; set; } // Список желаний
    public DateTime JoinedAt { get; set; }
}

