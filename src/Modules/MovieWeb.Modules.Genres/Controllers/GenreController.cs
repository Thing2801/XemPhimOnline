using Microsoft.AspNetCore.Mvc;
using MovieWeb.Core.Services;

namespace MovieWeb.Modules.Genres.Controllers;

public class GenreController : Controller
{
    private readonly IMovieService _movieService;

    public GenreController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet]
    [Route("the-loai")]
    public async Task<IActionResult> Index()
    {
        var genres = await _movieService.GetAllGenresAsync();
        return View(genres);
    }

    [HttpGet]
    [Route("the-loai/{slug}")]
    public IActionResult Detail(string slug)
    {
        return Redirect($"/phim?genre={slug}");
    }
}
