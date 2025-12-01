namespace SecretSantaBot.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long CreatorTelegramId { get; set; }
    public string CreatorUsername { get; set; } = string.Empty;
    public string? CreatorFirstName { get; set; }
    public string? Code { get; set; } // Уникальный код для приглашения
    public DateTime CreatedAt { get; set; }
    public DateTime? DrawDate { get; set; } // Дата розыгрыша
    public bool IsDrawn { get; set; } // Розыгрыш проведен
}

