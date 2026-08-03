using Microsoft.AspNetCore.Mvc;
using MovieWeb.Core.Models;
using MovieWeb.Core.Services;
using MovieWeb.Modules.Series.Models;

namespace MovieWeb.Modules.Series.Controllers;

public class SeriesController : Controller
{
    private readonly IMovieService _movieService;

    public SeriesController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet]
    [Route("phim-bo")]
    public async Task<IActionResult> Index(
        string? q = null,
        string? genre = null,
        string? country = null,
        string? sort = "newest",
        int page = 1)
    {
        ViewData["Title"] = "Kho Phim Bộ";
        ViewData["ActivePage"] = "Series";

        // Get all series movies (IsSeries = true, derived from BooleanField IsPartMovie in Orchard Core)
        var seriesListAll = await _movieService.GetSeriesMoviesAsync(pageSize: 500);
        IEnumerable<Movie> seriesQuery = seriesListAll;

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            seriesQuery = seriesQuery.Where(m => m.Title.ToLowerInvariant().Contains(kw) || m.Description.ToLowerInvariant().Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(genre) && genre != "all")
        {
            seriesQuery = seriesQuery.Where(m => m.GenreIds.Contains(genre, StringComparer.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(country) && country != "all")
        {
            seriesQuery = seriesQuery.Where(m => m.Country.Equals(country, StringComparison.OrdinalIgnoreCase));
        }

        // Sorting
        seriesQuery = sort switch
        {
            "popular" => seriesQuery.OrderByDescending(m => m.ViewsCount),
            "rating" => seriesQuery.OrderByDescending(m => m.Rating),
            "title" => seriesQuery.OrderBy(m => m.Title),
            _ => seriesQuery.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.ReleaseYear)
        };

        var seriesList = seriesQuery.ToList();
        var totalItems = seriesList.Count;

        var pageSize = 12;
        var pagedMovies = seriesList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var genres = await _movieService.GetAllGenresAsync();

        var model = new SeriesViewModel
        {
            Movies = pagedMovies,
            Genres = genres,
            SelectedGenre = genre ?? "all",
            SelectedCountry = country ?? "all",
            SearchKeyword = q ?? string.Empty,
            SortBy = sort ?? "newest",
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return View(model);
    }
}
