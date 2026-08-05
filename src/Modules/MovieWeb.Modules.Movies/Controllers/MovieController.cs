using Microsoft.AspNetCore.Mvc;
using MovieWeb.Core.Models;
using MovieWeb.Core.Services;
using MovieWeb.Modules.Movies.Models;
using System.Security.Claims;

namespace MovieWeb.Modules.Movies.Controllers;

public class MovieController : Controller
{
    private readonly IMovieService _movieService;
    private readonly ICommentService _commentService;
    private readonly IBookmarkService? _bookmarkService;
    private readonly IUserService? _userService;
    private readonly ICheckinService? _checkinService;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public MovieController(
        IMovieService movieService, 
        ICommentService commentService, 
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        IBookmarkService? bookmarkService = null, 
        IUserService? userService = null,
        ICheckinService? checkinService = null)
    {
        _movieService = movieService;
        _commentService = commentService;
        _configuration = configuration;
        _bookmarkService = bookmarkService;
        _userService = userService;
        _checkinService = checkinService;
    }

    [HttpGet]
    [Route("phim")]
    public async Task<IActionResult> Index(
        string? q = null,
        string? genre = null,
        string? country = null,
        int? year = null,
        bool? series = null,
        string? sort = "newest",
        int page = 1)
    {
        var movies = await _movieService.GetMoviesAsync(
            searchKeyword: q,
            genreId: genre,
            country: country,
            year: year,
            isSeries: series,
            sortBy: sort,
            page: page,
            pageSize: 12,
            isRegularOnly: true);

        var totalItems = await _movieService.GetMoviesCountAsync(
            searchKeyword: q,
            genreId: genre,
            country: country,
            year: year,
            isSeries: series,
            isRegularOnly: true);

        var genres = await _movieService.GetAllGenresAsync();
        Genre? currentGenre = null;
        if (!string.IsNullOrEmpty(genre) && genre != "all")
        {
            currentGenre = await _movieService.GetGenreByIdOrSlugAsync(genre);
        }

        var model = new MovieFilterViewModel
        {
            Movies = movies,
            Genres = genres,
            SelectedGenre = genre ?? "all",
            SelectedCountry = country ?? "all",
            SelectedYear = year,
            SelectedIsSeries = series,
            SearchKeyword = q ?? string.Empty,
            SortBy = sort ?? "newest",
            CurrentPage = page,
            PageSize = 12,
            TotalItems = totalItems,
            CurrentGenreInfo = currentGenre
        };

        return View(model);
    }

    [HttpGet]
    [Route("phim/{id}")]
    public async Task<IActionResult> Detail(string id)
    {
        var movie = await _movieService.GetMovieByIdOrSlugAsync(id);
        if (movie == null)
        {
            return NotFound();
        }

        await _movieService.IncrementViewsAsync(movie.Id);

        var related = await _movieService.GetRelatedMoviesAsync(movie.Id, 6);
        ViewBag.RelatedMovies = related;

        if (_commentService != null)
        {
            ViewBag.Comments = await _commentService.GetCommentsByMovieIdAsync(movie.Id);
            ViewBag.CommentsCount = await _commentService.GetCommentsCountAsync(movie.Id);
            ViewBag.RatingBreakdown = await _commentService.GetRatingBreakdownAsync(movie.Id);
            ViewBag.AverageRating = await _commentService.GetAverageRatingAsync(movie.Id, 0.0);
        }

        // Pass current user ID & bookmark status for view
        string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        ViewBag.CurrentUserId = currentUserId;

        if (_bookmarkService != null && !string.IsNullOrEmpty(currentUserId))
        {
            ViewBag.IsBookmarked = await _bookmarkService.IsBookmarkedAsync(currentUserId, movie.Id);
        }
        else
        {
            ViewBag.IsBookmarked = false;
        }

        bool isPurchased = false;
        if (_userService != null && !string.IsNullOrEmpty(currentUserId))
        {
            isPurchased = await _userService.HasPurchasedMovieAsync(currentUserId, movie.Id);
        }
        ViewBag.IsPurchased = isPurchased;

        return View(movie);
    }

    [HttpGet]
    [Route("bang-xep-hang")]
    [Route("rankings")]
    public async Task<IActionResult> Rankings(
        string type = "views",
        string period = "all",
        string? genre = null)
    {
        var rankedMovies = await _movieService.GetRankedMoviesAsync(
            criteria: type,
            period: period,
            genreId: genre,
            count: 50);

        var genres = await _movieService.GetAllGenresAsync();
        Genre? currentGenre = null;
        if (!string.IsNullOrEmpty(genre) && genre != "all")
        {
            currentGenre = await _movieService.GetGenreByIdOrSlugAsync(genre);
        }

        var model = new RankingsViewModel
        {
            RankedMovies = rankedMovies,
            Genres = genres,
            ActiveCriteria = type ?? "views",
            ActivePeriod = period ?? "all",
            SelectedGenre = genre ?? "all",
            SelectedGenreInfo = currentGenre
        };

        return View(model);
    }

    [HttpGet]
    [Route("api/rankings/filter")]
    public async Task<IActionResult> FilterRankings(
        string type = "views",
        string period = "all",
        string? genre = null)
    {
        var rankedMovies = await _movieService.GetRankedMoviesAsync(
            criteria: type,
            period: period,
            genreId: genre,
            count: 50);

        var data = rankedMovies.Select((m, index) => new
        {
            rank = index + 1,
            id = m.Id,
            title = m.Title,
            originalTitle = m.OriginalTitle,
            slug = m.Slug,
            posterUrl = m.PosterUrl,
            year = m.ReleaseYear,
            rating = m.Rating,
            viewsCount = m.ViewsCount,
            formattedViews = m.ViewsCount >= 1000000 ? $"{m.ViewsCount / 1000000.0:0.##}M" : m.ViewsCount.ToString("N0", new System.Globalization.CultureInfo("vi-VN")),
            genres = string.Join(", ", m.GenreNames),
            isCinema = m.IsCinema,
            isSeries = m.IsSeries,
            price = m.Price,
            formattedPrice = m.FormattedPrice,
            requiresPurchase = m.RequiresPurchase,
            quality = m.Quality,
            languageMode = m.LanguageMode
        });

        return Json(new { success = true, count = data.Count(), data });
    }

    [HttpGet]
    [Route("api/movies/quick-search")]
    public async Task<IActionResult> QuickSearch([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return Json(new { success = true, data = new object[] { } });
        }

        var allMovies = await _movieService.GetAllMoviesAsync();
        var kw = RemoveDiacritics(q.Trim().ToLowerInvariant());

        var filtered = allMovies
            .Where(m => RemoveDiacritics(m.Title.ToLowerInvariant()).Contains(kw))
            .Take(6)
            .ToList();

        var results = filtered.Select(m => new
        {
            id = m.Id,
            slug = m.Slug,
            title = m.Title,
            originalTitle = m.OriginalTitle,
            posterUrl = m.PosterUrl,
            year = m.ReleaseYear,
            rating = m.Rating,
            genres = string.Join(", ", m.GenreNames)
        });

        return Json(new { success = true, data = results });
    }

    [HttpGet]
    [Route("api/debug/movies")]
    public async Task<IActionResult> DebugMovies()
    {
        var allMovies = await _movieService.GetAllMoviesAsync();
        var result = allMovies.Select(m => new
        {
            id = m.Id,
            title = m.Title,
            trailerUrl = m.TrailerUrl,
            isCinema = m.IsCinema,
            isNowShowing = m.IsNowShowing,
            isComingSoon = m.IsComingSoon,
            price = m.Price,
            formattedPrice = m.FormattedPrice,
            requiresPurchase = m.RequiresPurchase,
            isSeries = m.IsSeries,
            isFeatured = m.IsFeatured,
            country = m.Country,
            year = m.ReleaseYear
        });
        return Json(result);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Route("api/movies/buy")]
    public async Task<IActionResult> BuyMovie([FromBody] BuyMovieRequest? request)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập để mua vé xem phim." });
        }

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        string movieId = request?.MovieId ?? string.Empty;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(movieId))
        {
            return Json(new { success = false, message = "Thông tin phim không hợp lệ." });
        }

        if (_userService == null)
        {
            return Json(new { success = false, message = "Dịch vụ thanh toán tạm thời không khả dụng." });
        }

        var result = await _userService.BuyMovieAsync(userId, movieId);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Route("api/movies/notify")]
    public async Task<IActionResult> RegisterNotification([FromBody] NotifyMovieRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.MovieId))
        {
            return Json(new { success = false, message = "Vui lòng nhập đầy đủ Email và Phim." });
        }

        try
        {
            string appDataDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }

            string filePath = Path.Combine(appDataDir, "movie_notifications.json");
            List<NotifyEntry> list = new();
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    string existingJson = await System.IO.File.ReadAllTextAsync(filePath);
                    list = System.Text.Json.JsonSerializer.Deserialize<List<NotifyEntry>>(existingJson) ?? new();
                }
                catch { }
            }

            var entry = new NotifyEntry
            {
                MovieId = request.MovieId,
                MovieTitle = request.MovieTitle,
                Name = request.Name,
                Email = request.Email,
                CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };

            list.Add(entry);

            string updatedJson = System.Text.Json.JsonSerializer.Serialize(list, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, updatedJson);

            // Log sent email record to App_Data/sent_emails.log
            string logPath = Path.Combine(appDataDir, "sent_emails.log");
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] REGISTRATION RECORDED: {request.Email} ({request.Name}) | Movie: {request.MovieTitle}\n";
            await System.IO.File.AppendAllTextAsync(logPath, logLine);

            // Attempt sending real SMTP email if SMTP Username & Password are provided
            var emailResult = await SendRealEmailAsync(request.Email, request.Name, request.MovieTitle);

            string responseMessage = emailResult.Success
                ? $"Đăng ký thành công! {emailResult.Message}"
                : $"Đã đăng ký nhận thông báo cho email {request.Email}. (Lưu ý: {emailResult.Message})";

            return Json(new
            {
                success = true,
                message = responseMessage,
                emailSent = emailResult.Success
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi hệ thống khi lưu thông tin: " + ex.Message });
        }
    }

    private async Task<(bool Success, string Message)> SendRealEmailAsync(string toEmail, string toName, string movieTitle)
    {
        var smtpHost = _configuration["Smtp:Host"] ?? "smtp.gmail.com";
        var smtpPortStr = _configuration["Smtp:Port"] ?? "587";
        int.TryParse(smtpPortStr, out int smtpPort);
        if (smtpPort <= 0) smtpPort = 587;

        var username = (_configuration["Smtp:Username"] ?? string.Empty).Trim();
        var password = (_configuration["Smtp:Password"] ?? string.Empty).Replace(" ", "").Trim();
        var senderEmail = _configuration["Smtp:SenderEmail"] ?? (string.IsNullOrWhiteSpace(username) ? "phanthinh571@gmail.com" : username);
        var senderName = _configuration["Smtp:SenderName"] ?? "MovieOnline Cinema";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Chưa điền Gmail Username / Mật khẩu ứng dụng trong appsettings.json. Vui lòng nhập thông tin Gmail để nhận email thực tế.");
        }

        try
        {
            var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress(senderName, senderEmail));
            message.To.Add(new MimeKit.MailboxAddress(toName.Trim(), toEmail.Trim()));
            message.Subject = $"[MovieOnline] Xác Nhận Đăng Ký Nhận Thông Báo Phim: {movieTitle}";

            var bodyBuilder = new MimeKit.BodyBuilder
            {
                HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; background-color: #181818; color: #ffffff; padding: 25px; border-radius: 12px;'>
                        <h2 style='color: #e50914;'>🎬 MovieOnline - Thông Báo Phim Chiếu Rạp</h2>
                        <p>Xin chào <strong>{toName}</strong>,</p>
                        <p>Cảm ơn bạn đã đăng ký nhận thông báo khởi chiếu cho bộ phim: <strong style='color: #f5c518;'>{movieTitle}</strong>.</p>
                        <p>Ngay khi bộ phim chính thức công chiếu tại rạp và có mặt trên MovieOnline, chúng tôi sẽ gửi email thông báo tới bạn ngay lập tức!</p>
                        <hr style='border: 0; border-top: 1px solid #333; margin: 20px 0;' />
                        <p style='font-size: 0.85rem; color: #888888;'>Trân trọng,<br/>Đội ngũ MovieOnline Server 4K</p>
                    </div>"
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            await smtp.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(username, password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            return (true, "Đã gửi Email xác nhận thành công tới hòm thư Gmail của bạn!");
        }
        catch (Exception ex)
        {
            return (false, "Lỗi khi kết nối Gmail SMTP server: " + ex.Message);
        }
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var sb1 = new System.Text.StringBuilder(text.Length);
        foreach (var c in text)
            sb1.Append(c == 'đ' ? 'd' : c == 'Đ' ? 'D' : c);
        var normalized = sb1.ToString().Normalize(System.Text.NormalizationForm.FormD);
        var sb2 = new System.Text.StringBuilder();
        foreach (var c in normalized)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb2.Append(c);
        return sb2.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    // ============= Checkin & Voucher System =============

    [HttpGet]
    [Route("diem-danh")]
    public IActionResult Checkin()
    {
        ViewData["Title"] = "Điểm Danh Nhận Quà";
        ViewData["ActivePage"] = "Checkin";
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Route("api/checkin")]
    public async Task<IActionResult> ApiCheckin()
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
            return Json(new { success = false, message = "Vui lòng đăng nhập để điểm danh." });

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (string.IsNullOrEmpty(userId) || _checkinService == null)
            return Json(new { success = false, message = "Dịch vụ điểm danh không khả dụng." });

        var result = await _checkinService.CheckinAsync(userId);
        return Json(new
        {
            success = result.Success,
            message = result.Message,
            totalDays = result.TotalDaysThisMonth,
            alreadyCheckedIn = result.AlreadyCheckedIn,
            reward = result.RewardVoucher != null ? new
            {
                voucherCode = result.RewardVoucher.VoucherCode,
                discountPercent = result.RewardVoucher.DiscountPercent,
                expiresAt = result.RewardVoucher.ExpiresAt.ToString("dd/MM/yyyy")
            } : null
        });
    }

    [HttpGet]
    [Route("api/checkin/status")]
    public async Task<IActionResult> ApiCheckinStatus()
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
            return Json(new { success = false, message = "Vui lòng đăng nhập." });

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (string.IsNullOrEmpty(userId) || _checkinService == null)
            return Json(new { success = false });

        var status = await _checkinService.GetCheckinStatusAsync(userId);
        return Json(new
        {
            success = true,
            totalDays = status.TotalDaysThisMonth,
            checkedDays = status.CheckedDays,
            checkedToday = status.CheckedToday,
            currentMonth = status.CurrentMonth,
            currentYear = status.CurrentYear,
            currentDay = status.CurrentDay,
            daysInMonth = status.DaysInMonth,
            firstDayOffset = status.FirstDayOffset,
            milestones = status.Milestones.Select(m => new
            {
                day = m.Day,
                discountPercent = m.DiscountPercent,
                reached = m.Reached,
                voucherCode = m.VoucherCode,
                label = m.Label
            }),
            vouchers = status.ActiveVouchers.Select(v => new
            {
                code = v.VoucherCode,
                discountPercent = v.DiscountPercent,
                expiresAt = v.ExpiresAt.ToString("dd/MM/yyyy"),
                isUsed = v.IsUsed,
                milestoneDay = v.MilestoneDay
            })
        });
    }

    [HttpGet]
    [Route("api/vouchers")]
    public async Task<IActionResult> ApiGetVouchers()
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
            return Json(new { success = false, message = "Vui lòng đăng nhập." });

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (string.IsNullOrEmpty(userId) || _checkinService == null)
            return Json(new { success = true, vouchers = new object[0] });

        var vouchers = await _checkinService.GetUserVouchersAsync(userId);
        return Json(new
        {
            success = true,
            vouchers = vouchers.Select(v => new
            {
                code = v.VoucherCode,
                discountPercent = v.DiscountPercent,
                originalPrice = v.OriginalPrice,
                discountedPrice = v.DiscountedPrice,
                expiresAt = v.ExpiresAt.ToString("dd/MM/yyyy"),
                milestoneDay = v.MilestoneDay
            })
        });
    }

    // ============= MoMo Payment Integration =============

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Route("api/movies/momo-create")]
    public async Task<IActionResult> MoMoCreatePayment([FromBody] BuyMovieRequest? request)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập để mua vé xem phim." });
        }

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        string movieId = request?.MovieId ?? string.Empty;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(movieId))
        {
            return Json(new { success = false, message = "Thông tin phim không hợp lệ." });
        }

        // Get movie info for amount
        var movie = await _movieService.GetMovieByIdOrSlugAsync(movieId);
        if (movie == null)
        {
            return Json(new { success = false, message = "Không tìm thấy phim." });
        }

        long amount = (long)movie.Price;
        if (amount < 1000) amount = 10000; // MoMo minimum is 1000 VND

        // Apply voucher if provided
        string? appliedVoucherCode = request?.VoucherCode;
        int appliedDiscount = 0;
        if (!string.IsNullOrWhiteSpace(appliedVoucherCode) && _checkinService != null)
        {
            var voucher = await _checkinService.ValidateVoucherAsync(userId, appliedVoucherCode);
            if (voucher != null)
            {
                long discountedAmount = (long)(movie.Price * (1 - voucher.DiscountPercent / 100m));
                if (discountedAmount < 1000) discountedAmount = 1000; // MoMo minimum
                amount = discountedAmount;
                appliedDiscount = voucher.DiscountPercent;
            }
        }

        // MoMo config
        var partnerCode = _configuration["MoMo:PartnerCode"] ?? "MOMO";
        var accessKey = _configuration["MoMo:AccessKey"] ?? "F8BBA842ECF85";
        var secretKey = _configuration["MoMo:SecretKey"] ?? "K951B6PE1waDMi640xX08PD3vg6EkVlz";
        var endpoint = _configuration["MoMo:Endpoint"] ?? "https://test-payment.momo.vn/v2/gateway/api/create";

        // Build dynamic return URL based on current request
        var scheme = Request.Scheme;
        var host = Request.Host.ToString();
        var baseUrl = $"{scheme}://{host}";
        var redirectUrl = $"{baseUrl}/api/movies/momo-return";
        var ipnUrl = $"{baseUrl}/api/movies/momo-ipn";

        var orderId = $"MOVIE_{movieId}_{userId.Substring(0, Math.Min(8, userId.Length))}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var requestId = Guid.NewGuid().ToString();
        var orderInfo = $"Thanh toan ve xem phim: {RemoveDiacritics(movie.Title)}";
        var requestType = "captureWallet";
        var extraData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(new { userId, movieId })
        ));

        // Build signature
        var rawSignature = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";

        string signature;
        using (var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secretKey)))
        {
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawSignature));
            signature = BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        var requestBody = new
        {
            partnerCode,
            accessKey,
            requestId,
            amount,
            orderId,
            orderInfo,
            redirectUrl,
            ipnUrl,
            extraData,
            requestType,
            lang = "vi",
            signature
        };

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            var response = await httpClient.PostAsync(endpoint, jsonContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[MoMo] Response: {responseBody}");

            using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var resultCode = root.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1;
            var payUrl = root.TryGetProperty("payUrl", out var pu) ? pu.GetString() : null;
            var qrCodeUrl = root.TryGetProperty("qrCodeUrl", out var qr) ? qr.GetString() : null;

            if (resultCode == 0 && !string.IsNullOrEmpty(payUrl))
            {
                // Also unlock the movie immediately (dev mode: cancel = success)
                if (_userService != null)
                {
                    await _userService.BuyMovieAsync(userId, movieId);
                }
                // Mark voucher as used
                if (!string.IsNullOrWhiteSpace(appliedVoucherCode) && _checkinService != null)
                {
                    await _checkinService.ApplyVoucherAsync(userId, appliedVoucherCode, movieId, movie.Price);
                }
                return Json(new { success = true, payUrl, qrCodeUrl, movieId, movieTitle = movie.Title, amount });
            }
            else
            {
                var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "Lỗi tạo thanh toán MoMo";
                return Json(new { success = false, message = msg });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MoMo] Error: {ex.Message}");
            return Json(new { success = false, message = "Lỗi kết nối đến MoMo: " + ex.Message });
        }
    }

    [HttpGet]
    [Route("api/movies/momo-return")]
    public async Task<IActionResult> MoMoReturn()
    {
        // Extract parameters from query string
        var orderId = Request.Query["orderId"].ToString();
        var resultCode = Request.Query["resultCode"].ToString();
        var extraData = Request.Query["extraData"].ToString();

        Console.WriteLine($"[MoMo Return] orderId={orderId}, resultCode={resultCode}, extraData={extraData}");

        // Parse extraData to get userId and movieId
        string userId = string.Empty;
        string movieId = string.Empty;

        try
        {
            if (!string.IsNullOrEmpty(extraData))
            {
                var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(extraData));
                using var doc = System.Text.Json.JsonDocument.Parse(decoded);
                userId = doc.RootElement.TryGetProperty("userId", out var uid) ? uid.GetString() ?? "" : "";
                movieId = doc.RootElement.TryGetProperty("movieId", out var mid) ? mid.GetString() ?? "" : "";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MoMo Return] Error parsing extraData: {ex.Message}");
        }

        // Unlock movie regardless of resultCode (as user requested: cancel = success)
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(movieId) && _userService != null)
        {
            var result = await _userService.BuyMovieAsync(userId, movieId);
            Console.WriteLine($"[MoMo Return] BuyMovie result: {result.Success} - {result.Message}");
        }

        // Redirect back to movie detail page with success flag
        return Redirect($"/phim/{movieId}?payment=success");
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Route("api/movies/momo-ipn")]
    public IActionResult MoMoIpn()
    {
        // IPN endpoint for MoMo server-to-server notification
        // In dev mode, we just acknowledge
        Console.WriteLine("[MoMo IPN] Received IPN callback");
        return NoContent();
    }
}

public class BuyMovieRequest
{
    public string MovieId { get; set; } = string.Empty;
    public string? VoucherCode { get; set; }
}

public class NotifyMovieRequest
{
    public string MovieId { get; set; } = string.Empty;
    public string MovieTitle { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class NotifyEntry
{
    public string MovieId { get; set; } = string.Empty;
    public string MovieTitle { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
