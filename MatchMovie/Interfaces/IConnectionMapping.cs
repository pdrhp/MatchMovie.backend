using MatchMovie.Models.Entities;

namespace MatchMovie.Interfaces;

public interface IConnectionMapping
{
    Task AddRoom(Room room);
    Task<Room> GetRoom(string code);
    Task UpdateRoom(Room room);
    Task RemoveRoom(string code);
    Task<IEnumerable<string>> GetAllRoomCodes();
}