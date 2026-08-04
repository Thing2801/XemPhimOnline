using System.Text.Json;
using MovieWeb.Core.Models;

namespace MovieWeb.Core.Services;

public class BookmarkService : IBookmarkService
{
    private readonly string _dataFolderPath;
    private readonly string _bookmarksFilePath;
    private readonly List<UserBookmark> _bookmarks = new();
    private readonly object _lockObj = new();

    public BookmarkService()
    {
        _dataFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
        _bookmarksFilePath = Path.Combine(_dataFolderPath, "bookmarks.json");
        LoadDataInternal();
    }

    private void LoadDataInternal()
    {
        lock (_lockObj)
        {
            if (!Directory.Exists(_dataFolderPath))
                Directory.CreateDirectory(_dataFolderPath);

            if (File.Exists(_bookmarksFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_bookmarksFilePath);
                    var list = JsonSerializer.Deserialize<List<UserBookmark>>(json);
                    if (list != null)
                    {
                        _bookmarks.Clear();
                        _bookmarks.AddRange(list);
                    }
                }
                catch { }
            }
        }
    }

    private void SaveDataInternal()
    {
        lock (_lockObj)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_bookmarks, options);
                File.WriteAllText(_bookmarksFilePath, json);
            }
            catch { }
        }
    }

    public Task<bool> IsBookmarkedAsync(string userId, string movieId)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(movieId)) return Task.FromResult(false);
        lock (_lockObj)
        {
            return Task.FromResult(_bookmarks.Any(b => b.UserId == userId && b.MovieId == movieId));
        }
    }

    public Task<bool> ToggleBookmarkAsync(string userId, string movieId)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(movieId)) return Task.FromResult(false);
        lock (_lockObj)
        {
            var existing = _bookmarks.FirstOrDefault(b => b.UserId == userId && b.MovieId == movieId);
            if (existing != null)
            {
                _bookmarks.Remove(existing);
                SaveDataInternal();
                return Task.FromResult(false); // unsaved
            }
            else
            {
                _bookmarks.Add(new UserBookmark
                {
                    UserId = userId,
                    MovieId = movieId,
                    CreatedAt = DateTime.UtcNow
                });
                SaveDataInternal();
                return Task.FromResult(true); // saved
            }
        }
    }

    public Task<List<string>> GetBookmarkedMovieIdsAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return Task.FromResult(new List<string>());
        lock (_lockObj)
        {
            var ids = _bookmarks
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => b.MovieId)
                .ToList();
            return Task.FromResult(ids);
        }
    }

    public async Task<List<Movie>> GetBookmarkedMoviesAsync(string userId, IMovieService movieService)
    {
        var movieIds = await GetBookmarkedMovieIdsAsync(userId);
        if (!movieIds.Any()) return new List<Movie>();

        var allMovies = await movieService.GetAllMoviesAsync();
        var result = new List<Movie>();

        foreach (var id in movieIds)
        {
            var movie = allMovies.FirstOrDefault(m => m.Id == id || m.Slug == id);
            if (movie != null)
            {
                result.Add(movie);
            }
        }

        return result;
    }
}
