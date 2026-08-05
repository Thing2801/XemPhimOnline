using MovieWeb.Core.Models;

namespace MovieWeb.Modules.Movies.Models;

public class RankingsViewModel
{
    public List<Movie> RankedMovies { get; set; } = new();
    
    public Movie? Top1Movie => RankedMovies.Count > 0 ? RankedMovies[0] : null;
    public Movie? Top2Movie => RankedMovies.Count > 1 ? RankedMovies[1] : null;
    public Movie? Top3Movie => RankedMovies.Count > 2 ? RankedMovies[2] : null;
    
    public List<Movie> ListMovies => RankedMovies.Count > 3 ? RankedMovies.Skip(3).ToList() : new();

    public List<Genre> Genres { get; set; } = new();
    public string ActiveCriteria { get; set; } = "views"; // "views" or "rating"
    public string ActivePeriod { get; set; } = "all"; // "today", "week", "month", "all"
    public string SelectedGenre { get; set; } = "all";
    public Genre? SelectedGenreInfo { get; set; }
}
