using MovieWeb.Core.Models;

namespace MovieWeb.Core.Services;

public interface IMovieService
{
    Task<List<Movie>> GetFeaturedMoviesAsync(int count = 5);
    Task<List<Movie>> GetTrendingMoviesAsync(int count = 10);
    Task<List<Movie>> GetMoviesByGenreAsync(string genreId, int count = 10);
    Task<List<Genre>> GetAllGenresAsync();
    Task<Genre?> GetGenreByIdOrSlugAsync(string identifier);
    Task<List<Movie>> GetMoviesAsync(
        string? searchKeyword = null, 
        string? genreId = null, 
        string? country = null, 
        int? year = null, 
        bool? isSeries = null, 
        string? sortBy = null, 
        int page = 1, 
        int pageSize = 12);
    Task<int> GetMoviesCountAsync(
        string? searchKeyword = null, 
        string? genreId = null, 
        string? country = null, 
        int? year = null, 
        bool? isSeries = null);
    Task<Movie?> GetMovieByIdOrSlugAsync(string identifier);
    Task<List<Movie>> GetRelatedMoviesAsync(string movieId, int count = 6);
    /// <summary>Lấy toàn bộ phim không phân biệt loại (dùng cho QuickSearch, Admin)</summary>
    Task<List<Movie>> GetAllMoviesAsync();
    /// <summary>Lấy danh sách phim chiếu rạp (IsCinema = true)</summary>
    Task<List<Movie>> GetCinemaMoviesAsync(int pageSize = 50);
    /// <summary>Lấy danh sách phim bộ (IsSeries / IsPartMovie = true)</summary>
    Task<List<Movie>> GetSeriesMoviesAsync(int pageSize = 50);

    // Dynamic Data Management (CRUD)
    Task AddMovieAsync(Movie movie);
    Task UpdateMovieAsync(Movie movie);
    Task DeleteMovieAsync(string movieId);
    Task AddGenreAsync(Genre genre);
}
