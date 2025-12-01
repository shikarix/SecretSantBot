namespace SecretSantaBot.Models;

public class Invitation
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public long InvitedTelegramId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsAccepted { get; set; }
}

