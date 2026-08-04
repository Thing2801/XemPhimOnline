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
    private readonly ICommentService? _commentService;

    public OrchardCoreMovieService(ISession session, IContentManager contentManager, ICommentService? commentService = null)
    {
        _session = session;
        _contentManager = contentManager;
        _commentService = commentService;
    }

    private async Task<List<Genre>> GetOrchardGenresInternalAsync()
    {
        try
        {
            var contentItems = await _session.Query<ContentItem, ContentItemIndex>(x => (x.ContentType == "Category" || x.ContentType == "Genre" || x.ContentType == "TheLoai") && (x.Published || x.Latest)).ListAsync();
            var list = new List<Genre>();

            foreach (var item in contentItems)
            {
                dynamic content = item.Content;
                dynamic categoryData = content.Category ?? content;

                string name = GetFieldText(categoryData?.NameCatedory) 
                              ?? GetFieldText(categoryData?.NameCategory) 
                              ?? item.DisplayText 
                              ?? "Thể loại";
                
                string cleanSlug = RemoveDiacritics(name).ToLowerInvariant().Replace(" ", "-");

                list.Add(new Genre
                {
                    Id = cleanSlug,
                    Name = name,
                    Slug = cleanSlug,
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
            // Pre-fetch categories/genres for Taxonomy/ContentPicker ID mapping
            var categoryItems = await _session.Query<ContentItem, ContentItemIndex>(x => (x.ContentType == "Category" || x.ContentType == "Genre" || x.ContentType == "TheLoai") && (x.Published || x.Latest)).ListAsync();
            var categoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cat in categoryItems)
            {
                dynamic catContent = cat.Content;
                dynamic categoryData = catContent.Category ?? catContent.MovieInfo ?? catContent;
                string catName = GetFieldText(categoryData?.NameCatedory) 
                              ?? GetFieldText(categoryData?.NameCategory) 
                              ?? cat.DisplayText 
                              ?? "Thể loại";
                categoryMap[cat.ContentItemId] = catName;
                if (!string.IsNullOrWhiteSpace(cat.DisplayText))
                {
                    categoryMap[cat.DisplayText] = catName;
                }
            }

            var rawItems = await _session.Query<ContentItem, ContentItemIndex>(x => x.Published || x.Latest).ListAsync();

            // Build dictionary lookup of all content items by ContentItemId
            var allItemsById = rawItems
                .GroupBy(x => x.ContentItemId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.Latest).ThenByDescending(x => x.Published).First(),
                    StringComparer.OrdinalIgnoreCase
                );

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
                dynamic movieData = content.Movie ?? content.MovieInfo ?? content;

                // Resolve picked MovieInfo ContentItem if linked via ContentPickerField
                dynamic? pickedMovieInfoData = null;
                dynamic? pickedMovieInfoContent = null;
                try
                {
                    dynamic? pickerField = movieData?.MovieInfo ?? content?.MovieInfo;
                    dynamic? pickedIds = pickerField?.ContentItemIds;
                    if (pickedIds != null)
                    {
                        foreach (var pid in pickedIds)
                        {
                            string idStr = pid.ToString();
                            if (allItemsById.TryGetValue(idStr, out var pickedItem))
                            {
                                pickedMovieInfoContent = pickedItem.Content;
                                pickedMovieInfoData = pickedMovieInfoContent.MovieInfo ?? pickedMovieInfoContent.Movie ?? pickedMovieInfoContent;
                                break;
                            }
                        }
                    }
                }
                catch { }

                string title = GetFieldText(movieData?.NameOfMovie) 
                            ?? GetFieldText(pickedMovieInfoData?.NameOfMovie) 
                            ?? item.DisplayText 
                            ?? "Phim mới";

                string detail = GetHtmlFieldText(movieData?.Detail) ?? GetFieldText(movieData?.Detail)
                             ?? GetHtmlFieldText(pickedMovieInfoData?.Detail) ?? GetFieldText(pickedMovieInfoData?.Detail) ?? "";

                string trailer = GetFieldText(movieData?.Trailer) 
                              ?? GetFieldText(pickedMovieInfoData?.Trailer) 
                              ?? "https://www.youtube.com/embed/dQw4w9WgXcQ";

                string watchUrl = GetFieldText(movieData?.Watch) 
                               ?? GetFieldText(pickedMovieInfoData?.Watch) 
                               ?? trailer;

                string director = GetFlexibleFieldText(pickedMovieInfoData, "DaoDien", "Đạo diễn", "Director")
                               ?? GetFlexibleFieldText(pickedMovieInfoContent, "DaoDien", "Đạo diễn", "Director")
                               ?? GetFlexibleFieldText(movieData, "DaoDien", "Đạo diễn", "Director")
                               ?? GetFlexibleFieldText(item.Content, "DaoDien", "Đạo diễn", "Director")
                               ?? "Đang cập nhật";

                string language = GetFlexibleFieldText(pickedMovieInfoData, "NgonNgu", "Ngôn ngữ", "Language", "LanguageMode")
                               ?? GetFlexibleFieldText(pickedMovieInfoContent, "NgonNgu", "Ngôn ngữ", "Language", "LanguageMode")
                               ?? GetFlexibleFieldText(movieData, "NgonNgu", "Ngôn ngữ", "Language", "LanguageMode")
                               ?? GetFlexibleFieldText(item.Content, "NgonNgu", "Ngôn ngữ", "Language", "LanguageMode")
                               ?? "Vietsub";

                string duration = GetFlexibleFieldText(pickedMovieInfoData, "ThoiLuong", "Thời lượng", "Duration", "NumbericPart")
                               ?? GetFlexibleFieldText(pickedMovieInfoContent, "ThoiLuong", "Thời lượng", "Duration", "NumbericPart")
                               ?? GetFlexibleFieldText(movieData, "ThoiLuong", "Thời lượng", "Duration", "NumbericPart")
                               ?? GetFlexibleFieldText(item.Content, "ThoiLuong", "Thời lượng", "Duration", "NumbericPart")
                               ?? "Full HD";

                string viewsText = GetFlexibleFieldText(pickedMovieInfoData, "View", "Views", "ViewField", "LuotXem", "Lượt xem", "ViewsCount")
                                ?? GetFlexibleFieldText(pickedMovieInfoContent, "View", "Views", "ViewField", "LuotXem", "Lượt xem", "ViewsCount")
                                ?? GetFlexibleFieldText(movieData, "View", "Views", "ViewField", "LuotXem", "Lượt xem", "ViewsCount")
                                ?? GetFlexibleFieldText(item.Content, "View", "Views", "ViewField", "LuotXem", "Lượt xem", "ViewsCount")
                                ?? "";
                
                int viewsCount = 0;
                if (!string.IsNullOrWhiteSpace(viewsText))
                {
                    string digitsOnly = System.Text.RegularExpressions.Regex.Replace(viewsText, @"[^\d]", "");
                    if (int.TryParse(digitsOnly, out int pViews)) viewsCount = pViews;
                }

                string ageRating = GetFlexibleFieldText(pickedMovieInfoData, "PhanLoai", "Phân loại", "AgeRating", "Classification")
                                ?? GetFlexibleFieldText(pickedMovieInfoContent, "PhanLoai", "Phân loại", "AgeRating", "Classification")
                                ?? GetFlexibleFieldText(movieData, "PhanLoai", "Phân loại", "AgeRating", "Classification")
                                ?? GetFlexibleFieldText(item.Content, "PhanLoai", "Phân loại", "AgeRating", "Classification")
                                ?? "16+";

                string country = GetFieldText(movieData?.Country) 
                              ?? GetFieldText(pickedMovieInfoData?.Country) 
                              ?? "Âu Mỹ";

                string avatar = GetMediaOrText(movieData?.AvatarOfMovie) 
                             ?? GetMediaOrText(movieData?.AvatarMovie) 
                             ?? GetMediaOrText(movieData?.Avatar) 
                             ?? GetMediaOrText(movieData?.Poster) 
                             ?? GetMediaOrText(movieData?.AnhDaiDien)
                             ?? GetMediaOrText(pickedMovieInfoData?.AvatarOfMovie)
                             ?? GetMediaOrText(pickedMovieInfoData?.AvatarMovie)
                             ?? GetMediaOrText(pickedMovieInfoData?.Avatar)
                             ?? GetMediaOrText(pickedMovieInfoData?.Poster)
                             ?? "https://images.unsplash.com/photo-1534447677768-be436bb09401?auto=format&fit=crop&w=600&q=80";

                string background = GetMediaOrText(movieData?.Background) 
                                 ?? GetMediaOrText(movieData?.HinhNen) 
                                 ?? GetMediaOrText(movieData?.Banner)
                                 ?? GetMediaOrText(pickedMovieInfoData?.Background)
                                 ?? GetMediaOrText(pickedMovieInfoData?.HinhNen)
                                 ?? GetMediaOrText(pickedMovieInfoData?.Banner)
                                 ?? "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?auto=format&fit=crop&w=1600&q=80";

                bool isFeatured = GetBoolFieldValue(item.Content, "Remarkable", "IsFeatured", "NoiBat")
                               || (pickedMovieInfoContent != null && GetBoolFieldValue(pickedMovieInfoContent, "Remarkable", "IsFeatured", "NoiBat"));

                double rawRating = GetDoubleField(movieData?.Evaluate ?? pickedMovieInfoData?.Evaluate, 0.0);
                double rating = rawRating;
                if (rating > 0.0)
                {
                    while (rating > 5.0)
                    {
                        rating /= 2.0;
                    }
                    rating = Math.Clamp(Math.Round(rating, 1), 1.0, 5.0);
                }
                else
                {
                    rating = 0.0;
                }

                if (_commentService != null)
                {
                    double userAvg = await _commentService.GetAverageRatingAsync(item.ContentItemId, 0.0);
                    if (userAvg > 0.0)
                    {
                        rating = userAvg;
                    }
                }
                string resolution = GetFieldText(movieData?.Resolution ?? pickedMovieInfoData?.Resolution) ?? "4K Ultra HD";
                int year = GetIntField(movieData?.YearOfProduction ?? pickedMovieInfoData?.YearOfProduction, 2026);

                var extracted = ExtractCategoryNamesAndIds((object)movieData, (object)item.Content, categoryMap);
                var genresList = new List<string>(extracted.Names);
                var genreIds = new List<string>(extracted.Ids);

                if (pickedMovieInfoData != null && pickedMovieInfoContent != null)
                {
                    var extractedPicked = ExtractCategoryNamesAndIds((object)pickedMovieInfoData, (object)pickedMovieInfoContent, categoryMap);
                    foreach (var n in extractedPicked.Names) if (!genresList.Contains(n, StringComparer.OrdinalIgnoreCase)) genresList.Add(n);
                    foreach (var id in extractedPicked.Ids) if (!genreIds.Contains(id, StringComparer.OrdinalIgnoreCase)) genreIds.Add(id);
                }

                // Read IsInCinema field (BooleanField for Phim Chiếu Rạp)
                bool isCinema = GetBoolFieldValue(item.Content, "IsInCinema", "IsCinema", "ChieuRap")
                             || (pickedMovieInfoContent != null && GetBoolFieldValue(pickedMovieInfoContent, "IsInCinema", "IsCinema", "ChieuRap"));

                // Read NowShowing field (BooleanField for Phim Đang Chiếu)
                bool isNowShowing = GetBoolFieldValue(item.Content, "NowShowing", "IsNowShowing", "DangChieu", "DangChieuRap", "NowShowingMovie")
                                 || (pickedMovieInfoContent != null && GetBoolFieldValue(pickedMovieInfoContent, "NowShowing", "IsNowShowing", "DangChieu", "DangChieuRap", "NowShowingMovie"));

                // Read ComingShowMovie field (BooleanField for Phim Sắp Chiếu)
                bool isComingSoon = GetBoolFieldValue(item.Content, "ComingShowMovie", "ComingShow", "IsComingSoon", "SapChieu", "SapChieuRap", "ComingSoon", "ComingSoonMovie")
                                 || (pickedMovieInfoContent != null && GetBoolFieldValue(pickedMovieInfoContent, "ComingShowMovie", "ComingShow", "IsComingSoon", "SapChieu", "SapChieuRap", "ComingSoon", "ComingSoonMovie"));

                // Read ComingShowDate field (TextField for ngày khởi chiếu)
                string comingShowDate = GetFieldText(movieData?.ComingShowDate ?? pickedMovieInfoData?.ComingShowDate)
                                     ?? GetFieldText(movieData?.ComingShowDateText ?? pickedMovieInfoData?.ComingShowDateText)
                                     ?? GetFieldText(movieData?.ComingDate ?? pickedMovieInfoData?.ComingDate)
                                     ?? GetFieldText(movieData?.NgayChieu ?? pickedMovieInfoData?.NgayChieu)
                                     ?? string.Empty;

                if (isComingSoon)
                {
                    isNowShowing = false;
                }

                // Read IsPartMovie field (BooleanField for Phim Bộ)
                bool isPartMovie = GetBoolFieldValue(item.Content, "IsPartMovie", "IsPart", "IsSeries", "PhimBo", "PhimBoField", "PartMovie", "IsMoviePart", "MoviePart")
                                || (pickedMovieInfoContent != null && GetBoolFieldValue(pickedMovieInfoContent, "IsPartMovie", "IsPart", "IsSeries", "PhimBo", "PhimBoField", "PartMovie", "IsMoviePart", "MoviePart"))
                                || duration.ToLowerInvariant().Contains("tập")
                                || duration.ToLowerInvariant().Contains("tap");

                // Read Price field (TextField or NumericField for Giá phim)
                string? priceRaw = GetStringFieldValue(item.Content, "Price", "Gia", "GiaVe", "GiaPhim", "MoviePrice", "TicketPrice")
                                ?? (pickedMovieInfoContent != null ? GetStringFieldValue(pickedMovieInfoContent, "Price", "Gia", "GiaVe", "GiaPhim", "MoviePrice", "TicketPrice") : null);

                decimal price = 0;
                if (!string.IsNullOrWhiteSpace(priceRaw))
                {
                    string digitsOnly = System.Text.RegularExpressions.Regex.Replace(priceRaw, @"[^\d]", "");
                    if (decimal.TryParse(digitsOnly, out decimal pVal))
                    {
                        price = pVal;
                    }
                }

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
                    Duration = duration,
                    Rating = rating,
                    AgeRating = ageRating,
                    Quality = resolution,
                    LanguageMode = language,
                    Country = country,
                    Director = director,
                    Cast = new List<string> { "Diễn viên" },
                    GenreIds = genreIds,
                    GenreNames = genresList,
                    IsFeatured = isFeatured,
                    FeaturedOrder = 1,
                    IsCinema = isCinema,
                    IsNowShowing = isNowShowing,
                    IsComingSoon = isComingSoon,
                    ComingShowDate = comingShowDate,
                    Price = price,
                    IsSeries = isPartMovie,
                    EpisodeInfo = duration,
                    ViewsCount = viewsCount > 0 ? viewsCount : 1000,
                    ViewsText = viewsText,
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

    private static (List<string> Names, List<string> Ids) ExtractCategoryNamesAndIds(dynamic movieData, dynamic fullContent, Dictionary<string, string> categoryMap)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string[] fieldNames = new[] { "Category", "Categories", "Genre", "Genres", "TheLoai", "TheLoaiField", "CategoryField", "MovieInfo", "MovieInfoField", "MovieInfoPicker" };

        foreach (var fname in fieldNames)
        {
            dynamic? field = null;
            try { field = movieData?[fname] ?? fullContent?[fname]; } catch { }
            if (field == null) continue;

            // 1. Text / Html / Value field
            string? textVal = GetFieldText(field);
            if (!string.IsNullOrWhiteSpace(textVal))
            {
                var parts = textVal.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    string trimmed = p.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        names.Add(trimmed);
                    }
                }
            }

            // 2. TaxonomyField / ContentPickerField (TermContentItemIds / ContentItemIds)
            try
            {
                dynamic? termIds = field.TermContentItemIds ?? field.ContentItemIds;
                if (termIds != null)
                {
                    foreach (var tid in termIds)
                    {
                        string idStr = tid.ToString();
                        ids.Add(idStr);
                        if (categoryMap.TryGetValue(idStr, out var mappedName))
                        {
                            names.Add(mappedName);
                        }
                    }
                }
            }
            catch { }

            // 3. Json string inspection fallback
            try
            {
                string json = field.ToString();
                if (!string.IsNullOrWhiteSpace(json) && (json.Contains("TermContentItemIds") || json.Contains("ContentItemIds")))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Name.Equals("TermContentItemIds", StringComparison.OrdinalIgnoreCase) ||
                            prop.Name.Equals("ContentItemIds", StringComparison.OrdinalIgnoreCase))
                        {
                            if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var elem in prop.Value.EnumerateArray())
                                {
                                    string idStr = elem.GetString() ?? "";
                                    if (!string.IsNullOrWhiteSpace(idStr))
                                    {
                                        ids.Add(idStr);
                                        if (categoryMap.TryGetValue(idStr, out var mappedName))
                                        {
                                            names.Add(mappedName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        if (!names.Any())
        {
            names.Add("Hành động");
        }

        var namesList = names.ToList();

        // Build robust IDs list for matching: unaccented slug, accented slug, raw lower
        foreach (var n in namesList)
        {
            string cleanSlug = RemoveDiacritics(n).ToLowerInvariant().Replace(" ", "-");
            string rawSlug = n.ToLowerInvariant().Replace(" ", "-");
            string rawLower = n.ToLowerInvariant();
            ids.Add(cleanSlug);
            ids.Add(rawSlug);
            ids.Add(rawLower);
        }

        return (namesList, ids.ToList());
    }

    public async Task<List<Movie>> GetFeaturedMoviesAsync(int count = 5)
    {
        var movies = await GetOrchardMoviesInternalAsync();
        // CHỈ lấy phim đã tick "Nổi bật" (IsFeatured = true)
        var featured = movies.Where(m => m.IsFeatured).Take(count).ToList();
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

    private IEnumerable<Movie> FilterMoviesQueryInternal(
        IEnumerable<Movie> movies,
        string? searchKeyword,
        string? genreId,
        string? country,
        int? year,
        bool? isSeries,
        bool isRegularOnly)
    {
        IEnumerable<Movie> query = movies;

        if (isRegularOnly)
        {
            query = query.Where(m => !m.IsCinema);
            if (!isSeries.HasValue)
            {
                query = query.Where(m => !m.IsSeries);
            }
        }

        if (isSeries.HasValue)
        {
            query = query.Where(m => m.IsSeries == isSeries.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = RemoveDiacritics(searchKeyword.Trim().ToLowerInvariant());
            query = query.Where(m =>
                RemoveDiacritics(m.Title.ToLowerInvariant()).Contains(kw) ||
                RemoveDiacritics(m.Description.ToLowerInvariant()).Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(genreId) && genreId != "all")
        {
            string normalizedGenreId = RemoveDiacritics(genreId.Trim().ToLowerInvariant());
            query = query.Where(m => m.GenreIds.Any(gid =>
                RemoveDiacritics(gid.ToLowerInvariant()) == normalizedGenreId));
        }

        if (!string.IsNullOrWhiteSpace(country) && country != "all")
        {
            string cNorm = RemoveDiacritics(country.Trim().ToLowerInvariant());
            query = query.Where(m => {
                if (string.IsNullOrWhiteSpace(m.Country)) return false;
                string mNorm = RemoveDiacritics(m.Country.Trim().ToLowerInvariant());
                if (mNorm == cNorm || mNorm.Contains(cNorm) || cNorm.Contains(mNorm)) return true;
                if (cNorm.Contains("au my") && (mNorm.Contains("my") || mNorm.Contains("hoa ky") || mNorm.Contains("anh") || mNorm.Contains("phap") || mNorm.Contains("au my"))) return true;
                return false;
            });
        }

        if (year.HasValue && year.Value > 0)
        {
            query = query.Where(m => m.ReleaseYear == year.Value);
        }

        return query;
    }

    public async Task<List<Movie>> GetMoviesAsync(
        string? searchKeyword = null,
        string? genreId = null,
        string? country = null,
        int? year = null,
        bool? isSeries = null,
        string? sortBy = null,
        int page = 1,
        int pageSize = 12,
        bool isRegularOnly = false)
    {
        var movies = await GetOrchardMoviesInternalAsync();
        var query = FilterMoviesQueryInternal(movies, searchKeyword, genreId, country, year, isSeries, isRegularOnly);

        query = (sortBy?.ToLowerInvariant()) switch
        {
            "popular" or "views" => query.OrderByDescending(m => m.ViewsCount),
            "rating" or "top" => query.OrderByDescending(m => m.Rating).ThenByDescending(m => m.ViewsCount),
            "title" => query.OrderBy(m => m.Title),
            "newest" => query.OrderByDescending(m => m.ReleaseYear).ThenByDescending(m => m.CreatedAt),
            _ => query.OrderByDescending(m => m.ReleaseYear).ThenByDescending(m => m.CreatedAt)
        };

        return query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    }

    public async Task<int> GetMoviesCountAsync(
        string? searchKeyword = null,
        string? genreId = null,
        string? country = null,
        int? year = null,
        bool? isSeries = null,
        bool isRegularOnly = false)
    {
        var movies = await GetOrchardMoviesInternalAsync();
        var query = FilterMoviesQueryInternal(movies, searchKeyword, genreId, country, year, isSeries, isRegularOnly);
        return query.Count();
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

    // Flexible Field Helpers
    private static string? GetFlexibleFieldText(dynamic? data, params string[] fieldNames)
    {
        if (data == null) return null;
        try
        {
            string jsonStr = "";
            try { jsonStr = data.ToJsonString(); }
            catch
            {
                try { jsonStr = System.Text.Json.JsonSerializer.Serialize((object)data); }
                catch { return null; }
            }

            if (string.IsNullOrWhiteSpace(jsonStr) || jsonStr.StartsWith("System.")) return null;

            using var doc = System.Text.Json.JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;

            if (root.ValueKind != System.Text.Json.JsonValueKind.Object) return null;

            foreach (var targetName in fieldNames)
            {
                string targetNorm = RemoveDiacritics(targetName).ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");

                foreach (var prop in root.EnumerateObject())
                {
                    string propNorm = RemoveDiacritics(prop.Name).ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
                    
                    bool isMatch = propNorm.Equals(targetNorm, StringComparison.OrdinalIgnoreCase) || 
                                   propNorm.Equals(targetNorm + "field", StringComparison.OrdinalIgnoreCase) ||
                                   targetNorm.Equals(propNorm + "field", StringComparison.OrdinalIgnoreCase) ||
                                   propNorm.Contains(targetNorm, StringComparison.OrdinalIgnoreCase);

                    if (isMatch)
                    {
                        var val = prop.Value;
                        if (val.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            if (val.TryGetProperty("Text", out var textProp) && textProp.ValueKind != System.Text.Json.JsonValueKind.Null && textProp.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                            {
                                string t = textProp.ToString();
                                if (!string.IsNullOrWhiteSpace(t) && t != "null") return CleanHtmlOrPath(t);
                            }
                            if (val.TryGetProperty("Value", out var valProp) && valProp.ValueKind != System.Text.Json.JsonValueKind.Null && valProp.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                            {
                                string v = valProp.ToString();
                                if (!string.IsNullOrWhiteSpace(v) && v != "null") return CleanHtmlOrPath(v);
                            }
                            if (val.TryGetProperty("Html", out var htmlProp) && htmlProp.ValueKind != System.Text.Json.JsonValueKind.Null && htmlProp.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                            {
                                string h = htmlProp.ToString();
                                if (!string.IsNullOrWhiteSpace(h) && h != "null") return CleanHtmlOrPath(h);
                            }
                        }
                        else if (val.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            string s = val.GetString() ?? "";
                            if (!string.IsNullOrWhiteSpace(s)) return CleanHtmlOrPath(s);
                        }
                        else if (val.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            return val.ToString();
                        }
                    }
                }
            }
        }
        catch { }
        return null;
    }

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
    public static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Bước 1: Thay thế trực tiếp các ký tự tiếng Việt độc lập không phải combining mark
        // (FormD normalization không xử lý được những ký tự này)
        var sb1 = new System.Text.StringBuilder(text.Length);
        foreach (var c in text)
        {
            sb1.Append(c switch
            {
                'đ' => 'd',
                'Đ' => 'D',
                _ => c
            });
        }

        // Bước 2: Xóa combining diacritical marks (à, á, â, ã, ä, ả, ạ, ắ, ặ, v.v.)
        var normalized = sb1.ToString().Normalize(System.Text.NormalizationForm.FormD);
        var sb2 = new System.Text.StringBuilder();
        foreach (var c in normalized)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb2.Append(c);
        }
        return sb2.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    /// <summary>
    /// Đọc boolean field từ Orchard Core ContentItem.
    /// CHỈ tìm trong phần "Movie" (cấp 1) để tránh false positive từ các phần metadata khác.
    /// </summary>
    private static bool GetBoolFieldValue(dynamic content, params string[] fieldNames)
    {
        try
        {
            if (content == null) return false;

            string jsonStr;
            try { jsonStr = content.ToJsonString(); }
            catch
            {
                try { jsonStr = System.Text.Json.JsonSerializer.Serialize((object)content); }
                catch { return false; }
            }

            if (string.IsNullOrWhiteSpace(jsonStr) || jsonStr.StartsWith("System.")) return false;

            using var doc = System.Text.Json.JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;

            // Chỉ tìm trong phần "Movie" hoặc "MovieInfo" của ContentItem (không đệ quy sâu)
            System.Text.Json.JsonElement moviePart;
            bool hasMoviePart = root.TryGetProperty("Movie", out moviePart) || root.TryGetProperty("MovieInfo", out moviePart);

            // Tìm field trong Movie part trước (ưu tiên)
            if (hasMoviePart && moviePart.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                bool? result = CheckBoolFieldShallow(moviePart, fieldNames);
                if (result.HasValue) return result.Value;
            }

            // Fallback: tìm ở cấp root (cho trường hợp không wrap trong Movie part)
            {
                bool? result = CheckBoolFieldShallow(root, fieldNames);
                if (result.HasValue) return result.Value;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tìm boolean field ở cấp NÔNG (1 cấp), không đệ quy.
    /// Trả về true/false nếu tìm thấy field, null nếu không thấy.
    /// </summary>
    private static bool? CheckBoolFieldShallow(System.Text.Json.JsonElement element, string[] fieldNames)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.Object) return null;

        foreach (var prop in element.EnumerateObject())
        {
            if (!fieldNames.Any(f => f.Equals(prop.Name, StringComparison.OrdinalIgnoreCase)))
                continue;

            // Trường hợp field trực tiếp là bool: "FieldName": true
            if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.True) return true;
            if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.False) return false;

            // Trường hợp BooleanField Orchard Core: "FieldName": { "Value": true }
            if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (prop.Value.TryGetProperty("Value", out var valProp) ||
                    prop.Value.TryGetProperty("value", out valProp))
                {
                    if (valProp.ValueKind == System.Text.Json.JsonValueKind.True) return true;
                    if (valProp.ValueKind == System.Text.Json.JsonValueKind.False) return false;
                    if (valProp.ValueKind == System.Text.Json.JsonValueKind.String &&
                        bool.TryParse(valProp.GetString(), out bool bParsed)) return bParsed;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Đọc string/numeric field từ Orchard Core ContentItem.
    /// Tìm linh hoạt trong phần "Movie", "MovieInfo" và root element.
    /// </summary>
    private static string? GetStringFieldValue(dynamic content, params string[] fieldNames)
    {
        try
        {
            if (content == null) return null;

            string jsonStr;
            try { jsonStr = content.ToJsonString(); }
            catch
            {
                try { jsonStr = System.Text.Json.JsonSerializer.Serialize((object)content); }
                catch { return null; }
            }

            if (string.IsNullOrWhiteSpace(jsonStr) || jsonStr.StartsWith("System.")) return null;

            using var doc = System.Text.Json.JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;

            System.Text.Json.JsonElement moviePart;
            bool hasMoviePart = root.TryGetProperty("Movie", out moviePart) || root.TryGetProperty("MovieInfo", out moviePart);

            if (hasMoviePart && moviePart.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                string? val = CheckStringFieldShallow(moviePart, fieldNames);
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }

            return CheckStringFieldShallow(root, fieldNames);
        }
        catch
        {
            return null;
        }
    }

    private static string? CheckStringFieldShallow(System.Text.Json.JsonElement element, string[] fieldNames)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.Object) return null;

        foreach (var prop in element.EnumerateObject())
        {
            if (!fieldNames.Any(f => f.Equals(prop.Name, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                return prop.Value.GetString();

            if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                return prop.Value.GetRawText();

            if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (prop.Value.TryGetProperty("Text", out var textProp) ||
                    prop.Value.TryGetProperty("text", out textProp) ||
                    prop.Value.TryGetProperty("Value", out textProp) ||
                    prop.Value.TryGetProperty("value", out textProp))
                {
                    if (textProp.ValueKind == System.Text.Json.JsonValueKind.String)
                        return textProp.GetString();
                    if (textProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                        return textProp.GetRawText();
                }
            }
        }
        return null;
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
