using MatchMovie.Interfaces;
using MatchMovie.Models.Entities;
using Microsoft.AspNetCore.SignalR;

namespace MatchMovie.Hubs;

public class MatchHub : Hub
{
    private readonly ILogger<MatchHub> _logger;
    private readonly IConnectionMapping _connections;
    private readonly IMovieAnalysisService _movieAnalysisService;
    private readonly IMetricsService _metrics;

    public MatchHub(
        ILogger<MatchHub> logger,
        IConnectionMapping connections, 
        IMovieAnalysisService movieAnalysisService,
        IMetricsService metrics)
    {
        _logger = logger;
        _connections = connections;
        _movieAnalysisService = movieAnalysisService;
        _metrics = metrics;
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
                    _metrics.RoomDeactivated();
                }
                else if (room.ParticipantsConnectionIds.Contains(Context.ConnectionId))
                {
                    room.ParticipantsConnectionIds.Remove(Context.ConnectionId);
                    room.ParticipantNames.Remove(Context.ConnectionId);
                    await _connections.UpdateRoom(room);
                
                    await Clients.Group(roomCode).SendAsync("ParticipantLeft", new
                    {
                        ParticipantCount = room.ParticipantsConnectionIds.Count,
                        ParticipantNames = room.ParticipantNames
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

    public async Task CreateRoom(string userName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                await Clients.Caller.SendAsync("Error", "Nome do usuário é obrigatório");
                return;
            }

            var room = new Room 
            { 
                HostConnectionId = Context.ConnectionId,
                Code = Guid.NewGuid().ToString("N")[..6].ToUpper()
            };

            room.ParticipantNames[Context.ConnectionId] = userName;
            await _connections.AddRoom(room);
            await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);
            
            _metrics.RoomCreated();
            
            await Clients.Caller.SendAsync("RoomCreated", new
            {
                room.Code,
                IsHost = true,
                UserName = userName
            });

            _logger.LogInformation("Room created: {RoomCode} by {UserName} ({ConnectionId})", 
                room.Code, userName, Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room");
            await Clients.Caller.SendAsync("Error", "Erro ao criar sala");
        }
    }

    public async Task JoinRoom(string roomCode, string userName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                await Clients.Caller.SendAsync("Error", "Nome do usuário é obrigatório");
                return;
            }

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
                room.ParticipantNames[Context.ConnectionId] = userName;
                await _connections.UpdateRoom(room);
                
                _metrics.ParticipantJoined();
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            
            await Clients.Group(roomCode).SendAsync("ParticipantJoined", new
            {
                ParticipantCount = room.ParticipantsConnectionIds.Count,
                IsHost = room.HostConnectionId == Context.ConnectionId,
                UserName = userName,
                ParticipantNames = room.ParticipantNames
            });

            await Clients.Caller.SendAsync("RoomJoined", room);
            
            _logger.LogInformation("User {UserName} ({ConnectionId}) joined room: {RoomCode}", 
                userName, Context.ConnectionId, roomCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining room {RoomCode}", roomCode);
            await Clients.Caller.SendAsync("Error", "Erro ao entrar na sala");
        }
    }

    public async Task VoteMovie(string roomCode, int movieId)
    {
        try
        {
            var room = await _connections.GetRoom(roomCode);
            if (room == null)
            {
                await Clients.Caller.SendAsync("Error", "Sala não encontrada");
                return;
            }
            
            if (room.Status != RoomStatus.InProgress)
            {
                await Clients.Caller.SendAsync("Error", "Votação encerrada");
                return;
            }

            if (!room.Movies.Any(m => m.Id == movieId))
            {
                await Clients.Caller.SendAsync("Error", "Filme não encontrado");
                return;
            }

            if (!room.ParticipantVotes.ContainsKey(Context.ConnectionId))
            {
                room.ParticipantVotes[Context.ConnectionId] = new List<int>();
            }

            if (!room.ParticipantVotes[Context.ConnectionId].Contains(movieId))
            {
                room.ParticipantVotes[Context.ConnectionId].Add(movieId);
                await _connections.UpdateRoom(room);
                
                _metrics.VoteRegistered();
                
                await Clients.Group(roomCode).SendAsync("MovieVoted", new
                {
                    ParticipantId = Context.ConnectionId,
                    MovieId = movieId,
                    ParticipantsVotes = room.ParticipantVotes
                });
                
                _logger.LogInformation("User {ConnectionId} voted for movie {MovieId} in room {RoomCode}", 
                    Context.ConnectionId, movieId, roomCode);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error voting movie in room {RoomCode}", roomCode);
            await Clients.Caller.SendAsync("Error", "Erro ao votar no filme");
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

            _metrics.RoomActivated();

            await Clients.Group(roomCode).SendAsync("MatchingStarted", room);
            
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

            if (settings.RoundDurationInSeconds is < 15 or > 300)
            {
                await Clients.Caller.SendAsync("Error", "Duração da rodada deve ser entre 10 segundos e 200 segundos");
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
                settings.RoundDurationInSeconds,
                settings.MaxParticipants
            });

            _logger.LogInformation("Room {RoomCode} configured with {Categories} categories and {Duration}s duration", 
                roomCode, settings.Categories.Count, settings.RoundDurationInSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring room {RoomCode}", roomCode);
            await Clients.Caller.SendAsync("Error", "Erro ao configurar sala");
        }
    }
    
    public async Task AddMoviesToRoom(string roomCode, List<Movie> movies)
    {
        var room = await _connections.GetRoom(roomCode);
        if (room == null)
        {
            await Clients.Caller.SendAsync("Error", "Sala não encontrada");
            return;
        }

        if (Context.ConnectionId != room.HostConnectionId)
        {
            await Clients.Caller.SendAsync("Error", "Apenas o host pode adicionar filmes");
            return;
        }

        room.Movies = movies;
        room.Status = RoomStatus.LoadingMovies;
        await _connections.UpdateRoom(room);
        
        _logger.LogInformation("Movies added to room {RoomCode}", roomCode);

        await Clients.Group(roomCode).SendAsync("MoviesLoading", movies.Count);
    }

    public async Task FinishRoom(string roomCode)
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
                await Clients.Caller.SendAsync("Error", "Apenas o host pode encerrar a sala");
                return;
            }

            if (room.Status != RoomStatus.InProgress)
            {
                return;
            }

            room.Status = RoomStatus.LoadingFinalizedData;

            await Clients.Group(roomCode).SendAsync("RoomAnalyzing", new {
                Status = RoomStatus.LoadingFinalizedData
            });

            var summaryAnalysis =
                await _movieAnalysisService.AnalyzeMoviesAsync(room.Movies, room.ParticipantVotes, room);
            
            room.AnalyzedRoom = summaryAnalysis;

            room.Status = RoomStatus.Finished;

            
            await _connections.UpdateRoom(room);

            _metrics.RoomFinished();

            var movieVotes = room.Movies.Select(movie => new
            {
                Movie = movie,
                VoteCount = room.ParticipantVotes.Values.Count(votes => votes.Contains(movie.Id))
            }).OrderByDescending(x => x.VoteCount);

            await Clients.Group(roomCode).SendAsync("RoomFinished", new
            {
                room.ParticipantVotes,
                MovieResults = movieVotes,
                TotalParticipants = room.ParticipantsConnectionIds.Count,
                AnalyzedRoom = summaryAnalysis
            });

            _logger.LogInformation("Room {RoomCode} finished by host {ConnectionId}", 
                roomCode, Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finishing room {RoomCode}", roomCode);
            await Clients.Caller.SendAsync("Error", "Erro ao encerrar a sala");
        }
    }
}