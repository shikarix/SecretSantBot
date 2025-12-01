using Dapper;
using Microsoft.Data.SqlClient;
using SecretSantaBot.Models;

namespace SecretSantaBot.Repositories;

public class AssignmentRepository : IAssignmentRepository
{
    private readonly string _connectionString;

    public AssignmentRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Assignment?> GetAssignmentAsync(int roomId, long santaTelegramId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, RoomId, SantaTelegramId, RecipientTelegramId, CreatedAt
            FROM Assignments
            WHERE RoomId = @RoomId AND SantaTelegramId = @SantaTelegramId";
        return await connection.QueryFirstOrDefaultAsync<Assignment>(sql, new { RoomId = roomId, SantaTelegramId = santaTelegramId });
    }

    public async Task<List<Assignment>> GetAssignmentsByRoomAsync(int roomId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, RoomId, SantaTelegramId, RecipientTelegramId, CreatedAt
            FROM Assignments
            WHERE RoomId = @RoomId";
        var result = await connection.QueryAsync<Assignment>(sql, new { RoomId = roomId });
        return result.ToList();
    }

    public async Task CreateAssignmentAsync(Assignment assignment)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO Assignments (RoomId, SantaTelegramId, RecipientTelegramId, CreatedAt)
            VALUES (@RoomId, @SantaTelegramId, @RecipientTelegramId, @CreatedAt)";
        await connection.ExecuteAsync(sql, assignment);
    }

    public async Task DeleteAssignmentsByRoomAsync(int roomId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"DELETE FROM Assignments WHERE RoomId = @RoomId";
        await connection.ExecuteAsync(sql, new { RoomId = roomId });
    }

    public async Task<bool> HasAssignmentsAsync(int roomId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"SELECT COUNT(1) FROM Assignments WHERE RoomId = @RoomId";
        var count = await connection.QuerySingleAsync<int>(sql, new { RoomId = roomId });
        return count > 0;
    }
}

