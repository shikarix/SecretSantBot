using Dapper;
using Microsoft.Data.SqlClient;
using SecretSantaBot.Models;

namespace SecretSantaBot.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly string _connectionString;

    public RoomRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Room?> GetRoomByIdAsync(int roomId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, Name, CreatorTelegramId, CreatorUsername, CreatorFirstName, Code, CreatedAt, DrawDate, IsDrawn
            FROM Rooms
            WHERE Id = @RoomId";
        return await connection.QueryFirstOrDefaultAsync<Room>(sql, new { RoomId = roomId });
    }

    public async Task<Room?> GetRoomByCodeAsync(string code)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, Name, CreatorTelegramId, CreatorUsername, CreatorFirstName, Code, CreatedAt, DrawDate, IsDrawn
            FROM Rooms
            WHERE Code = @Code";
        return await connection.QueryFirstOrDefaultAsync<Room>(sql, new { Code = code });
    }

    public async Task<Room> CreateRoomAsync(Room room)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO Rooms (Name, CreatorTelegramId, CreatorUsername, CreatorFirstName, Code, CreatedAt, IsDrawn)
            OUTPUT INSERTED.Id, INSERTED.Name, INSERTED.CreatorTelegramId, INSERTED.CreatorUsername, 
                   INSERTED.CreatorFirstName, INSERTED.Code, INSERTED.CreatedAt, INSERTED.DrawDate, INSERTED.IsDrawn
            VALUES (@Name, @CreatorTelegramId, @CreatorUsername, @CreatorFirstName, @Code, @CreatedAt, @IsDrawn)";
        return await connection.QuerySingleAsync<Room>(sql, room);
    }

    public async Task UpdateRoomAsync(Room room)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            UPDATE Rooms
            SET Name = @Name, DrawDate = @DrawDate, IsDrawn = @IsDrawn
            WHERE Id = @Id";
        await connection.ExecuteAsync(sql, room);
    }

    public async Task<List<Room>> GetRoomsByCreatorAsync(long creatorTelegramId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, Name, CreatorTelegramId, CreatorUsername, CreatorFirstName, Code, CreatedAt, DrawDate, IsDrawn
            FROM Rooms
            WHERE CreatorTelegramId = @CreatorTelegramId
            ORDER BY CreatedAt DESC";
        var result = await connection.QueryAsync<Room>(sql, new { CreatorTelegramId = creatorTelegramId });
        return result.ToList();
    }

    public async Task<List<Room>> GetRoomsByParticipantAsync(long telegramId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT DISTINCT r.Id, r.Name, r.CreatorTelegramId, r.CreatorUsername, r.CreatorFirstName, 
                   r.Code, r.CreatedAt, r.DrawDate, r.IsDrawn
            FROM Rooms r
            INNER JOIN RoomParticipants rp ON r.Id = rp.RoomId
            WHERE rp.TelegramId = @TelegramId
            ORDER BY r.CreatedAt DESC";
        var result = await connection.QueryAsync<Room>(sql, new { TelegramId = telegramId });
        return result.ToList();
    }
}

