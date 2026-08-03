namespace MovieWeb.Core.Models;

public class Movie
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string OriginalTitle { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string BannerUrl { get; set; } = string.Empty;
    public string TrailerUrl { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public string Duration { get; set; } = string.Empty;
    public double Rating { get; set; } = 8.0;
    public string AgeRating { get; set; } = "16+";
    public string Quality { get; set; } = "4K Ultra HD";
    public string LanguageMode { get; set; } = "Vietsub + Thuyết Minh";
    public string Country { get; set; } = string.Empty;
    public string Director { get; set; } = string.Empty;
    public List<string> Cast { get; set; } = new();
    public List<string> GenreIds { get; set; } = new();
    public List<string> GenreNames { get; set; } = new();
    
    public bool IsFeatured { get; set; }
    public int FeaturedOrder { get; set; }
    
    /// <summary>Phim chiếu rạp - chỉ hiển thị ở trang Phim Chiếu Rạp, không hiện ở trang Phim</summary>
    public bool IsCinema { get; set; }
    
    public bool IsSeries { get; set; }
    public string EpisodeInfo { get; set; } = "Full Movie";
    
    public int ViewsCount { get; set; }
    public string ViewsText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
