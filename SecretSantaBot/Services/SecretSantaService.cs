using SecretSantaBot.Models;
using SecretSantaBot.Repositories;

namespace SecretSantaBot.Services;

public class SecretSantaService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IInvitationRepository _invitationRepository;
    private readonly IUserRepository _userRepository;
    private readonly Random _random = new();

    public SecretSantaService(
        IRoomRepository roomRepository,
        IParticipantRepository participantRepository,
        IAssignmentRepository assignmentRepository,
        IInvitationRepository invitationRepository,
        IUserRepository userRepository)
    {
        _roomRepository = roomRepository;
        _participantRepository = participantRepository;
        _assignmentRepository = assignmentRepository;
        _invitationRepository = invitationRepository;
        _userRepository = userRepository;
    }

    public async Task<User> EnsureUserAsync(long telegramId, string username, string? firstName, string? lastName)
    {
        var user = new User
        {
            TelegramId = telegramId,
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            RegisteredAt = DateTime.UtcNow
        };
        return await _userRepository.CreateOrUpdateUserAsync(user);
    }

    public async Task<Room> CreateRoomAsync(string name, long creatorTelegramId, string creatorUsername, string? creatorFirstName)
    {
        var code = GenerateRoomCode();
        
        var room = new Room
        {
            Name = name,
            CreatorTelegramId = creatorTelegramId,
            CreatorUsername = creatorUsername,
            CreatorFirstName = creatorFirstName,
            Code = code,
            CreatedAt = DateTime.UtcNow,
            IsDrawn = false
        };

        var createdRoom = await _roomRepository.CreateRoomAsync(room);
        
        // Создатель автоматически становится участником
        await _participantRepository.AddParticipantAsync(new RoomParticipant
        {
            RoomId = createdRoom.Id,
            TelegramId = creatorTelegramId,
            Username = creatorUsername,
            FirstName = creatorFirstName,
            JoinedAt = DateTime.UtcNow
        });

        return createdRoom;
    }

    public async Task<string> CreateInvitationAsync(int roomId, long invitedTelegramId)
    {
        var room = await _roomRepository.GetRoomByIdAsync(roomId);
        if (room == null)
            throw new ArgumentException("Комната не найдена");

        var code = GenerateInvitationCode();
        var invitation = new Invitation
        {
            RoomId = roomId,
            InvitedTelegramId = invitedTelegramId,
            Code = code,
            CreatedAt = DateTime.UtcNow,
            IsAccepted = false
        };

        await _invitationRepository.CreateInvitationAsync(invitation);
        return code;
    }

    public async Task<bool> JoinRoomByCodeAsync(string code, long telegramId, string username, string? firstName)
    {
        var invitation = await _invitationRepository.GetInvitationByCodeAsync(code);
        if (invitation == null || invitation.IsAccepted)
            return false;

        if (invitation.InvitedTelegramId != telegramId)
            return false;

        var room = await _roomRepository.GetRoomByIdAsync(invitation.RoomId);
        if (room == null || room.IsDrawn)
            return false;

        var isAlreadyParticipant = await _participantRepository.IsParticipantAsync(room.Id, telegramId);
        if (isAlreadyParticipant)
            return false;

        await _participantRepository.AddParticipantAsync(new RoomParticipant
        {
            RoomId = room.Id,
            TelegramId = telegramId,
            Username = username,
            FirstName = firstName,
            JoinedAt = DateTime.UtcNow
        });

        invitation.IsAccepted = true;
        await _invitationRepository.UpdateInvitationAsync(invitation);

        return true;
    }

    public async Task<bool> JoinRoomByRoomCodeAsync(string roomCode, long telegramId, string username, string? firstName)
    {
        var room = await _roomRepository.GetRoomByCodeAsync(roomCode);
        if (room == null || room.IsDrawn)
            return false;

        var isAlreadyParticipant = await _participantRepository.IsParticipantAsync(room.Id, telegramId);
        if (isAlreadyParticipant)
            return false;

        await _participantRepository.AddParticipantAsync(new RoomParticipant
        {
            RoomId = room.Id,
            TelegramId = telegramId,
            Username = username,
            FirstName = firstName,
            JoinedAt = DateTime.UtcNow
        });

        return true;
    }

    public async Task<bool> DrawSecretSantaAsync(int roomId, long creatorTelegramId)
    {
        var room = await _roomRepository.GetRoomByIdAsync(roomId);
        if (room == null)
            return false;

        if (room.CreatorTelegramId != creatorTelegramId)
            return false;

        if (room.IsDrawn)
            return false;

        var participants = await _participantRepository.GetParticipantsByRoomAsync(roomId);
        if (participants.Count < 2)
            return false;

        // Удаляем предыдущие назначения если есть
        await _assignmentRepository.DeleteAssignmentsByRoomAsync(roomId);

        // Создаем список для перемешивания
        var shuffledParticipants = participants.OrderBy(_ => _random.Next()).ToList();
        
        // Распределяем участников по кругу
        for (int i = 0; i < shuffledParticipants.Count; i++)
        {
            var santa = shuffledParticipants[i];
            var recipient = shuffledParticipants[(i + 1) % shuffledParticipants.Count];

            var assignment = new Assignment
            {
                RoomId = roomId,
                SantaTelegramId = santa.TelegramId,
                RecipientTelegramId = recipient.TelegramId,
                CreatedAt = DateTime.UtcNow
            };

            await _assignmentRepository.CreateAssignmentAsync(assignment);
        }

        room.IsDrawn = true;
        room.DrawDate = DateTime.UtcNow;
        await _roomRepository.UpdateRoomAsync(room);

        return true;
    }

    public async Task<Assignment?> GetMyAssignmentAsync(int roomId, long telegramId)
    {
        return await _assignmentRepository.GetAssignmentAsync(roomId, telegramId);
    }

    private string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    private string GenerateInvitationCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}

