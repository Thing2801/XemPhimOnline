using MovieWeb.Core.Models;

namespace MovieWeb.Modules.Cinema.Models;

public class CinemaViewModel
{
    public List<Movie> NowShowingMovies { get; set; } = new();
    public List<Movie> ComingSoonMovies { get; set; } = new();
    public List<Genre> Genres { get; set; } = new();
    public string SelectedTab { get; set; } = "now-showing"; // "now-showing" or "coming-soon"
    public string SelectedGenre { get; set; } = "all";
    public string SelectedCountry { get; set; } = "all";
    public string SearchKeyword { get; set; } = string.Empty;
    public string SortBy { get; set; } = "newest";
    public Movie? SpotlightMovie { get; set; }
}
