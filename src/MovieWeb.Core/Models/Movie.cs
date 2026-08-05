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
    public double Rating { get; set; } = 0.0;
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
    
    /// <summary>Phim đang chiếu rạp (BooleanField: NowShowing)</summary>
    public bool IsNowShowing { get; set; } = true;
    
    /// <summary>Phim sắp chiếu rạp (BooleanField: ComingShowMovie)</summary>
    public bool IsComingSoon { get; set; }
    
    /// <summary>Ngày khởi chiếu rạp (TextField: ComingShowDate)</summary>
    public string ComingShowDate { get; set; } = string.Empty;
    
    /// <summary>Giá vé xem phim chiếu rạp (TextField/NumericField: Price / GiaVe)</summary>
    public decimal Price { get; set; } = 0;
    public string FormattedPrice => Price > 0 ? Price.ToString("#,##0") + " VNĐ" : "Miễn phí";
    public bool RequiresPurchase => IsCinema && IsNowShowing && Price > 0;

    public bool IsSeries { get; set; }
    public string EpisodeInfo { get; set; } = "Full Movie";
    public int TotalEpisodes { get; set; } = 0;
    public List<string> EpisodeUrls { get; set; } = new();

    public string GetEpisodeUrl(int epNumber)
    {
        if (EpisodeUrls != null && epNumber > 0 && epNumber <= EpisodeUrls.Count && !string.IsNullOrWhiteSpace(EpisodeUrls[epNumber - 1]))
        {
            return EpisodeUrls[epNumber - 1];
        }
        return TrailerUrl;
    }

    public int GetTotalEpisodes()
    {
        if (TotalEpisodes > 0) return TotalEpisodes;
        if (!IsSeries) return 1;

        if (!string.IsNullOrWhiteSpace(EpisodeInfo))
        {
            var match = System.Text.RegularExpressions.Regex.Match(EpisodeInfo, @"(\d+)(?:\s*\/\s*(\d+))?");
            if (match.Success)
            {
                if (match.Groups[2].Success && int.TryParse(match.Groups[2].Value, out int count2) && count2 > 0)
                    return count2;
                if (int.TryParse(match.Groups[1].Value, out int count1) && count1 > 0)
                    return count1;
            }
        }
        return 24;
    }
    
    public int ViewsCount { get; set; }
    public string ViewsText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
