using Microsoft.AspNetCore.Mvc;
using MovieWeb.Core.Models;
using MovieWeb.Core.Services;
using MovieWeb.Modules.Movies.Models;

namespace MovieWeb.Modules.Movies.Controllers;

public class MovieController : Controller
{
    private readonly IMovieService _movieService;

    public MovieController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet]
    [Route("phim")]
    public async Task<IActionResult> Index(
        string? q = null,
        string? genre = null,
        string? country = null,
        int? year = null,
        bool? series = null,
        string? sort = "newest",
        int page = 1)
    {
        var movies = await _movieService.GetMoviesAsync(
            searchKeyword: q,
            genreId: genre,
            country: country,
            year: year,
            isSeries: series,
            sortBy: sort,
            page: page,
            pageSize: 12);

        var totalItems = await _movieService.GetMoviesCountAsync(
            searchKeyword: q,
            genreId: genre,
            country: country,
            year: year,
            isSeries: series);

        var genres = await _movieService.GetAllGenresAsync();
        Genre? currentGenre = null;
        if (!string.IsNullOrEmpty(genre) && genre != "all")
        {
            currentGenre = await _movieService.GetGenreByIdOrSlugAsync(genre);
        }

        var model = new MovieFilterViewModel
        {
            Movies = movies,
            Genres = genres,
            SelectedGenre = genre ?? "all",
            SelectedCountry = country ?? "all",
            SelectedYear = year,
            SelectedIsSeries = series,
            SearchKeyword = q ?? string.Empty,
            SortBy = sort ?? "newest",
            CurrentPage = page,
            PageSize = 12,
            TotalItems = totalItems,
            CurrentGenreInfo = currentGenre
        };

        return View(model);
    }

    [HttpGet]
    [Route("phim/{id}")]
    public async Task<IActionResult> Detail(string id)
    {
        var movie = await _movieService.GetMovieByIdOrSlugAsync(id);
        if (movie == null)
        {
            return NotFound();
        }

        var related = await _movieService.GetRelatedMoviesAsync(movie.Id, 6);
        ViewBag.RelatedMovies = related;

        return View(movie);
    }

    [HttpGet]
    [Route("api/movies/quick-search")]
    public async Task<IActionResult> QuickSearch([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return Json(new { success = true, data = new object[] { } });
        }

        // Lấy toàn bộ phim không phân biệt loại (Phim Lẻ, Phim Bộ, Phim Chiếu Rạp) để gợi ý
        var allMovies = await _movieService.GetAllMoviesAsync();

        var kw = RemoveDiacritics(q.Trim().ToLowerInvariant());

        var filtered = allMovies
            .Where(m => RemoveDiacritics(m.Title.ToLowerInvariant()).Contains(kw))
            .Take(6)
            .ToList();

        var results = filtered.Select(m => new
        {
            id = m.Id,
            slug = m.Slug,
            title = m.Title,
            originalTitle = m.OriginalTitle,
            posterUrl = m.PosterUrl,
            year = m.ReleaseYear,
            rating = m.Rating,
            genres = string.Join(", ", m.GenreNames)
        });

        return Json(new { success = true, data = results });
    }

    /// <summary>Bỏ dấu tiếng Việt để so sánh không phân biệt dấu</summary>
    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in normalized)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}
