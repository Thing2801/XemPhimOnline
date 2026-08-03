using Microsoft.AspNetCore.Mvc;
using MovieWeb.Core.Services;
using MovieWeb.Modules.Home.Models;

namespace MovieWeb.Modules.Home.Controllers;

public class HomeController : Controller
{
    private readonly IMovieService _movieService;

    public HomeController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet]
    [Route("")]
    [Route("home")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActivePage"] = "Home";

        var model = new HomeViewModel
        {
            HeroMovies = await _movieService.GetFeaturedMoviesAsync(5),
            TrendingMovies = await _movieService.GetTrendingMoviesAsync(10),
            Genres = await _movieService.GetAllGenresAsync(),
            ActionMovies = await _movieService.GetMoviesByGenreAsync("hanh-dong", 10),
            RomanceMovies = await _movieService.GetMoviesByGenreAsync("tinh-cam", 10),
            HorrorMovies = await _movieService.GetMoviesByGenreAsync("kinh-di", 10),
            FantasyMovies = await _movieService.GetMoviesByGenreAsync("huyen-thoai", 10),
            AnimationMovies = await _movieService.GetMoviesByGenreAsync("hoat-hinh", 10),
            SciFiMovies = await _movieService.GetMoviesByGenreAsync("khoa-hoc", 10),
            AncientMovies = await _movieService.GetMoviesByGenreAsync("co-trang", 10)
        };

        return View(model);
    }
}
