using Microsoft.AspNetCore.Mvc;
using MovieWeb.Core.Models;
using MovieWeb.Core.Services;
using MovieWeb.Modules.Movies.Models;

namespace MovieWeb.Modules.Movies.Controllers;

public class MovieController : Controller
{
    private readonly IMovieService _movieService;
    private readonly ICommentService _commentService;

    public MovieController(IMovieService movieService, ICommentService commentService)
    {
        _movieService = movieService;
        _commentService = commentService;
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
            pageSize: 12,
            isRegularOnly: true);

        var totalItems = await _movieService.GetMoviesCountAsync(
            searchKeyword: q,
            genreId: genre,
            country: country,
            year: year,
            isSeries: series,
            isRegularOnly: true);

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

        if (_commentService != null)
        {
            ViewBag.Comments = await _commentService.GetCommentsByMovieIdAsync(movie.Id);
            ViewBag.CommentsCount = await _commentService.GetCommentsCountAsync(movie.Id);
            ViewBag.RatingBreakdown = await _commentService.GetRatingBreakdownAsync(movie.Id);
            ViewBag.AverageRating = await _commentService.GetAverageRatingAsync(movie.Id, movie.Rating);
        }

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

    [HttpGet]
    [Route("api/debug/movies")]
    public async Task<IActionResult> DebugMovies()
    {
        var allMovies = await _movieService.GetAllMoviesAsync();
        var result = allMovies.Select(m => new
        {
            id = m.Id,
            title = m.Title,
            director = m.Director,
            language = m.LanguageMode,
            duration = m.Duration,
            viewsCount = m.ViewsCount,
            ageRating = m.AgeRating,
            isCinema = m.IsCinema,
            isSeries = m.IsSeries,
            isFeatured = m.IsFeatured,
            genreNames = m.GenreNames,
            genreIds = m.GenreIds,
            country = m.Country,
            year = m.ReleaseYear
        });
        return Json(result);
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var sb1 = new System.Text.StringBuilder(text.Length);
        foreach (var c in text)
            sb1.Append(c == 'đ' ? 'd' : c == 'Đ' ? 'D' : c);
        var normalized = sb1.ToString().Normalize(System.Text.NormalizationForm.FormD);
        var sb2 = new System.Text.StringBuilder();
        foreach (var c in normalized)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb2.Append(c);
        return sb2.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}
