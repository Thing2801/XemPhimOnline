using System.Text.Json;
using MovieWeb.Core.Data;
using MovieWeb.Core.Models;

namespace MovieWeb.Core.Services;

public class MovieService : IMovieService
{
    private readonly string _dataFolderPath;
    private readonly string _moviesFilePath;
    private readonly string _genresFilePath;
    
    private List<Movie> _movies = new();
    private List<Genre> _genres = new();
    private readonly object _lockObj = new();

    public MovieService()
    {
        _dataFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
        _moviesFilePath = Path.Combine(_dataFolderPath, "movies.json");
        _genresFilePath = Path.Combine(_dataFolderPath, "genres.json");

        LoadData();
    }

    private void LoadData()
    {
        lock (_lockObj)
        {
            if (!Directory.Exists(_dataFolderPath))
            {
                Directory.CreateDirectory(_dataFolderPath);
            }

            // Genres
            if (File.Exists(_genresFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_genresFilePath);
                    _genres = JsonSerializer.Deserialize<List<Genre>>(json) ?? MovieSeedData.GetDefaultGenres();
                }
                catch
                {
                    _genres = MovieSeedData.GetDefaultGenres();
                }
            }
            else
            {
                _genres = MovieSeedData.GetDefaultGenres();
                SaveGenresToDisk();
            }

            // Movies
            if (File.Exists(_moviesFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_moviesFilePath);
                    _movies = JsonSerializer.Deserialize<List<Movie>>(json) ?? MovieSeedData.GetDefaultMovies();
                }
                catch
                {
                    _movies = MovieSeedData.GetDefaultMovies();
                }
            }
            else
            {
                _movies = MovieSeedData.GetDefaultMovies();
                SaveMoviesToDisk();
            }

            RecalculateGenreMovieCounts();
        }
    }

    private void SaveMoviesToDisk()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_movies, options);
        File.WriteAllText(_moviesFilePath, json);
    }

    private void SaveGenresToDisk()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_genres, options);
        File.WriteAllText(_genresFilePath, json);
    }

    private void RecalculateGenreMovieCounts()
    {
        foreach (var genre in _genres)
        {
            genre.MovieCount = _movies.Count(m => m.GenreIds.Contains(genre.Id, StringComparer.OrdinalIgnoreCase));
        }
    }

    public Task<List<Movie>> GetFeaturedMoviesAsync(int count = 5)
    {
        lock (_lockObj)
        {
            var featured = _movies
                .Where(m => m.IsFeatured)
                .OrderBy(m => m.FeaturedOrder)
                .Take(count)
                .ToList();

            if (featured.Count < count)
            {
                var existingIds = featured.Select(f => f.Id).ToHashSet();
                var additional = _movies
                    .Where(m => !existingIds.Contains(m.Id))
                    .OrderByDescending(m => m.Rating)
                    .Take(count - featured.Count);
                featured.AddRange(additional);
            }

            return Task.FromResult(featured);
        }
    }

    public Task<List<Movie>> GetTrendingMoviesAsync(int count = 10)
    {
        lock (_lockObj)
        {
            var trending = _movies
                .OrderByDescending(m => m.ViewsCount)
                .ThenByDescending(m => m.Rating)
                .Take(count)
                .ToList();

            return Task.FromResult(trending);
        }
    }

    public Task<List<Movie>> GetMoviesByGenreAsync(string genreId, int count = 10)
    {
        lock (_lockObj)
        {
            var results = _movies
                .Where(m => m.GenreIds.Contains(genreId, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(m => m.ReleaseYear)
                .ThenByDescending(m => m.Rating)
                .Take(count)
                .ToList();

            return Task.FromResult(results);
        }
    }

    public Task<List<Genre>> GetAllGenresAsync()
    {
        lock (_lockObj)
        {
            return Task.FromResult(_genres.ToList());
        }
    }

    public Task<Genre?> GetGenreByIdOrSlugAsync(string identifier)
    {
        lock (_lockObj)
        {
            var genre = _genres.FirstOrDefault(g => 
                g.Id.Equals(identifier, StringComparison.OrdinalIgnoreCase) || 
                g.Slug.Equals(identifier, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(genre);
        }
    }

    public Task<List<Movie>> GetMoviesAsync(
        string? searchKeyword = null,
        string? genreId = null,
        string? country = null,
        int? year = null,
        bool? isSeries = null,
        string? sortBy = null,
        int page = 1,
        int pageSize = 12,
        bool isRegularOnly = false)
    {
        lock (_lockObj)
        {
            var query = FilterMoviesQuery(searchKeyword, genreId, country, year, isSeries);

            if (isRegularOnly)
                query = query.Where(m => !m.IsCinema && !m.IsSeries);

            query = sortBy?.ToLowerInvariant() switch
            {
                "views" or "popular" => query.OrderByDescending(m => m.ViewsCount),
                "rating" or "top" => query.OrderByDescending(m => m.Rating),
                "oldest" => query.OrderBy(m => m.ReleaseYear).ThenBy(m => m.Title),
                "title" => query.OrderBy(m => m.Title),
                _ => query.OrderByDescending(m => m.ReleaseYear).ThenByDescending(m => m.CreatedAt)
            };

            var pagedMovies = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult(pagedMovies);
        }
    }

    public Task<int> GetMoviesCountAsync(
        string? searchKeyword = null,
        string? genreId = null,
        string? country = null,
        int? year = null,
        bool? isSeries = null,
        bool isRegularOnly = false)
    {
        lock (_lockObj)
        {
            var query = FilterMoviesQuery(searchKeyword, genreId, country, year, isSeries);
            if (isRegularOnly)
                query = query.Where(m => !m.IsCinema && !m.IsSeries);
            return Task.FromResult(query.Count());
        }
    }

    public Task<Movie?> GetMovieByIdOrSlugAsync(string identifier)
    {
        lock (_lockObj)
        {
            var movie = _movies.FirstOrDefault(m => 
                m.Id.Equals(identifier, StringComparison.OrdinalIgnoreCase) || 
                m.Slug.Equals(identifier, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(movie);
        }
    }

    public Task<List<Movie>> GetRelatedMoviesAsync(string movieId, int count = 6)
    {
        lock (_lockObj)
        {
            var current = _movies.FirstOrDefault(m => m.Id.Equals(movieId, StringComparison.OrdinalIgnoreCase));
            if (current == null)
                return Task.FromResult(_movies.Take(count).ToList());

            var related = _movies
                .Where(m => m.Id != current.Id && m.GenreIds.Any(g => current.GenreIds.Contains(g)))
                .OrderByDescending(m => m.Rating)
                .Take(count)
                .ToList();

            if (related.Count < count)
            {
                var currentIds = related.Select(r => r.Id).ToHashSet();
                currentIds.Add(current.Id);
                var fill = _movies.Where(m => !currentIds.Contains(m.Id)).Take(count - related.Count);
                related.AddRange(fill);
            }

            return Task.FromResult(related);
        }
    }

    public Task<List<Movie>> GetRankedMoviesAsync(string criteria = "views", string period = "all", string? genreId = null, int count = 50)
    {
        lock (_lockObj)
        {
            IEnumerable<Movie> query = _movies;

            if (!string.IsNullOrWhiteSpace(genreId) && genreId != "all")
            {
                query = query.Where(m => m.GenreIds.Contains(genreId, StringComparer.OrdinalIgnoreCase));
            }

            if (string.Equals(criteria, "rating", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderByDescending(m => m.Rating).ThenByDescending(m => m.ViewsCount);
            }
            else
            {
                query = (period?.ToLowerInvariant()) switch
                {
                    "today" => query.OrderByDescending(m => (m.ViewsCount * 0.05) + (m.ReleaseYear >= 2026 ? 100000 : 0) + (m.Rating * 5000)),
                    "week"  => query.OrderByDescending(m => (m.ViewsCount * 0.25) + (m.ReleaseYear >= 2026 ? 250000 : 0) + (m.Rating * 15000)),
                    "month" => query.OrderByDescending(m => (m.ViewsCount * 0.65) + (m.ReleaseYear >= 2026 ? 500000 : 0) + (m.Rating * 30000)),
                    _       => query.OrderByDescending(m => m.ViewsCount).ThenByDescending(m => m.Rating)
                };
            }

            return Task.FromResult(query.Take(count).ToList());
        }
    }

    public Task IncrementViewsAsync(string movieId)
    {
        lock (_lockObj)
        {
            var movie = _movies.FirstOrDefault(m => m.Id == movieId || m.Slug == movieId);
            if (movie != null)
            {
                movie.ViewsCount++;
            }
        }
        return Task.CompletedTask;
    }

    public Task<List<Movie>> GetAllMoviesAsync()
    {
        lock (_lockObj)
        {
            return Task.FromResult(_movies.ToList());
        }
    }

    public Task<List<Movie>> GetCinemaMoviesAsync(int pageSize = 50)
    {
        lock (_lockObj)
        {
            // In-memory fallback: return movies with IsCinema = true
            var cinemaMovies = _movies.Where(m => m.IsCinema).Take(pageSize).ToList();
            return Task.FromResult(cinemaMovies);
        }
    }

    public Task<List<Movie>> GetSeriesMoviesAsync(int pageSize = 50)
    {
        lock (_lockObj)
        {
            // In-memory fallback: return movies with IsSeries = true
            var seriesMovies = _movies.Where(m => m.IsSeries).Take(pageSize).ToList();
            return Task.FromResult(seriesMovies);
        }
    }

    public Task AddMovieAsync(Movie movie)
    {
        lock (_lockObj)
        {
            if (string.IsNullOrEmpty(movie.Id))
            {
                movie.Id = Guid.NewGuid().ToString("N")[..8];
            }
            if (string.IsNullOrEmpty(movie.Slug))
            {
                movie.Slug = movie.Id;
            }

            _movies.RemoveAll(m => m.Id == movie.Id);
            _movies.Insert(0, movie);

            RecalculateGenreMovieCounts();
            SaveMoviesToDisk();
            SaveGenresToDisk();
        }
        return Task.CompletedTask;
    }

    public Task UpdateMovieAsync(Movie movie)
    {
        return AddMovieAsync(movie);
    }

    public Task DeleteMovieAsync(string movieId)
    {
        lock (_lockObj)
        {
            _movies.RemoveAll(m => m.Id.Equals(movieId, StringComparison.OrdinalIgnoreCase));
            RecalculateGenreMovieCounts();
            SaveMoviesToDisk();
            SaveGenresToDisk();
        }
        return Task.CompletedTask;
    }

    public Task AddGenreAsync(Genre genre)
    {
        lock (_lockObj)
        {
            _genres.RemoveAll(g => g.Id.Equals(genre.Id, StringComparison.OrdinalIgnoreCase));
            _genres.Add(genre);
            RecalculateGenreMovieCounts();
            SaveGenresToDisk();
        }
        return Task.CompletedTask;
    }

    private IEnumerable<Movie> FilterMoviesQuery(
        string? searchKeyword,
        string? genreId,
        string? country,
        int? year,
        bool? isSeries)
    {
        IEnumerable<Movie> query = _movies;

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.Trim().ToLowerInvariant();
            query = query.Where(m => 
                m.Title.ToLowerInvariant().Contains(kw) ||
                m.OriginalTitle.ToLowerInvariant().Contains(kw) ||
                m.Director.ToLowerInvariant().Contains(kw) ||
                m.Cast.Any(c => c.ToLowerInvariant().Contains(kw)) ||
                m.Description.ToLowerInvariant().Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(genreId) && genreId != "all")
        {
            query = query.Where(m => m.GenreIds.Contains(genreId, StringComparer.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(country) && country != "all")
        {
            string cNorm = OrchardCoreMovieService.RemoveDiacritics(country.Trim().ToLowerInvariant());
            query = query.Where(m => {
                if (string.IsNullOrWhiteSpace(m.Country)) return false;
                string mNorm = OrchardCoreMovieService.RemoveDiacritics(m.Country.Trim().ToLowerInvariant());
                if (mNorm == cNorm || mNorm.Contains(cNorm) || cNorm.Contains(mNorm)) return true;
                if (cNorm.Contains("au my") && (mNorm.Contains("my") || mNorm.Contains("hoa ky") || mNorm.Contains("anh") || mNorm.Contains("phap") || mNorm.Contains("au my"))) return true;
                return false;
            });
        }

        if (year.HasValue && year.Value > 0)
        {
            query = query.Where(m => m.ReleaseYear == year.Value);
        }

        if (isSeries.HasValue)
        {
            query = query.Where(m => m.IsSeries == isSeries.Value);
        }

        return query;
    }
}
