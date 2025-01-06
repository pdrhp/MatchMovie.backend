namespace MatchMovie.Models.Entities;

public class Room
{
    public string Code { get; set; } = Guid.NewGuid().ToString("N")[..6].ToUpper();
    public string HostConnectionId { get; set; }
    public RoomSettings Settings { get; set; } = new();
    public List<string> ParticipantsConnectionIds { get; set; } = new();
    public RoomStatus Status { get; set; } = RoomStatus.WaitingToStart;
}

public enum RoomStatus
{
    WaitingToStart,
    InProgress,
    Finished
}