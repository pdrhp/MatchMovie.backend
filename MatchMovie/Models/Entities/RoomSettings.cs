namespace MatchMovie.Models.Entities;

public class RoomSettings
{
    public List<string> Categories { get; set; } = new();
    public int RoundDurationInMinutes { get; set; } = 3;
    public int MaxParticipants { get; set; } = 10;
}