using MatchMovie.Models.Entities;

namespace MatchMovie.Interfaces;

public interface IMovieAnalysisService
{
    Task<MovieAnalysisDto> AnalyzeMoviesAsync(List<Movie> movies, Dictionary<string, List<int>> votes, Room room);
}