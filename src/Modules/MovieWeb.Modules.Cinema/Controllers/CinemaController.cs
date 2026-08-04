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
    public async Task<IActionResult> Index(
        string? q = null,
        string? genre = "all",
        string? country = "all",
        string? sort = "newest",
        string tab = "now-showing")
    {
        ViewData["Title"] = "Phim Chiếu Rạp";
        ViewData["ActivePage"] = "Cinema";

        // Chỉ lấy phim có IsCinema = true (đánh dấu Chiếu Rạp trong Orchard Core admin)
        var cinemaMovies = await _movieService.GetCinemaMoviesAsync(pageSize: 100);
        var genres = await _movieService.GetAllGenresAsync();

        IEnumerable<MovieWeb.Core.Models.Movie> query = cinemaMovies;

        if (!string.IsNullOrWhiteSpace(q))
        {
            string kw = OrchardCoreMovieService.RemoveDiacritics(q.Trim().ToLowerInvariant());
            query = query.Where(m =>
                OrchardCoreMovieService.RemoveDiacritics(m.Title.ToLowerInvariant()).Contains(kw) ||
                OrchardCoreMovieService.RemoveDiacritics(m.Description.ToLowerInvariant()).Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(genre) && genre != "all")
        {
            string normalizedGenre = NormalizeSlug(genre.Trim().ToLowerInvariant());
            query = query.Where(m => m.GenreIds.Any(gid => NormalizeSlug(gid.ToLowerInvariant()) == normalizedGenre));
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

        query = (sort?.ToLowerInvariant()) switch
        {
            "popular" or "views" => query.OrderByDescending(m => m.ViewsCount),
            "rating" or "top" => query.OrderByDescending(m => m.Rating).ThenByDescending(m => m.ViewsCount),
            "title" => query.OrderBy(m => m.Title),
            "newest" => query.OrderByDescending(m => m.ReleaseYear).ThenByDescending(m => m.CreatedAt),
            _ => query.OrderByDescending(m => m.ReleaseYear).ThenByDescending(m => m.CreatedAt)
        };

        var filteredList = query.ToList();

        var nowShowing = filteredList.Where(m => m.IsNowShowing || (!m.IsNowShowing && !m.IsComingSoon)).ToList();
        var comingSoon = filteredList.Where(m => m.IsComingSoon).ToList();

        var spotlight = nowShowing.FirstOrDefault(m => m.IsFeatured) ?? nowShowing.FirstOrDefault();

        var model = new CinemaViewModel
        {
            NowShowingMovies = nowShowing,
            ComingSoonMovies = comingSoon,
            Genres = genres,
            SelectedTab = tab,
            SelectedGenre = genre ?? "all",
            SelectedCountry = country ?? "all",
            SearchKeyword = q ?? string.Empty,
            SortBy = sort ?? "newest",
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
