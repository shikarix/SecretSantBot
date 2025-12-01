using SecretSantaBot.Models;

namespace SecretSantaBot.Repositories;

public interface IParticipantRepository
{
    Task<RoomParticipant?> GetParticipantAsync(int roomId, long telegramId);
    Task<List<RoomParticipant>> GetParticipantsByRoomAsync(int roomId);
    Task<RoomParticipant> AddParticipantAsync(RoomParticipant participant);
    Task RemoveParticipantAsync(int roomId, long telegramId);
    Task UpdateParticipantAsync(RoomParticipant participant);
    Task<bool> IsParticipantAsync(int roomId, long telegramId);
}

