using Microsoft.AspNetCore.Mvc;
using MovieWeb.Core.Services;
using MovieWeb.Modules.Cinema.Models;

namespace MovieWeb.Modules.Cinema.Controllers;

public class CinemaController : Controller
{
    private readonly IMovieService _movieService;

    public CinemaController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet]
    [Route("phim-chieu-rap")]
    public async Task<IActionResult> Index(string tab = "now-showing", string genre = "all")
    {
        ViewData["Title"] = "Phim Chiếu Rạp";
        ViewData["ActivePage"] = "Cinema";

        // Chỉ lấy phim có IsCinema = true (đánh dấu Chiếu Rạp trong Orchard Core admin)
        var cinemaMovies = await _movieService.GetCinemaMoviesAsync(pageSize: 50);
        var genres = await _movieService.GetAllGenresAsync();

        // Filter movies: Phim chiếu rạp - đang chiếu vs sắp chiếu
        var nowShowing = cinemaMovies
            .OrderByDescending(m => m.Rating)
            .ThenByDescending(m => m.CreatedAt)
            .ToList();

        var comingSoon = cinemaMovies
            .Where(m => m.ReleaseYear >= DateTime.Now.Year)
            .OrderBy(m => m.ReleaseYear)
            .ToList();

        if (genre != "all")
        {
            nowShowing = nowShowing.Where(m => m.GenreIds.Contains(genre, StringComparer.OrdinalIgnoreCase)).ToList();
            comingSoon = comingSoon.Where(m => m.GenreIds.Contains(genre, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        var spotlight = nowShowing.FirstOrDefault(m => m.IsFeatured) ?? nowShowing.FirstOrDefault();

        var model = new CinemaViewModel
        {
            NowShowingMovies = nowShowing,
            ComingSoonMovies = comingSoon,
            Genres = genres,
            SelectedTab = tab,
            SelectedGenre = genre,
            SpotlightMovie = spotlight
        };

        return View(model);
    }
}
