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

    public AccountController(IUserService userService, ICommentService commentService)
    {
        _userService = userService;
        _commentService = commentService;
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

    [HttpPost]
    [Route("Account/Logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Route("api/comments/add")]
    public async Task<IActionResult> AddComment([FromBody] CommentRequest req)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để bình luận." });
        }

        if (req == null || string.IsNullOrWhiteSpace(req.MovieId) || string.IsNullOrWhiteSpace(req.Content))
        {
            return BadRequest(new { success = false, message = "Nội dung bình luận không hợp lệ." });
        }

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        string userName = User.FindFirstValue(ClaimTypes.Name) ?? "Thành viên";
        string avatarUrl = User.FindFirstValue("AvatarUrl") ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={Uri.EscapeDataString(userName)}";

        var comment = await _commentService.AddCommentAsync(req.MovieId, userId, userName, avatarUrl, req.Rating, req.Content);
        int totalCount = await _commentService.GetCommentsCountAsync(req.MovieId);

        return Json(new { success = true, comment = comment, totalCount = totalCount });
    }

    [HttpPost]
    [Route("api/comments/like")]
    public async Task<IActionResult> LikeComment([FromBody] LikeRequest req)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });
        }

        if (req == null || string.IsNullOrWhiteSpace(req.CommentId))
        {
            return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
        }

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        int likes = await _commentService.LikeCommentAsync(req.CommentId, userId);

        return Json(new { success = true, likesCount = likes });
    }

    private async Task SignInUserAsync(User user, bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.GivenName, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("AvatarUrl", user.AvatarUrl)
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

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }
}

public class CommentRequest
{
    public string MovieId { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public string Content { get; set; } = string.Empty;
}

public class LikeRequest
{
    public string CommentId { get; set; } = string.Empty;
}
