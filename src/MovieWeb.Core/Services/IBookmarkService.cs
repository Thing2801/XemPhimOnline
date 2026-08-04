using MovieWeb.Core.Models;

namespace MovieWeb.Core.Services;

public interface IBookmarkService
{
    Task<bool> IsBookmarkedAsync(string userId, string movieId);
    Task<bool> ToggleBookmarkAsync(string userId, string movieId);
    Task<List<string>> GetBookmarkedMovieIdsAsync(string userId);
    Task<List<Movie>> GetBookmarkedMoviesAsync(string userId, IMovieService movieService);
}
