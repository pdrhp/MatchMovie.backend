namespace MatchMovie.Models.Entities;

public class Room
{
    public string Code { get; set; } = Guid.NewGuid().ToString("N")[..6].ToUpper();
    public string HostConnectionId { get; set; }
    public RoomSettings Settings { get; set; } = new();
    public List<string> ParticipantsConnectionIds { get; set; } = new();
    public RoomStatus Status { get; set; } = RoomStatus.WaitingToStart;
    public List<Movie> Movies { get; set; } = new();
    public Dictionary<string, List<int>> ParticipantVotes { get; set; } = new();
    public Dictionary<string, string> ParticipantNames { get; set; } = new();
}

public enum RoomStatus
{
    WaitingToStart,
    InProgress,
    LoadingMovies,
    Finished
}