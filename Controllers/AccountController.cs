using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieWeb.Core.Models;
using MovieWeb.Core.Services;
using System.Security.Claims;

namespace MovieWeb.Web.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly ICommentService _commentService;
    private readonly IBookmarkService _bookmarkService;
    private readonly IMovieService _movieService;
    private readonly IConfiguration _configuration;

    public AccountController(
        IUserService userService,
        ICommentService commentService,
        IBookmarkService bookmarkService,
        IMovieService movieService,
        IConfiguration configuration)
    {
        _userService = userService;
        _commentService = commentService;
        _bookmarkService = bookmarkService;
        _movieService = movieService;
        _configuration = configuration;
    }

    [HttpGet]
    [Route("Account/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [Route("Account/Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _userService.ValidateUserAsync(model.UsernameOrEmail, model.Password);
        if (!result.Success || result.User == null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        await SignInUserAsync(result.User, model.RememberMe);
        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpGet]
    [Route("Account/Register")]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [Route("Account/Register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _userService.RegisterAsync(model);
        if (!result.Success || result.User == null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        await SignInUserAsync(result.User, isPersistent: true);
        return RedirectToLocal(returnUrl);
    }

    [HttpGet]
    [Route("Account/GoogleLogin")]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(GoogleCallback), "Account", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    [Route("Account/GoogleCallback")]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
    {
        var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var googleResult = await HttpContext.AuthenticateAsync(Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme);

        var principal = (authResult.Succeeded && authResult.Principal != null)
            ? authResult.Principal
            : ((googleResult.Succeeded && googleResult.Principal != null) ? googleResult.Principal : HttpContext.User);

        var email = principal?.FindFirstValue(ClaimTypes.Email)
                 ?? principal?.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
                 ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        var name = principal?.FindFirstValue(ClaimTypes.Name)
                ?? principal?.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
                ?? email;

        var avatar = principal?.FindFirstValue("picture")
                  ?? principal?.FindFirstValue("urn:google:picture")
                  ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={Uri.EscapeDataString(email ?? "google")}";

        if (!string.IsNullOrEmpty(email))
        {
            var existingUser = await _userService.GetUserByUsernameAsync(email);
            if (existingUser == null)
            {
                var randomPass = Guid.NewGuid().ToString("N") + "!1Aa";
                var regModel = new RegisterViewModel
                {
                    Username = email,
                    Email = email,
                    FullName = string.IsNullOrWhiteSpace(name) ? email : name,
                    Password = randomPass,
                    ConfirmPassword = randomPass
                };
                var regResult = await _userService.RegisterAsync(regModel);
                existingUser = regResult.User;
            }

            if (existingUser != null)
            {
                await _userService.MarkGoogleAccountAsync(existingUser.Id);
                // Only assign Google profile photo if user has no custom avatar set
                if ((string.IsNullOrEmpty(existingUser.AvatarUrl) || existingUser.AvatarUrl == "/images/default-avatar.png")
                    && !string.IsNullOrEmpty(avatar) && avatar.StartsWith("http"))
                {
                    await _userService.UpdateProfileAsync(existingUser.Id, existingUser.FullName, existingUser.Email, avatar);
                }
                await SignInUserAsync(existingUser, isPersistent: true);
            }
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpGet]
    [HttpPost]
    [Route("Account/Logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync();
        Response.Cookies.Delete("MovieWeb.AuthCookie");
        return Redirect("~/");
    }

    [HttpGet]
    [Authorize]
    [Route("Account/Profile")]
    public async Task<IActionResult> Profile()
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            user = new User
            {
                Id = userId,
                FullName = User.FindFirstValue(ClaimTypes.Name) ?? "Thành viên",
                Username = User.FindFirstValue(ClaimTypes.GivenName) ?? User.Identity?.Name ?? "",
                Email = User.FindFirstValue(ClaimTypes.Email) ?? "",
                Role = User.FindFirstValue(ClaimTypes.Role) ?? "User",
                AvatarUrl = User.FindFirstValue("AvatarUrl") ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={Uri.EscapeDataString(User.Identity?.Name ?? "User")}"
            };
        }

        ViewData["Title"] = "Trang Cá Nhân - " + user.FullName;
        var savedMovieIds = await _bookmarkService.GetBookmarkedMovieIdsAsync(userId);
        ViewBag.SavedMoviesCount = savedMovieIds.Count;

        var purchasedMovies = new List<Movie>();
        if (user.PurchasedMovieIds != null && user.PurchasedMovieIds.Count > 0)
        {
            foreach (var mId in user.PurchasedMovieIds)
            {
                var m = await _movieService.GetMovieByIdOrSlugAsync(mId);
                if (m != null)
                {
                    purchasedMovies.Add(m);
                }
            }
        }
        ViewBag.PurchasedMovies = purchasedMovies;
        ViewBag.PurchasedMoviesCount = purchasedMovies.Count;

        return View(user);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Route("api/comments/add")]
    public async Task<IActionResult> AddComment([FromBody] CommentRequest? req)
    {
        Console.WriteLine($"[AddComment] Received request: IsAuth={User.Identity?.IsAuthenticated}, MovieId='{req?.MovieId}', Content='{req?.Content}', Rating={req?.Rating}");

        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            Console.WriteLine("[AddComment] Rejected: User is not authenticated.");
            return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để bình luận." });
        }

        string movieId = req?.MovieId ?? string.Empty;
        string content = req?.Content ?? string.Empty;
        int rating = req?.Rating ?? 5;
        string? parentId = req?.ParentId;
        string? replyToUserId = req?.ReplyToUserId;
        string? replyToUserName = req?.ReplyToUserName;

        if (string.IsNullOrWhiteSpace(movieId) || string.IsNullOrWhiteSpace(content))
        {
            Console.WriteLine($"[AddComment] Rejected 400: movieId='{movieId}', content='{content}'");
            return BadRequest(new { success = false, message = "Nội dung hoặc ID phim không hợp lệ." });
        }

        try
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            string userName = User.FindFirstValue(ClaimTypes.Name) ?? "Thành viên";
            string avatarUrl = User.FindFirstValue("AvatarUrl") ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={Uri.EscapeDataString(userName)}";

            var comment = await _commentService.AddCommentAsync(movieId, userId, userName, avatarUrl, rating, content, parentId, replyToUserId, replyToUserName);
            int totalCount = await _commentService.GetCommentsCountAsync(movieId);

            Console.WriteLine($"[AddComment] Success: Added comment {comment.Id} for movie {movieId} (ParentId={parentId})");
            return Json(new { success = true, comment = comment, totalCount = totalCount });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AddComment] Error Exception: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Route("api/comments/like")]
    public async Task<IActionResult> LikeComment([FromBody] LikeRequest? req)
    {
        Console.WriteLine($"[LikeComment] Received request: CommentId='{req?.CommentId}'");
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });
        }

        string commentId = req?.CommentId ?? string.Empty;

        if (string.IsNullOrWhiteSpace(commentId))
        {
            return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
        }

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        int likes = await _commentService.LikeCommentAsync(commentId, userId);

        return Json(new { success = true, likesCount = likes });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Route("api/comments/delete")]
    public async Task<IActionResult> DeleteComment([FromBody] DeleteCommentRequest? req)
    {
        Console.WriteLine($"[DeleteComment] Received request: CommentId='{req?.CommentId}'");
        if (User.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

        string commentId = req?.CommentId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(commentId))
            return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _commentService.DeleteCommentAsync(commentId, userId);

        if (!result.Success)
            return Json(new { success = false, message = result.Message });

        return Json(new { success = true, message = result.Message });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Route("api/comments/edit")]
    public async Task<IActionResult> EditComment([FromBody] EditCommentRequest? req)
    {
        Console.WriteLine($"[EditComment] Received request: CommentId='{req?.CommentId}', Content='{req?.Content}'");
        if (User.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

        string commentId = req?.CommentId ?? string.Empty;
        string content = req?.Content ?? string.Empty;
        int rating = req?.Rating ?? 5;

        if (string.IsNullOrWhiteSpace(commentId) || string.IsNullOrWhiteSpace(content))
            return BadRequest(new { success = false, message = "Nội dung hoặc ID bình luận không hợp lệ." });

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _commentService.UpdateCommentAsync(commentId, userId, content, rating);

        if (!result.Success)
            return Json(new { success = false, message = result.Message });

        return Json(new { success = true, message = result.Message, comment = result.Comment });
    }

    [HttpGet]
    [HttpPost]
    [Route("api/user/me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        // Manually authenticate from the cookie to bypass any OrchardCore middleware interference
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (result.Succeeded && result.Principal != null)
        {
            string userId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            string userName = result.Principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            string avatarUrl = result.Principal.FindFirstValue("AvatarUrl") ?? string.Empty;
            return Json(new { userId, userName, avatarUrl, isAuthenticated = true });
        }

        return Json(new { userId = "", isAuthenticated = false });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Route("api/user/update-profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest? request)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để thực hiện." });
        }

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (string.IsNullOrEmpty(userId) || request == null)
        {
            return BadRequest(new { success = false, message = "Dữ liệu cập nhật không hợp lệ." });
        }

        string fullName = (request.FullName ?? "").Trim();
        string email = (request.Email ?? "").Trim();
        string avatarUrl = (request.AvatarUrl ?? "").Trim();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return BadRequest(new { success = false, message = "Họ và tên không được để trống." });
        }

        if (avatarUrl.StartsWith("data:image"))
        {
            avatarUrl = SaveBase64Avatar(avatarUrl, userId);
        }

        var updateResult = await _userService.UpdateProfileAsync(userId, fullName, email, avatarUrl);
        if (!updateResult.Success || updateResult.User == null)
        {
            return Json(new { success = false, message = updateResult.Message });
        }

        var updatedUser = updateResult.User;
        if (string.IsNullOrEmpty(updatedUser.Username))
            updatedUser.Username = User.FindFirstValue(ClaimTypes.GivenName) ?? User.Identity?.Name ?? "user";
        if (string.IsNullOrEmpty(updatedUser.Email))
            updatedUser.Email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        if (string.IsNullOrEmpty(updatedUser.Role))
            updatedUser.Role = User.FindFirstValue(ClaimTypes.Role) ?? "User";

        await SignInUserAsync(updatedUser, isPersistent: true);

        return Json(new { 
            success = true, 
            message = "Cập nhật thông tin cá nhân thành công!",
            user = new {
                fullName = updatedUser.FullName,
                avatarUrl = updatedUser.AvatarUrl
            }
        });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Route("api/user/change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest? request)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để thực hiện." });
        }

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (string.IsNullOrEmpty(userId) || request == null)
        {
            return BadRequest(new { success = false, message = "Dữ liệu đổi mật khẩu không hợp lệ." });
        }

        string currentPass = (request.CurrentPassword ?? "").Trim();
        string newPass = (request.NewPassword ?? "").Trim();
        string confirmPass = (request.ConfirmNewPassword ?? "").Trim();

        if (string.IsNullOrWhiteSpace(currentPass))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập mật khẩu hiện tại." });
        }

        if (string.IsNullOrWhiteSpace(newPass) || newPass.Length < 6)
        {
            return BadRequest(new { success = false, message = "Mật khẩu mới phải từ 6 ký tự trở lên." });
        }

        if (newPass != confirmPass)
        {
            return BadRequest(new { success = false, message = "Mật khẩu mới và Xác nhận mật khẩu không trùng khớp." });
        }

        var result = await _userService.ChangePasswordAsync(userId, currentPass, newPass);
        if (!result.Success)
        {
            return Json(new { success = false, message = result.Message });
        }

        return Json(new { success = true, message = result.Message });
    }

    private string SaveBase64Avatar(string base64Data, string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(base64Data)) return "/images/default-avatar.png";
            if (!base64Data.StartsWith("data:image")) return base64Data;

            int commaIdx = base64Data.IndexOf(',');
            if (commaIdx < 0) return base64Data;

            string base64Sub = base64Data.Substring(commaIdx + 1);
            byte[] imageBytes = Convert.FromBase64String(base64Sub);

            string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            string extension = ".png";
            if (base64Data.Contains("image/jpeg") || base64Data.Contains("image/jpg")) extension = ".jpg";
            else if (base64Data.Contains("image/gif")) extension = ".gif";
            else if (base64Data.Contains("image/webp")) extension = ".webp";

            string fileName = $"avatar_{userId}{extension}";
            string filePath = Path.Combine(uploadsDir, fileName);

            System.IO.File.WriteAllBytes(filePath, imageBytes);

            return $"/uploads/avatars/{fileName}?v={DateTime.UtcNow.Ticks}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SaveBase64Avatar Error] {ex.Message}");
            return "/images/default-avatar.png";
        }
    }

    private async Task SignInUserAsync(User user, bool isPersistent)
    {
        string safeAvatarUrl = user.AvatarUrl ?? "";
        if (safeAvatarUrl.StartsWith("data:image"))
        {
            safeAvatarUrl = SaveBase64Avatar(safeAvatarUrl, user.Id);
        }
        if (safeAvatarUrl.Length > 300)
        {
            safeAvatarUrl = "/images/default-avatar.png";
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.GivenName, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("AvatarUrl", safeAvatarUrl)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
    }

    [HttpGet]
    [Route("phim-da-luu")]
    [Authorize]
    public async Task<IActionResult> SavedMovies()
    {
        ViewData["Title"] = "Phim Đã Lưu";
        ViewData["ActivePage"] = "SavedMovies";

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var savedMovies = await _bookmarkService.GetBookmarkedMoviesAsync(userId, _movieService);

        return View(savedMovies);
    }

    [HttpGet]
    [Route("phim-da-mua")]
    [Authorize]
    public async Task<IActionResult> PurchasedMovies()
    {
        ViewData["Title"] = "Phim Đã Mua";
        ViewData["ActivePage"] = "PurchasedMovies";

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var user = await _userService.GetUserByIdAsync(userId);

        var purchasedMovies = new List<Movie>();
        if (user != null && user.PurchasedMovieIds != null && user.PurchasedMovieIds.Count > 0)
        {
            foreach (var mId in user.PurchasedMovieIds)
            {
                var m = await _movieService.GetMovieByIdOrSlugAsync(mId);
                if (m != null)
                {
                    purchasedMovies.Add(m);
                }
            }
        }

        return View(purchasedMovies);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Route("api/bookmarks/toggle")]
    public async Task<IActionResult> ToggleBookmark([FromBody] ToggleBookmarkRequest? request)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập để sử dụng tính năng lưu phim." });
        }

        string movieId = request?.MovieId ?? string.Empty;
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(movieId))
        {
            return Json(new { success = false, message = "Dữ liệu phim không hợp lệ." });
        }

        bool isSaved = await _bookmarkService.ToggleBookmarkAsync(userId, movieId);
        return Json(new { success = true, isBookmarked = isSaved, message = isSaved ? "Đã lưu phim vào danh sách yêu thích!" : "Đã xóa phim khỏi danh sách đã lưu." });
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return Redirect("~/");
    }
}

public class ToggleBookmarkRequest
{
    public string MovieId { get; set; } = string.Empty;
}

public class CommentRequest
{
    public string MovieId { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public string Content { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string? ReplyToUserId { get; set; }
    public string? ReplyToUserName { get; set; }
}

public class LikeRequest
{
    public string CommentId { get; set; } = string.Empty;
}

public class DeleteCommentRequest
{
    public string CommentId { get; set; } = string.Empty;
}

public class EditCommentRequest
{
    public string CommentId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
}

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
