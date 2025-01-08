namespace MatchMovie.Models.Entities;

public class RoomSettings
{
    public List<string> Categories { get; set; } = new();
    public int RoundDurationInSeconds { get; set; } = 60;
    public int MaxParticipants { get; set; } = 10;
}