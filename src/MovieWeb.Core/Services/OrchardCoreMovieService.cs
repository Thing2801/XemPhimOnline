using System.Text.RegularExpressions;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using MovieWeb.Core.Data;
using MovieWeb.Core.Models;
using YesSql;

namespace MovieWeb.Core.Services;

public class OrchardCoreMovieService : IMovieService
{
    private readonly ISession _session;
    private readonly IContentManager _contentManager;

    public OrchardCoreMovieService(ISession session, IContentManager contentManager)
    {
        _session = session;
        _contentManager = contentManager;
    }

    private async Task<List<Genre>> GetOrchardGenresInternalAsync()
    {
        try
        {
            var contentItems = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "Category" && (x.Published || x.Latest)).ListAsync();
            var list = new List<Genre>();

            foreach (var item in contentItems)
            {
                dynamic content = item.Content;
                dynamic categoryData = content.Category ?? content;

                string name = GetFieldText(categoryData?.NameCatedory) 
                              ?? GetFieldText(categoryData?.NameCategory) 
                              ?? item.DisplayText 
                              ?? "Thể loại";
                
                string slug = item.DisplayText?.ToLowerInvariant().Replace(" ", "-") ?? item.ContentItemId;

                list.Add(new Genre
                {
                    Id = slug,
                    Name = name,
                    Slug = slug,
                    Description = $"Phim thể loại {name}",
                    IconClass = GetGenreIcon(name),
                    BadgeColor = "badge-primary"
                });
            }

            if (!list.Any())
            {
                return MovieSeedData.GetDefaultGenres();
            }

            return list;
        }
        catch
        {
            return MovieSeedData.GetDefaultGenres();
        }
    }

    private async Task<List<Movie>> GetOrchardMoviesInternalAsync()
    {
        try
        {
            var rawItems = await _session.Query<ContentItem, ContentItemIndex>(x => x.Published || x.Latest).ListAsync();
            var movieItems = rawItems.Where(x => string.Equals(x.ContentType, "Movie", StringComparison.OrdinalIgnoreCase)).ToList();

            // Deduplicate by ContentItemId: Always pick the Latest / Published version
            var contentItems = movieItems
                .GroupBy(x => x.ContentItemId)
                .Select(g => g.OrderByDescending(x => x.Latest).ThenByDescending(x => x.Published).First())
                .ToList();

            var list = new List<Movie>();

            foreach (var item in contentItems)
            {
                dynamic content = item.Content;
                dynamic movieData = content.Movie ?? content;

                string title = GetFieldText(movieData?.NameOfMovie) ?? item.DisplayText ?? "Phim mới";
                string detail = GetHtmlFieldText(movieData?.Detail) ?? GetFieldText(movieData?.Detail) ?? "";
                string trailer = GetFieldText(movieData?.Trailer) ?? "https://www.youtube.com/embed/dQw4w9WgXcQ";
                string watchUrl = GetFieldText(movieData?.Watch) ?? trailer;
                string categoryText = GetFieldText(movieData?.Category) ?? "Hành động";
                string country = GetFieldText(movieData?.Country) ?? "Âu Mỹ";
                string numbericPart = GetFieldText(movieData?.NumbericPart) ?? GetNumberFieldText(movieData?.NumbericPart) ?? "Full HD";
                
                string avatar = GetMediaOrText(movieData?.AvatarOfMovie) 
                             ?? GetMediaOrText(movieData?.AvatarMovie) 
                             ?? GetMediaOrText(movieData?.Avatar) 
                             ?? GetMediaOrText(movieData?.Poster) 
                             ?? GetMediaOrText(movieData?.AnhDaiDien)
                             ?? "https://images.unsplash.com/photo-1534447677768-be436bb09401?auto=format&fit=crop&w=600&q=80";

                string background = GetMediaOrText(movieData?.Background) 
                                 ?? GetMediaOrText(movieData?.HinhNen) 
                                 ?? GetMediaOrText(movieData?.Banner)
                                 ?? "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?auto=format&fit=crop&w=1600&q=80";
                
                bool isFeatured = GetBoolFieldValue(item.Content, "Remarkable", "IsFeatured", "NoiBat");
                double rating = GetDoubleField(movieData?.Evaluate, 9.0);
                string resolution = GetFieldText(movieData?.Resolution) ?? "4K Ultra HD";
                int year = GetIntField(movieData?.YearOfProduction, 2026);

                var genresList = categoryText.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => g.Trim())
                    .ToList();

                var genreIds = genresList.Select(g => g.ToLowerInvariant().Replace(" ", "-")).ToList();

                // Read IsInCinema field (BooleanField for Phim Chiếu Rạp)
                bool isCinema = GetBoolFieldValue(item.Content, "IsInCinema", "IsCinema", "ChieuRap");

                // Read IsPartMovie field (BooleanField for Phim Bộ)
                bool isPartMovie = GetBoolFieldValue(item.Content, "IsPartMovie", "IsPart", "IsSeries", "PhimBo", "PhimBoField", "PartMovie", "IsMoviePart", "MoviePart")
                                || numbericPart.ToLowerInvariant().Contains("tập")
                                || numbericPart.ToLowerInvariant().Contains("tap");

                list.Add(new Movie
                {
                    Id = item.ContentItemId,
                    Title = title,
                    OriginalTitle = title,
                    Slug = item.DisplayText?.ToLowerInvariant().Replace(" ", "-") ?? item.ContentItemId,
                    Description = detail,
                    Tagline = title,
                    PosterUrl = avatar,
                    BannerUrl = background,
                    TrailerUrl = trailer,
                    ReleaseYear = year,
                    Duration = numbericPart,
                    Rating = rating,
                    AgeRating = "16+",
                    Quality = resolution,
                    LanguageMode = "Vietsub + Thuyết Minh",
                    Country = country,
                    Director = "Orchard Core Admin",
                    Cast = new List<string> { "Diễn viên" },
                    GenreIds = genreIds,
                    GenreNames = genresList,
                    IsFeatured = isFeatured,
                    FeaturedOrder = 1,
                    IsCinema = isCinema,
                    IsSeries = isPartMovie,
                    EpisodeInfo = numbericPart,
                    ViewsCount = 10000,
                    CreatedAt = item.CreatedUtc ?? DateTime.UtcNow
                });
            }

            if (!list.Any())
            {
                return MovieSeedData.GetDefaultMovies();
            }

            return list;
        }
        catch
        {
            return MovieSeedData.GetDefaultMovies();
        }
    }

    public async Task<List<Movie>> GetFeaturedMoviesAsync(int count = 5)
    {
        var movies = await GetOrchardMoviesInternalAsync();
        var featured = movies.Where(m => m.IsFeatured).Take(count).ToList();
        if (featured.Count < count)
        {
            var remaining = movies.Where(m => !featured.Contains(m)).Take(count - featured.Count);
            featured.AddRange(remaining);
        }
        return featured;
    }

    public async Task<List<Movie>> GetTrendingMoviesAsync(int count = 10)
    {
        var movies = await GetOrchardMoviesInternalAsync();
        return movies.OrderByDescending(m => m.Rating).ThenByDescending(m => m.ViewsCount).Take(count).ToList();
    }

    public async Task<List<Movie>> GetMoviesByGenreAsync(string genreId, int count = 10)
    {
        var movies = await GetOrchardMoviesInternalAsync();
        return movies.Where(m => m.GenreIds.Any(g => g.Equals(genreId, StringComparison.OrdinalIgnoreCase)))
            .Take(count).ToList();
    }

    public async Task<List<Genre>> GetAllGenresAsync()
    {
        var genres = await GetOrchardGenresInternalAsync();
        var movies = await GetOrchardMoviesInternalAsync();

        foreach (var g in genres)
        {
            g.MovieCount = movies.Count(m => m.GenreIds.Contains(g.Id, StringComparer.OrdinalIgnoreCase));
        }

        return genres;
    }

    public async Task<Genre?> GetGenreByIdOrSlugAsync(string identifier)
    {
        var genres = await GetAllGenresAsync();
        return genres.FirstOrDefault(g => 
            g.Id.Equals(identifier, StringComparison.OrdinalIgnoreCase) || 
            g.Slug.Equals(identifier, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<Movie>> GetMoviesAsync(
        string? searchKeyword = null,
        string? genreId = null,
        string? country = null,
        int? year = null,
        bool? isSeries = null,
        string? sortBy = null,
        int page = 1,
        int pageSize = 12)
    {
        var movies = await GetOrchardMoviesInternalAsync();
        IEnumerable<Movie> query = movies;

        // Phim trên trang /phim chỉ là Phim Lẻ (không thuộc Phim Chiếu Rạp và không thuộc Phim Bộ)
        query = query.Where(m => !m.IsCinema && !m.IsSeries);

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = RemoveDiacritics(searchKeyword.Trim().ToLowerInvariant());
            query = query.Where(m =>
                RemoveDiacritics(m.Title.ToLowerInvariant()).Contains(kw) ||
                RemoveDiacritics(m.Description.ToLowerInvariant()).Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(genreId) && genreId != "all")
        {
            query = query.Where(m => m.GenreIds.Contains(genreId, StringComparer.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(country) && country != "all")
        {
            query = query.Where(m => m.Country.Equals(country, StringComparison.OrdinalIgnoreCase));
        }

        if (year.HasValue && year.Value > 0)
        {
            query = query.Where(m => m.ReleaseYear == year.Value);
        }

        if (isSeries.HasValue)
        {
            query = query.Where(m => m.IsSeries == isSeries.Value);
        }

        return query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    }

    public async Task<int> GetMoviesCountAsync(
        string? searchKeyword = null,
        string? genreId = null,
        string? country = null,
        int? year = null,
        bool? isSeries = null)
    {
        var movies = await GetMoviesAsync(searchKeyword, genreId, country, year, isSeries, pageSize: 1000);
        return movies.Count;
    }

    public async Task<Movie?> GetMovieByIdOrSlugAsync(string identifier)
    {
        var movies = await GetOrchardMoviesInternalAsync();
        return movies.FirstOrDefault(m => m.Id.Equals(identifier, StringComparison.OrdinalIgnoreCase) || m.Slug.Equals(identifier, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<Movie>> GetAllMoviesAsync()
    {
        return await GetOrchardMoviesInternalAsync();
    }

    public async Task<List<Movie>> GetCinemaMoviesAsync(int pageSize = 50)
    {
        var movies = await GetOrchardMoviesInternalAsync();
        return movies.Where(m => m.IsCinema).Take(pageSize).ToList();
    }

    public async Task<List<Movie>> GetSeriesMoviesAsync(int pageSize = 50)
    {
        var movies = await GetOrchardMoviesInternalAsync();
        return movies.Where(m => m.IsSeries).Take(pageSize).ToList();
    }

    public async Task<List<Movie>> GetRelatedMoviesAsync(string movieId, int count = 6)
    {
        var movies = await GetOrchardMoviesInternalAsync();
        return movies.Where(m => m.Id != movieId && !m.IsCinema).Take(count).ToList();
    }

    public Task AddMovieAsync(Movie movie) => Task.CompletedTask;
    public Task UpdateMovieAsync(Movie movie) => Task.CompletedTask;
    public Task DeleteMovieAsync(string movieId) => Task.CompletedTask;
    public Task AddGenreAsync(Genre genre) => Task.CompletedTask;

    // Field Helpers
    private static string? GetFieldText(dynamic field)
    {
        try
        {
            if (field == null) return null;
            if (field.Text != null) return CleanHtmlOrPath(field.Text.ToString());
            if (field.Html != null) return CleanHtmlOrPath(field.Html.ToString());
            if (field.Value != null) return field.Value.ToString();
            return null;
        }
        catch { return null; }
    }

    private static string? GetHtmlFieldText(dynamic field)
    {
        try
        {
            if (field?.Html != null) return field.Html.ToString();
            if (field?.Text != null) return field.Text.ToString();
            return null;
        }
        catch { return null; }
    }

    private static string? GetNumberFieldText(dynamic field)
    {
        try { return field?.Value?.ToString(); } catch { return null; }
    }

    private static string? GetMediaOrText(dynamic field)
    {
        try
        {
            if (field == null) return null;

            // HtmlField support
            if (field.Html != null)
            {
                string cleaned = CleanHtmlOrPath(field.Html.ToString());
                if (!string.IsNullOrWhiteSpace(cleaned))
                {
                    return cleaned.StartsWith("http") || cleaned.StartsWith("/") ? cleaned : $"/media/{cleaned}";
                }
            }

            // MediaField support
            if (field.MediaItemPaths != null && field.MediaItemPaths.Count > 0)
            {
                string p = field.MediaItemPaths[0].ToString();
                return p.StartsWith("http") || p.StartsWith("/") ? p : $"/media/{p}";
            }

            if (field.Paths != null && field.Paths.Count > 0)
            {
                string p = field.Paths[0].ToString();
                return p.StartsWith("http") || p.StartsWith("/") ? p : $"/media/{p}";
            }

            // TextField support
            string? txt = field.Text?.ToString() ?? field.Value?.ToString();
            if (!string.IsNullOrWhiteSpace(txt))
            {
                string cleanedText = CleanHtmlOrPath(txt);
                return cleanedText.StartsWith("http") || cleanedText.StartsWith("/") ? cleanedText : $"/media/{cleanedText}";
            }
        }
        catch { return null; }
        return null;
    }

    private static string CleanHtmlOrPath(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Extract URL from <img src="..." />
        var srcMatch = Regex.Match(input, @"src=[""'](?<url>[^""']+)[""']", RegexOptions.IgnoreCase);
        string result = srcMatch.Success ? srcMatch.Groups["url"].Value : Regex.Replace(input, @"<[^>]+>", "").Trim();

        // Auto-fix typo AvaterMovie -> AvatarMovie
        if (result.Contains("/AvaterMovie/", StringComparison.OrdinalIgnoreCase))
        {
            result = Regex.Replace(result, "/AvaterMovie/", "/AvatarMovie/", RegexOptions.IgnoreCase);
        }

        return result;
    }

    /// <summary>Bỏ dấu tiếng Việt để tìm kiếm không phân biệt dấu</summary>
    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in normalized)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    /// <summary>
    /// Đọc boolean field từ Orchard Core ContentItem dynamic object (JObject/JsonObject).
    /// </summary>
    private static bool GetBoolFieldValue(dynamic content, params string[] fieldNames)
    {
        try
        {
            if (content == null) return false;

            string jsonStr = "";
            try
            {
                jsonStr = content.ToJsonString();
            }
            catch
            {
                try
                {
                    jsonStr = System.Text.Json.JsonSerializer.Serialize((object)content);
                }
                catch
                {
                    jsonStr = content.ToString();
                }
            }

            if (string.IsNullOrWhiteSpace(jsonStr) || jsonStr == "System.Text.Json.Dynamic.JsonDynamicObject")
            {
                // Fallback to property iteration
                try
                {
                    dynamic moviePart = content.Movie ?? content;
                    foreach (var name in fieldNames)
                    {
                        dynamic field = moviePart[name] ?? content[name];
                        if (field != null)
                        {
                            dynamic val = field["Value"] ?? field["value"];
                            bool parsedBool = false;
                            if (val != null && bool.TryParse(val.ToString(), out parsedBool))
                            {
                                return parsedBool;
                            }
                        }
                    }
                }
                catch { }
                return false;
            }

            using var doc = System.Text.Json.JsonDocument.Parse(jsonStr);
            return SearchJsonForBoolField(doc.RootElement, fieldNames);
        }
        catch
        {
            return false;
        }
    }

    private static bool SearchJsonForBoolField(System.Text.Json.JsonElement element, string[] fieldNames)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (fieldNames.Any(f => f.Equals(prop.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        foreach (var inner in prop.Value.EnumerateObject())
                        {
                            if (inner.Name.Equals("Value", StringComparison.OrdinalIgnoreCase))
                            {
                                if (inner.Value.ValueKind == System.Text.Json.JsonValueKind.True) return true;
                                if (inner.Value.ValueKind == System.Text.Json.JsonValueKind.False) return false;
                                if (inner.Value.ValueKind == System.Text.Json.JsonValueKind.String && bool.TryParse(inner.Value.GetString(), out bool bResult)) return bResult;
                            }
                        }
                    }
                    else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.True)
                    {
                        return true;
                    }
                }

                if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (SearchJsonForBoolField(prop.Value, fieldNames))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private static double GetDoubleField(dynamic field, double fallback = 8.5)
    {
        try
        {
            if (field?.Value != null) return Convert.ToDouble(field.Value);
        }
        catch { }
        return fallback;
    }

    private static int GetIntField(dynamic field, int fallback = 2026)
    {
        try
        {
            if (field?.Value != null) return Convert.ToInt32(field.Value);
        }
        catch { }
        return fallback;
    }

    private static string GetGenreIcon(string name)
    {
        return name.ToLowerInvariant() switch
        {
            var n when n.Contains("hành động") => "fas fa-fire",
            var n when n.Contains("tình cảm") => "fas fa-heart",
            var n when n.Contains("ngôn tình") => "fas fa-feather-alt",
            var n when n.Contains("kinh dị") => "fas fa-ghost",
            var n when n.Contains("trinh thám") => "fas fa-search-location",
            var n when n.Contains("huyền thoại") || n.Contains("truyền thuyết") => "fas fa-dragon",
            var n when n.Contains("hoạt hình") => "fas fa-magic",
            var n when n.Contains("khoa học") => "fas fa-user-astronaut",
            var n when n.Contains("cổ trang") => "fas fa-fan",
            var n when n.Contains("siêu anh hùng") => "fas fa-mask",
            _ => "fas fa-film"
        };
    }
}
