using Dapper;
using Microsoft.Data.SqlClient;
using SecretSantaBot.Models;

namespace SecretSantaBot.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<User?> GetUserByTelegramIdAsync(long telegramId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, TelegramId, Username, FirstName, LastName, RegisteredAt
            FROM Users
            WHERE TelegramId = @TelegramId";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { TelegramId = telegramId });
    }

    public async Task<User> CreateOrUpdateUserAsync(User user)
    {
        await using var connection = new SqlConnection(_connectionString);
        
        var existing = await GetUserByTelegramIdAsync(user.TelegramId);
        if (existing != null)
        {
            // Обновляем информацию о пользователе
            const string updateSql = @"
                UPDATE Users
                SET Username = @Username, FirstName = @FirstName, LastName = @LastName
                WHERE TelegramId = @TelegramId
                
                SELECT Id, TelegramId, Username, FirstName, LastName, RegisteredAt
                FROM Users
                WHERE TelegramId = @TelegramId";
            return await connection.QuerySingleAsync<User>(updateSql, user);
        }
        else
        {
            // Создаем нового пользователя
            const string insertSql = @"
                INSERT INTO Users (TelegramId, Username, FirstName, LastName, RegisteredAt)
                OUTPUT INSERTED.Id, INSERTED.TelegramId, INSERTED.Username, 
                       INSERTED.FirstName, INSERTED.LastName, INSERTED.RegisteredAt
                VALUES (@TelegramId, @Username, @FirstName, @LastName, @RegisteredAt)";
            return await connection.QuerySingleAsync<User>(insertSql, user);
        }
    }
}

