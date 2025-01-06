using MatchMovie.Interfaces;
using MatchMovie.Models.Entities;
using Microsoft.AspNetCore.SignalR;

namespace MatchMovie.Hubs;

public class MatchHub : Hub
{
    private readonly ILogger<MatchHub> _logger;
    private readonly IConnectionMapping _connections;

    public MatchHub(
        ILogger<MatchHub> logger,
        IConnectionMapping connections)
    {
        _logger = logger;
        _connections = connections;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var allRooms = await _connections.GetAllRoomCodes();
            foreach (var roomCode in allRooms)
            {
                var room = await _connections.GetRoom(roomCode);
                if (room == null) continue;

                if (room.HostConnectionId == Context.ConnectionId)
                {
                    await _connections.RemoveRoom(roomCode);
                    await Clients.Group(roomCode).SendAsync("RoomClosed", "O host saiu da sala");
                    _logger.LogInformation("Room {RoomCode} closed - Host disconnected", roomCode);
                }
                else if (room.ParticipantsConnectionIds.Contains(Context.ConnectionId))
                {
                    room.ParticipantsConnectionIds.Remove(Context.ConnectionId);
                    await _connections.UpdateRoom(room);
                
                    await Clients.Group(roomCode).SendAsync("ParticipantLeft", new
                    {
                        ParticipantCount = room.ParticipantsConnectionIds.Count
                    });
                
                    _logger.LogInformation("Participant left room {RoomCode}", roomCode);
                }

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling disconnection for {ConnectionId}", Context.ConnectionId);
        }
        finally
        {
            await base.OnDisconnectedAsync(exception);
        }
    }

    public async Task CreateRoom()
    {
        try
        {
            var room = new Room 
            { 
                HostConnectionId = Context.ConnectionId,
                Code = Guid.NewGuid().ToString("N")[..6].ToUpper()
            };

            await _connections.AddRoom(room);
            await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);
            
            await Clients.Caller.SendAsync("RoomCreated", new
            {
                room.Code,
                IsHost = true
            });

            _logger.LogInformation("Room created: {RoomCode} by {ConnectionId}", 
                room.Code, Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room");
            await Clients.Caller.SendAsync("Error", "Erro ao criar sala");
        }
    }

    public async Task JoinRoom(string roomCode)
    {
        try
        {
            var room = await _connections.GetRoom(roomCode);
            if (room == null)
            {
                await Clients.Caller.SendAsync("Error", "Sala não encontrada");
                return;
            }

            if (room.Status != RoomStatus.WaitingToStart)
            {
                await Clients.Caller.SendAsync("Error", "Esta sala já iniciou");
                return;
            }

            if (!room.ParticipantsConnectionIds.Contains(Context.ConnectionId))
            {
                room.ParticipantsConnectionIds.Add(Context.ConnectionId);
                await _connections.UpdateRoom(room);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            
            await Clients.Group(roomCode).SendAsync("ParticipantJoined", new
            {
                ParticipantCount = room.ParticipantsConnectionIds.Count,
                IsHost = room.HostConnectionId == Context.ConnectionId
            });

            _logger.LogInformation("User {ConnectionId} joined room: {RoomCode}", 
                Context.ConnectionId, roomCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining room {RoomCode}", roomCode);
            await Clients.Caller.SendAsync("Error", "Erro ao entrar na sala");
        }
    }

    public async Task StartMatching(string roomCode)
    {
        try
        {
            var room = await _connections.GetRoom(roomCode);
            if (room == null)
            {
                await Clients.Caller.SendAsync("Error", "Sala não encontrada");
                return;
            }

            if (Context.ConnectionId != room.HostConnectionId)
            {
                await Clients.Caller.SendAsync("Error", "Apenas o host pode iniciar");
                return;
            }

            room.Status = RoomStatus.InProgress;
            await _connections.UpdateRoom(room);

            await Clients.Group(roomCode).SendAsync("MatchingStarted");
            
            _logger.LogInformation("Matching started in room: {RoomCode}", roomCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting matching in room {RoomCode}", roomCode);
            await Clients.Caller.SendAsync("Error", "Erro ao iniciar matching");
        }
    }
    
    public async Task ConfigureRoom(string roomCode, RoomSettings settings)
    {
        try
        {
            var room = await _connections.GetRoom(roomCode);
            if (room == null)
            {
                await Clients.Caller.SendAsync("Error", "Sala não encontrada");
                return;
            }

            if (Context.ConnectionId != room.HostConnectionId)
            {
                await Clients.Caller.SendAsync("Error", "Apenas o host pode configurar a sala");
                return;
            }

            if (settings.RoundDurationInMinutes is < 1 or > 5)
            {
                await Clients.Caller.SendAsync("Error", "Duração da rodada deve ser entre 1 e 5 minutos");
                return;
            }

            if (!settings.Categories.Any())
            {
                await Clients.Caller.SendAsync("Error", "Selecione pelo menos uma categoria");
                return;
            }

            room.Settings = settings;
            await _connections.UpdateRoom(room);

            await Clients.Group(roomCode).SendAsync("RoomConfigured", new
            {
                settings.Categories,
                settings.RoundDurationInMinutes,
                settings.MaxParticipants
            });

            _logger.LogInformation("Room {RoomCode} configured with {Categories} categories and {Duration}min duration", 
                roomCode, settings.Categories.Count, settings.RoundDurationInMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring room {RoomCode}", roomCode);
            await Clients.Caller.SendAsync("Error", "Erro ao configurar sala");
        }
    }
}