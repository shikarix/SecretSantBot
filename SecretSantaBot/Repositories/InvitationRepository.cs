using Dapper;
using Microsoft.Data.SqlClient;
using SecretSantaBot.Models;

namespace SecretSantaBot.Repositories;

public class InvitationRepository : IInvitationRepository
{
    private readonly string _connectionString;

    public InvitationRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Invitation?> GetInvitationByCodeAsync(string code)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, RoomId, InvitedTelegramId, Code, CreatedAt, IsAccepted
            FROM Invitations
            WHERE Code = @Code";
        return await connection.QueryFirstOrDefaultAsync<Invitation>(sql, new { Code = code });
    }

    public async Task<List<Invitation>> GetInvitationsByRoomAsync(int roomId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, RoomId, InvitedTelegramId, Code, CreatedAt, IsAccepted
            FROM Invitations
            WHERE RoomId = @RoomId
            ORDER BY CreatedAt DESC";
        var result = await connection.QueryAsync<Invitation>(sql, new { RoomId = roomId });
        return result.ToList();
    }

    public async Task<Invitation> CreateInvitationAsync(Invitation invitation)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO Invitations (RoomId, InvitedTelegramId, Code, CreatedAt, IsAccepted)
            OUTPUT INSERTED.Id, INSERTED.RoomId, INSERTED.InvitedTelegramId, INSERTED.Code, 
                   INSERTED.CreatedAt, INSERTED.IsAccepted
            VALUES (@RoomId, @InvitedTelegramId, @Code, @CreatedAt, @IsAccepted)";
        return await connection.QuerySingleAsync<Invitation>(sql, invitation);
    }

    public async Task UpdateInvitationAsync(Invitation invitation)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            UPDATE Invitations
            SET IsAccepted = @IsAccepted
            WHERE Id = @Id";
        await connection.ExecuteAsync(sql, invitation);
    }

    public async Task DeleteInvitationAsync(int invitationId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"DELETE FROM Invitations WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = invitationId });
    }
}

