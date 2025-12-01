using Dapper;
using Microsoft.Data.SqlClient;
using SecretSantaBot.Models;

namespace SecretSantaBot.Repositories;

public class ParticipantRepository : IParticipantRepository
{
    private readonly string _connectionString;

    public ParticipantRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<RoomParticipant?> GetParticipantAsync(int roomId, long telegramId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, RoomId, TelegramId, Username, FirstName, WishList, JoinedAt
            FROM RoomParticipants
            WHERE RoomId = @RoomId AND TelegramId = @TelegramId";
        return await connection.QueryFirstOrDefaultAsync<RoomParticipant>(sql, new { RoomId = roomId, TelegramId = telegramId });
    }

    public async Task<List<RoomParticipant>> GetParticipantsByRoomAsync(int roomId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, RoomId, TelegramId, Username, FirstName, WishList, JoinedAt
            FROM RoomParticipants
            WHERE RoomId = @RoomId
            ORDER BY JoinedAt";
        var result = await connection.QueryAsync<RoomParticipant>(sql, new { RoomId = roomId });
        return result.ToList();
    }

    public async Task<RoomParticipant> AddParticipantAsync(RoomParticipant participant)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO RoomParticipants (RoomId, TelegramId, Username, FirstName, WishList, JoinedAt)
            OUTPUT INSERTED.Id, INSERTED.RoomId, INSERTED.TelegramId, INSERTED.Username, 
                   INSERTED.FirstName, INSERTED.WishList, INSERTED.JoinedAt
            VALUES (@RoomId, @TelegramId, @Username, @FirstName, @WishList, @JoinedAt)";
        return await connection.QuerySingleAsync<RoomParticipant>(sql, participant);
    }

    public async Task RemoveParticipantAsync(int roomId, long telegramId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            DELETE FROM RoomParticipants
            WHERE RoomId = @RoomId AND TelegramId = @TelegramId";
        await connection.ExecuteAsync(sql, new { RoomId = roomId, TelegramId = telegramId });
    }

    public async Task UpdateParticipantAsync(RoomParticipant participant)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            UPDATE RoomParticipants
            SET Username = @Username, FirstName = @FirstName, WishList = @WishList
            WHERE Id = @Id";
        await connection.ExecuteAsync(sql, participant);
    }

    public async Task<bool> IsParticipantAsync(int roomId, long telegramId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT COUNT(1)
            FROM RoomParticipants
            WHERE RoomId = @RoomId AND TelegramId = @TelegramId";
        var count = await connection.QuerySingleAsync<int>(sql, new { RoomId = roomId, TelegramId = telegramId });
        return count > 0;
    }
}

