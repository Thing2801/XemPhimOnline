using Microsoft.AspNetCore.Mvc;
using MovieWeb.Core.Services;
using MovieWeb.Modules.Cinema.Models;
using System.Globalization;
using System.Text;

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
            string normalizedGenre = NormalizeSlug(genre.Trim().ToLowerInvariant());
            nowShowing = nowShowing.Where(m => m.GenreIds.Any(gid => NormalizeSlug(gid.ToLowerInvariant()) == normalizedGenre)).ToList();
            comingSoon = comingSoon.Where(m => m.GenreIds.Any(gid => NormalizeSlug(gid.ToLowerInvariant()) == normalizedGenre)).ToList();
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

    private static string NormalizeSlug(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var sb1 = new StringBuilder(text.Length);
        foreach (var c in text)
            sb1.Append(c == 'đ' ? 'd' : c == 'Đ' ? 'D' : c);
        var normalized = sb1.ToString().Normalize(NormalizationForm.FormD);
        var sb2 = new StringBuilder();
        foreach (var c in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb2.Append(c);
        return sb2.ToString().Normalize(NormalizationForm.FormC);
    }
}
