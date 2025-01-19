using MatchMovie.Interfaces;
using Prometheus;

namespace MatchMovie.Services;

public class MetricService : IMetricsService
{
    private readonly Counter _roomsCreated;
    private readonly Counter _roomsFinished;
    private readonly Counter _totalVotes;
    private readonly Gauge _activeRooms;
    private readonly Counter _participantsJoined;

    public MetricService()
    {
        _roomsCreated = Metrics.CreateCounter(
            "matchmovie_rooms_created_total",
            "Número total de salas criadas");
            
        _roomsFinished = Metrics.CreateCounter(
            "matchmovie_rooms_finished_total",
            "Número total de salas finalizadas");
            
        _totalVotes = Metrics.CreateCounter(
            "matchmovie_votes_total",
            "Número total de votos registrados");
            
        _activeRooms = Metrics.CreateGauge(
            "matchmovie_active_rooms",
            "Número atual de salas ativas");
            
        _participantsJoined = Metrics.CreateCounter(
            "matchmovie_participants_joined_total",
            "Número total de participantes que entraram nas salas");
    }

    public void RoomCreated() => _roomsCreated.Inc();

    public void RoomFinished() => _roomsFinished.Inc();

    public void VoteRegistered() => _totalVotes.Inc();

    public void RoomActivated() => _activeRooms.Inc();

    public void RoomDeactivated() => _activeRooms.Dec();

    public void ParticipantJoined() => _participantsJoined.Inc();
}