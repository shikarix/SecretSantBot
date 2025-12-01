using SecretSantaBot.Models;

namespace SecretSantaBot.Repositories;

public interface IAssignmentRepository
{
    Task<Assignment?> GetAssignmentAsync(int roomId, long santaTelegramId);
    Task<List<Assignment>> GetAssignmentsByRoomAsync(int roomId);
    Task CreateAssignmentAsync(Assignment assignment);
    Task DeleteAssignmentsByRoomAsync(int roomId);
    Task<bool> HasAssignmentsAsync(int roomId);
}

