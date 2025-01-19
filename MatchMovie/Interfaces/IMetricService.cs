namespace MatchMovie.Interfaces;

public interface IMetricsService
{
    void RoomCreated();
    void RoomFinished();
    void VoteRegistered();
    void RoomActivated();
    void RoomDeactivated();
    void ParticipantJoined();
}