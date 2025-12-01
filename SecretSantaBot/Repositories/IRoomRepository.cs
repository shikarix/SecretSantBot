using SecretSantaBot.Models;

namespace SecretSantaBot.Repositories;

public interface IRoomRepository
{
    Task<Room?> GetRoomByIdAsync(int roomId);
    Task<Room?> GetRoomByCodeAsync(string code);
    Task<Room> CreateRoomAsync(Room room);
    Task UpdateRoomAsync(Room room);
    Task<List<Room>> GetRoomsByCreatorAsync(long creatorTelegramId);
    Task<List<Room>> GetRoomsByParticipantAsync(long telegramId);
}

