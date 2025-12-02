using SecretSantaBot.Repositories;
using SecretSantaBot.Services;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация
var botToken = builder.Configuration["TelegramBot:BotToken"];
if (string.IsNullOrEmpty(botToken))
{
    throw new InvalidOperationException("TelegramBot:BotToken не настроен в appsettings.json");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection не настроен в appsettings.json");
}

// Регистрация Telegram Bot Client
builder.Services.AddSingleton<ITelegramBotClient>(provider =>
    new TelegramBotClient(botToken));

// Регистрация репозиториев
builder.Services.AddScoped<IRoomRepository>(provider => new RoomRepository(connectionString));
builder.Services.AddScoped<IParticipantRepository>(provider => new ParticipantRepository(connectionString));
builder.Services.AddScoped<IAssignmentRepository>(provider => new AssignmentRepository(connectionString));
builder.Services.AddScoped<IInvitationRepository>(provider => new InvitationRepository(connectionString));
builder.Services.AddScoped<IUserRepository>(provider => new UserRepository(connectionString));

// Регистрация сервисов
builder.Services.AddScoped<SecretSantaService>();

// Регистрация фонового сервиса бота
builder.Services.AddHostedService<TelegramBotService>();

var app = builder.Build();

// Health check endpoint
app.MapGet("/", () => new { status = "ok", service = "SecretSantaBot" });

app.Run();