using MovieWeb.Core.Models;

namespace MovieWeb.Web.Models;

public class MovieFilterViewModel
{
    public List<Movie> Movies { get; set; } = new();
    public List<Genre> Genres { get; set; } = new();
    public string SelectedGenre { get; set; } = "all";
    public string SelectedCountry { get; set; } = "all";
    public int? SelectedYear { get; set; }
    public bool? SelectedIsSeries { get; set; }
    public string SearchKeyword { get; set; } = string.Empty;
    public string SortBy { get; set; } = "newest";
    
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    public Genre? CurrentGenreInfo { get; set; }
}
