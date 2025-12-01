using SecretSantaBot.Models;

namespace SecretSantaBot.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByTelegramIdAsync(long telegramId);
    Task<User> CreateOrUpdateUserAsync(User user);
}

