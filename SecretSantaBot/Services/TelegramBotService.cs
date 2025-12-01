using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using SecretSantaBot.Models;
using SecretSantaBot.Repositories;

namespace SecretSantaBot.Services;

public class TelegramBotService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly SecretSantaService _secretSantaService;
    private readonly IRoomRepository _roomRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(
        ITelegramBotClient botClient,
        SecretSantaService secretSantaService,
        IRoomRepository roomRepository,
        IParticipantRepository participantRepository,
        ILogger<TelegramBotService> logger)
    {
        _botClient = botClient;
        _secretSantaService = secretSantaService;
        _roomRepository = roomRepository;
        _participantRepository = participantRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
        };

        var updateHandler = new DefaultUpdateHandler(
            HandleUpdateAsync,
            HandlePollingErrorAsync
        );

        _botClient.StartReceiving(
            updateHandler: updateHandler,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await _botClient.GetMe(stoppingToken);
        _logger.LogInformation("Бот @{BotUsername} запущен", me.Username);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long? chatId = null;
        long? userId = null;

        try
        {
            // Обработка CallbackQuery (нажатия на inline кнопки)
            if (update.CallbackQuery is { } callbackQuery)
            {
                await HandleCallbackQueryAsync(botClient, callbackQuery, cancellationToken);
                return;
            }

            // Обработка сообщений
            if (update.Message is not { } message)
                return;

            if (message.Text is not { } messageText)
                return;

            chatId = message.Chat.Id;
            userId = message.From!.Id;
            var username = message.From.Username ?? "Unknown";
            var firstName = message.From.FirstName;

            // Регистрируем пользователя
            await _secretSantaService.EnsureUserAsync(userId.Value, username, firstName, message.From.LastName);

            // Обработка команд
            if (messageText.StartsWith("/"))
            {
                await HandleCommandAsync(botClient, message, cancellationToken);
            }
            else if (messageText.StartsWith("JOIN "))
            {
                await HandleJoinCommandAsync(botClient, message, messageText, cancellationToken);
            }
            else if (messageText == "🏠 Мои комнаты" || messageText == "📋 Помощь" || messageText == "➕ Создать комнату" || messageText == "🎲 Мои назначения")
            {
                // Обработка нажатий на кнопки постоянного меню
                await HandleMenuButtonAsync(botClient, message, messageText, cancellationToken);
            }
            else
            {
                await botClient.SendMessage(
                    chatId: chatId.Value,
                    text: "Я не понимаю эту команду. Используйте кнопки меню или команду /help для списка доступных команд.",
                    replyMarkup: GetMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
            }
        }
        catch (ApiRequestException ex)
        {
            _logger.LogError(ex, "Ошибка Telegram API при обработке сообщения от пользователя {UserId}: {ErrorCode} - {Message}", 
                userId, ex.ErrorCode, ex.Message);
            if (chatId.HasValue)
            {
                try
                {
                    await botClient.SendMessage(
                        chatId: chatId.Value,
                        text: "Произошла ошибка при обработке вашего запроса. Попробуйте позже.",
                        cancellationToken: cancellationToken);
                }
                catch
                {
                    // Игнорируем ошибки при отправке сообщения об ошибке
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке сообщения от пользователя {UserId}", userId);
            if (chatId.HasValue)
            {
                try
                {
                    await botClient.SendMessage(
                        chatId: chatId.Value,
                        text: "Произошла ошибка при обработке вашего запроса. Попробуйте позже.",
                        cancellationToken: cancellationToken);
                }
                catch
                {
                    // Игнорируем ошибки при отправке сообщения об ошибке
                }
            }
        }
    }

    private async Task HandleCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var userId = message.From!.Id;
        var username = message.From.Username ?? "Unknown";
        var firstName = message.From.FirstName;
        var messageText = message.Text!;

        var command = messageText.Split(' ')[0].ToLower();

        switch (command)
        {
            case "/start":
                await HandleStartCommandAsync(botClient, chatId, cancellationToken);
                break;

            case "/help":
                await HandleHelpCommandAsync(botClient, chatId, cancellationToken);
                break;

            case "/createroom":
                await HandleCreateRoomCommandAsync(botClient, message, userId, username, firstName, cancellationToken);
                break;

            case "/myrooms":
                await HandleMyRoomsCommandAsync(botClient, chatId, userId, cancellationToken);
                break;

            case "/invite":
                await HandleInviteCommandAsync(botClient, message, userId, cancellationToken);
                break;

            case "/roominfo":
                await HandleRoomInfoCommandAsync(botClient, message, userId, cancellationToken);
                break;

            case "/draw":
                await HandleDrawCommandAsync(botClient, message, userId, cancellationToken);
                break;

            case "/myassignment":
                await HandleMyAssignmentCommandAsync(botClient, message, userId, cancellationToken);
                break;

            default:
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Неизвестная команда. Используйте /help для списка доступных команд.",
                    replyMarkup: GetMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                break;
        }
    }

    private async Task HandleStartCommandAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        const string text = """
            🎅 Добро пожаловать в бота Тайного Санты!
            
            Я помогу вам организовать игру в Тайного Санту.
            
            🎯 Как это работает:
            1. Создайте комнату
            2. Пригласите друзей
            3. Проведите розыгрыш
            4. Узнайте, кому вы дарите подарок!
            
            Используйте кнопки меню для навигации или команду /help для подробной информации.
            """;

        await botClient.SendMessage(
            chatId: chatId, 
            text: text, 
            replyMarkup: GetMainMenuKeyboard(),
            cancellationToken: cancellationToken);
    }

    private async Task HandleHelpCommandAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        const string text = """
            📋 Список доступных команд:
            
            /start - Начать работу с ботом
            /help - Показать эту справку
            
            🏠 Управление комнатами:
            /createroom <название> - Создать новую комнату для игры
            /myrooms - Показать список ваших комнат
            /roominfo <room_id> - Показать информацию о комнате и участниках
            
            🎫 Приглашения:
            /invite <room_id> - Создать код приглашения для добавления игрока
            JOIN <код> - Присоединиться к комнате по коду приглашения
            
            🎲 Розыгрыш:
            /draw <room_id> - Провести розыгрыш (только создатель комнаты)
            /myassignment <room_id> - Узнать, кому вы дарите подарок в этой комнате
            
            Примеры:
            /createroom Новогодний обмен
            /invite 1
            JOIN ABC123XYZ789
            /draw 1
            """;

        await botClient.SendMessage(
            chatId: chatId, 
            text: text,
            replyMarkup: GetMainMenuKeyboard(),
            cancellationToken: cancellationToken);
    }

    private async Task HandleCreateRoomCommandAsync(ITelegramBotClient botClient, Message message, long userId, string username, string? firstName, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var parts = message.Text!.Split(' ', 2);
        
        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Пожалуйста, укажите название комнаты.\nПример: /createroom Новогодний обмен",
                replyMarkup: GetMainMenuKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        var roomName = parts[1].Trim();
        var room = await _secretSantaService.CreateRoomAsync(roomName, userId, username, firstName);

        var text = $"""
            ✅ Комната "{room.Name}" успешно создана!
            
            ID комнаты: {room.Id}
            Код комнаты: {room.Code}
            
            Используйте кнопки ниже для управления комнатой или поделитесь кодом {room.Code} с участниками
            """;

        var keyboard = GetRoomInlineKeyboard(room.Id, true, false);
        await botClient.SendMessage(
            chatId: chatId, 
            text: text,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private async Task HandleMyRoomsCommandAsync(ITelegramBotClient botClient, long chatId, long userId, CancellationToken cancellationToken)
    {
        var createdRooms = await _roomRepository.GetRoomsByCreatorAsync(userId);
        var participantRooms = await _roomRepository.GetRoomsByParticipantAsync(userId);

        if (createdRooms.Count == 0 && participantRooms.Count == 0)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "У вас пока нет комнат. Создайте новую комнату кнопкой \"➕ Создать комнату\" или командой /createroom",
                replyMarkup: GetMainMenuKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        // Объединяем все комнаты
        var allRooms = createdRooms.Concat(participantRooms.Where(r => r.CreatorTelegramId != userId))
            .DistinctBy(r => r.Id)
            .ToList();

        var keyboard = GetRoomsListKeyboard(allRooms);
        if (keyboard == null)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Комнаты не найдены.",
                replyMarkup: GetMainMenuKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        var text = "🏠 Выберите комнату для управления:";

        await botClient.SendMessage(
            chatId: chatId, 
            text: text,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private async Task HandleInviteCommandAsync(ITelegramBotClient botClient, Message message, long userId, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var parts = message.Text!.Split(' ');

        if (parts.Length < 2 || !int.TryParse(parts[1], out var roomId))
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Пожалуйста, укажите ID комнаты.\nПример: /invite 1",
                cancellationToken: cancellationToken);
            return;
        }

        var room = await _roomRepository.GetRoomByIdAsync(roomId);
        if (room == null)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Комната не найдена.",
                cancellationToken: cancellationToken);
            return;
        }

        if (room.CreatorTelegramId != userId)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Только создатель комнаты может создавать приглашения.",
                cancellationToken: cancellationToken);
            return;
        }

        // Создаем приглашение для конкретного пользователя (можно расширить)
        var code = await _secretSantaService.CreateInvitationAsync(roomId, userId);
        
        var text = $"""
            🎫 Приглашение создано!
            
            Код приглашения: {code}
            Комната: {room.Name}
            
            Поделитесь этим кодом с участниками. Они смогут присоединиться командой:
            JOIN {code}
            
            Также можно использовать код комнаты: {room.Code}
            """;

        await botClient.SendMessage(chatId: chatId, text: text, cancellationToken: cancellationToken);
    }

    private async Task HandleRoomInfoCommandAsync(ITelegramBotClient botClient, Message message, long userId, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var parts = message.Text!.Split(' ');

        if (parts.Length < 2 || !int.TryParse(parts[1], out var roomId))
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Пожалуйста, укажите ID комнаты.\nПример: /roominfo 1",
                cancellationToken: cancellationToken);
            return;
        }

        var room = await _roomRepository.GetRoomByIdAsync(roomId);
        if (room == null)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Комната не найдена.",
                cancellationToken: cancellationToken);
            return;
        }

        var isParticipant = await _participantRepository.IsParticipantAsync(roomId, userId);
        if (!isParticipant && room.CreatorTelegramId != userId)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Вы не являетесь участником этой комнаты.",
                cancellationToken: cancellationToken);
            return;
        }

        var participants = await _participantRepository.GetParticipantsByRoomAsync(roomId);
        var status = room.IsDrawn ? "✅ Розыгрыш проведен" : "⏳ Ожидание участников";

        var text = $"""
            🏠 Комната: {room.Name}
            
            Статус: {status}
            Код комнаты: {room.Code}
            Участников: {participants.Count}
            
            👥 Участники:
            """;

        foreach (var participant in participants)
        {
            text += $"• {participant.FirstName ?? participant.Username}\n";
        }

        if (room.CreatorTelegramId == userId && !room.IsDrawn && participants.Count >= 2)
        {
            text += $"\nИспользуйте /draw {roomId} для проведения розыгрыша";
        }

        await botClient.SendMessage(chatId: chatId, text: text, cancellationToken: cancellationToken);
    }

    private async Task HandleDrawCommandAsync(ITelegramBotClient botClient, Message message, long userId, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var parts = message.Text!.Split(' ');

        if (parts.Length < 2 || !int.TryParse(parts[1], out var roomId))
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Пожалуйста, укажите ID комнаты.\nПример: /draw 1",
                cancellationToken: cancellationToken);
            return;
        }

        var success = await _secretSantaService.DrawSecretSantaAsync(roomId, userId);
        if (!success)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Не удалось провести розыгрыш. Проверьте, что вы создатель комнаты, в комнате есть минимум 2 участника и розыгрыш еще не был проведен.",
                cancellationToken: cancellationToken);
            return;
        }

        var participants = await _participantRepository.GetParticipantsByRoomAsync(roomId);
        var room = await _roomRepository.GetRoomByIdAsync(roomId);
        var roomName = room?.Name ?? "комната";
        
        var text = $"""
            🎲 Розыгрыш проведен!
            
            В комнате участвует {participants.Count} человек(а).
            Теперь каждый участник может узнать, кому он дарит подарок, командой:
            /myassignment {roomId}
            """;

        await botClient.SendMessage(chatId: chatId, text: text, cancellationToken: cancellationToken);

        // Отправляем уведомление каждому участнику
        foreach (var participant in participants)
        {
            if (participant.TelegramId != userId)
            {
                try
                {
                    await botClient.SendTextMessageAsync(
                        chatId: participant.TelegramId,
                        text: $"🎅 Розыгрыш в комнате '{roomName}' проведен! Используйте /myassignment {roomId} чтобы узнать, кому вы дарите подарок.",
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось отправить сообщение участнику {TelegramId}", participant.TelegramId);
                }
            }
        }
    }

    private async Task HandleMyAssignmentCommandAsync(ITelegramBotClient botClient, Message message, long userId, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var parts = message.Text!.Split(' ');

        if (parts.Length < 2 || !int.TryParse(parts[1], out var roomId))
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Пожалуйста, укажите ID комнаты.\nПример: /myassignment 1",
                cancellationToken: cancellationToken);
            return;
        }

        var assignment = await _secretSantaService.GetMyAssignmentAsync(roomId, userId);
        if (assignment == null)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Розыгрыш в этой комнате еще не был проведен или вы не являетесь участником.",
                cancellationToken: cancellationToken);
            return;
        }

        var recipient = await _participantRepository.GetParticipantAsync(roomId, assignment.RecipientTelegramId);
        if (recipient == null)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Информация о получателе не найдена.",
                cancellationToken: cancellationToken);
            return;
        }

        var recipientName = recipient.FirstName ?? recipient.Username;
        var wishList = string.IsNullOrEmpty(recipient.WishList) 
            ? "Список желаний не указан" 
            : $"📝 Список желаний:\n{recipient.WishList}";

        var text = $"""
            🎁 Вы дарите подарок:
            
            Получатель: {recipientName}
            {wishList}
            
            Удачи в выборе подарка! 🎅
            """;

        await botClient.SendMessage(chatId: chatId, text: text, cancellationToken: cancellationToken);
    }

    private async Task HandleJoinCommandAsync(ITelegramBotClient botClient, Message message, string messageText, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var userId = message.From!.Id;
        var username = message.From.Username ?? "Unknown";
        var firstName = message.From.FirstName;

        var parts = messageText.Split(' ', 2);
        if (parts.Length < 2)
        {
        await botClient.SendMessage(
            chatId: chatId,
            text: "Пожалуйста, укажите код приглашения.\nПример: JOIN ABC123XYZ789",
            cancellationToken: cancellationToken);
            return;
        }

        var code = parts[1].Trim();

        // Пробуем присоединиться по коду приглашения
        var success = await _secretSantaService.JoinRoomByCodeAsync(code, userId, username, firstName);
        
        if (!success)
        {
            // Если не получилось по коду приглашения, пробуем по коду комнаты
            success = await _secretSantaService.JoinRoomByRoomCodeAsync(code, userId, username, firstName);
        }

        if (!success)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Не удалось присоединиться. Проверьте код или убедитесь, что приглашение не было использовано.",
                cancellationToken: cancellationToken);
            return;
        }

        await botClient.SendMessage(
            chatId: chatId,
            text: "✅ Вы успешно присоединились к комнате! Используйте кнопку \"🏠 Мои комнаты\" чтобы увидеть свои комнаты.",
            replyMarkup: GetMainMenuKeyboard(),
            cancellationToken: cancellationToken);
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogError(exception, "Ошибка при работе с Telegram API: {ErrorMessage}", errorMessage);
        return Task.CompletedTask;
    }

    #region Keyboard Methods

    private ReplyKeyboardMarkup GetMainMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "🏠 Мои комнаты", "➕ Создать комнату" },
            new KeyboardButton[] { "🎲 Мои назначения", "📋 Помощь" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };
    }

    private InlineKeyboardMarkup GetRoomInlineKeyboard(int roomId, bool isCreator, bool isDrawn)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("ℹ️ Информация", $"room_info:{roomId}")
        });

        if (!isDrawn)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("🎫 Пригласить", $"room_invite:{roomId}")
            });

            if (isCreator)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("🎲 Провести розыгрыш", $"room_draw:{roomId}")
                });
            }
        }
        else
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("🎁 Моё назначение", $"room_assignment:{roomId}")
            });
        }

        return new InlineKeyboardMarkup(buttons);
    }

    private InlineKeyboardMarkup GetRoomsListKeyboard(List<Room> rooms)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var room in rooms.Take(10)) // Ограничиваем 10 комнатами из-за лимита Telegram
        {
            var roomName = room.Name.Length > 30 ? room.Name.Substring(0, 30) + "..." : room.Name;
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{(room.IsDrawn ? "✅" : "⏳")} {roomName}",
                    $"room_select:{room.Id}")
            });
        }

        if (buttons.Count == 0)
        {
            return null;
        }

        return new InlineKeyboardMarkup(buttons);
    }

    #endregion

    #region Callback Query Handler

    private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message!.Chat.Id;
        var userId = callbackQuery.From.Id;
        var data = callbackQuery.Data;

        if (string.IsNullOrEmpty(data))
            return;

        try
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

            var parts = data.Split(':');
            if (parts.Length < 2)
                return;

            var action = parts[0];
            var roomIdStr = parts[1];

            if (!int.TryParse(roomIdStr, out var roomId))
                return;

            switch (action)
            {
                case "room_info":
                    await HandleRoomInfoCallbackAsync(botClient, chatId, userId, roomId, cancellationToken);
                    break;

                case "room_invite":
                    await HandleRoomInviteCallbackAsync(botClient, chatId, userId, roomId, cancellationToken);
                    break;

                case "room_draw":
                    await HandleRoomDrawCallbackAsync(botClient, chatId, userId, roomId, cancellationToken);
                    break;

                case "room_assignment":
                    await HandleRoomAssignmentCallbackAsync(botClient, chatId, userId, roomId, cancellationToken);
                    break;

                case "room_select":
                    await HandleRoomSelectCallbackAsync(botClient, chatId, userId, roomId, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке CallbackQuery: {Data}", data);
            try
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Произошла ошибка. Попробуйте еще раз.",
                    cancellationToken: cancellationToken);
            }
            catch { }
        }
    }

    private async Task HandleRoomInfoCallbackAsync(ITelegramBotClient botClient, long chatId, long userId, int roomId, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetRoomByIdAsync(roomId);
        if (room == null)
        {
            await botClient.SendMessage(chatId: chatId, text: "Комната не найдена.", cancellationToken: cancellationToken);
            return;
        }

        var isParticipant = await _participantRepository.IsParticipantAsync(roomId, userId);
        if (!isParticipant && room.CreatorTelegramId != userId)
        {
            await botClient.SendMessage(chatId: chatId, text: "Вы не являетесь участником этой комнаты.", cancellationToken: cancellationToken);
            return;
        }

        var participants = await _participantRepository.GetParticipantsByRoomAsync(roomId);
        var status = room.IsDrawn ? "✅ Розыгрыш проведен" : "⏳ Ожидание участников";

        var text = $"""
            🏠 Комната: {room.Name}
            
            Статус: {status}
            Код комнаты: {room.Code}
            Участников: {participants.Count}
            
            👥 Участники:
            """;

        foreach (var participant in participants)
        {
            text += $"• {participant.FirstName ?? participant.Username}\n";
        }

        var isCreator = room.CreatorTelegramId == userId;
        var keyboard = GetRoomInlineKeyboard(roomId, isCreator, room.IsDrawn);

        await botClient.SendMessage(
            chatId: chatId,
            text: text,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private async Task HandleRoomInviteCallbackAsync(ITelegramBotClient botClient, long chatId, long userId, int roomId, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetRoomByIdAsync(roomId);
        if (room == null || room.CreatorTelegramId != userId)
        {
            await botClient.SendMessage(chatId: chatId, text: "Только создатель комнаты может создавать приглашения.", cancellationToken: cancellationToken);
            return;
        }

        var code = await _secretSantaService.CreateInvitationAsync(roomId, userId);
        
        var text = $"""
            🎫 Приглашение создано!
            
            Код приглашения: {code}
            Комната: {room.Name}
            
            Поделитесь этим кодом с участниками. Они смогут присоединиться командой:
            JOIN {code}
            
            Также можно использовать код комнаты: {room.Code}
            """;

        await botClient.SendMessage(chatId: chatId, text: text, cancellationToken: cancellationToken);
    }

    private async Task HandleRoomDrawCallbackAsync(ITelegramBotClient botClient, long chatId, long userId, int roomId, CancellationToken cancellationToken)
    {
        var success = await _secretSantaService.DrawSecretSantaAsync(roomId, userId);
        if (!success)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Не удалось провести розыгрыш. Проверьте, что в комнате есть минимум 2 участника и розыгрыш еще не был проведен.",
                cancellationToken: cancellationToken);
            return;
        }

        var participants = await _participantRepository.GetParticipantsByRoomAsync(roomId);
        var room = await _roomRepository.GetRoomByIdAsync(roomId);
        var roomName = room?.Name ?? "комната";
        
        var text = $"""
            🎲 Розыгрыш проведен!
            
            В комнате участвует {participants.Count} человек(а).
            Теперь каждый участник может узнать, кому он дарит подарок, используя кнопку "🎁 Моё назначение".
            """;

        await botClient.SendMessage(chatId: chatId, text: text, cancellationToken: cancellationToken);

        // Отправляем уведомление каждому участнику
        foreach (var participant in participants)
        {
            if (participant.TelegramId != userId)
            {
                try
                {
                    var keyboard = GetRoomInlineKeyboard(roomId, false, true);
                    await botClient.SendMessage(
                        chatId: participant.TelegramId,
                        text: $"🎅 Розыгрыш в комнате '{roomName}' проведен! Используйте кнопку ниже, чтобы узнать, кому вы дарите подарок.",
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось отправить сообщение участнику {TelegramId}", participant.TelegramId);
                }
            }
        }
    }

    private async Task HandleRoomAssignmentCallbackAsync(ITelegramBotClient botClient, long chatId, long userId, int roomId, CancellationToken cancellationToken)
    {
        var assignment = await _secretSantaService.GetMyAssignmentAsync(roomId, userId);
        if (assignment == null)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Розыгрыш в этой комнате еще не был проведен или вы не являетесь участником.",
                cancellationToken: cancellationToken);
            return;
        }

        var recipient = await _participantRepository.GetParticipantAsync(roomId, assignment.RecipientTelegramId);
        if (recipient == null)
        {
            await botClient.SendMessage(chatId: chatId, text: "Информация о получателе не найдена.", cancellationToken: cancellationToken);
            return;
        }

        var recipientName = recipient.FirstName ?? recipient.Username;
        var wishList = string.IsNullOrEmpty(recipient.WishList) 
            ? "Список желаний не указан" 
            : $"📝 Список желаний:\n{recipient.WishList}";

        var text = $"""
            🎁 Вы дарите подарок:
            
            Получатель: {recipientName}
            {wishList}
            
            Удачи в выборе подарка! 🎅
            """;

        await botClient.SendMessage(chatId: chatId, text: text, cancellationToken: cancellationToken);
    }

    private async Task HandleRoomSelectCallbackAsync(ITelegramBotClient botClient, long chatId, long userId, int roomId, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetRoomByIdAsync(roomId);
        if (room == null)
        {
            await botClient.SendMessage(chatId: chatId, text: "Комната не найдена.", cancellationToken: cancellationToken);
            return;
        }

        var isCreator = room.CreatorTelegramId == userId;
        var keyboard = GetRoomInlineKeyboard(roomId, isCreator, room.IsDrawn);

        var text = $"🏠 Комната: {room.Name}\n\nВыберите действие:";

        await botClient.SendMessage(
            chatId: chatId,
            text: text,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    #endregion

    #region Menu Button Handler

    private async Task HandleMenuButtonAsync(ITelegramBotClient botClient, Message message, string buttonText, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var userId = message.From!.Id;

        switch (buttonText)
        {
            case "🏠 Мои комнаты":
                await HandleMyRoomsCommandAsync(botClient, chatId, userId, cancellationToken);
                break;

            case "➕ Создать комнату":
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Чтобы создать комнату, используйте команду:\n/createroom <название>\n\nНапример: /createroom Новогодний обмен",
                    replyMarkup: GetMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                break;

            case "🎲 Мои назначения":
                await HandleMyAssignmentsMenuAsync(botClient, chatId, userId, cancellationToken);
                break;

            case "📋 Помощь":
                await HandleHelpCommandAsync(botClient, chatId, cancellationToken);
                break;
        }
    }

    private async Task HandleMyAssignmentsMenuAsync(ITelegramBotClient botClient, long chatId, long userId, CancellationToken cancellationToken)
    {
        var participantRooms = await _roomRepository.GetRoomsByParticipantAsync(userId);
        var drawnRooms = participantRooms.Where(r => r.IsDrawn).ToList();

        if (drawnRooms.Count == 0)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "У вас пока нет комнат с проведенным розыгрышем. После проведения розыгрыша здесь появится список ваших назначений.",
                replyMarkup: GetMainMenuKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        var keyboard = GetRoomsListKeyboard(drawnRooms);
        if (keyboard == null)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Комнаты не найдены.",
                replyMarkup: GetMainMenuKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        await botClient.SendMessage(
            chatId: chatId,
            text: "🎁 Выберите комнату, чтобы узнать, кому вы дарите подарок:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    #endregion

}

