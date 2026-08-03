using MovieWeb.Core.Models;

namespace MovieWeb.Modules.Home.Models;

public class HomeViewModel
{
    public List<Movie> HeroMovies { get; set; } = new();
    public List<Movie> TrendingMovies { get; set; } = new();
    public List<Genre> Genres { get; set; } = new();

    public List<Movie> ActionMovies { get; set; } = new();
    public List<Movie> RomanceMovies { get; set; } = new();
    public List<Movie> HorrorMovies { get; set; } = new();
    public List<Movie> FantasyMovies { get; set; } = new();
    public List<Movie> AnimationMovies { get; set; } = new();
    public List<Movie> SciFiMovies { get; set; } = new();
    public List<Movie> AncientMovies { get; set; } = new();
}
