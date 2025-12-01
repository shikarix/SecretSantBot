using SecretSantaBot.Models;

namespace SecretSantaBot.Repositories;

public interface IInvitationRepository
{
    Task<Invitation?> GetInvitationByCodeAsync(string code);
    Task<List<Invitation>> GetInvitationsByRoomAsync(int roomId);
    Task<Invitation> CreateInvitationAsync(Invitation invitation);
    Task UpdateInvitationAsync(Invitation invitation);
    Task DeleteInvitationAsync(int invitationId);
}

