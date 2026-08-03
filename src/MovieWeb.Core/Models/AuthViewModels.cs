using System.ComponentModel.DataAnnotations;

namespace MovieWeb.Core.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên tài khoản hoặc Email")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = true;
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập Họ và tên")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Họ và tên từ 2 đến 50 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên tài khoản")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Tên tài khoản từ 3 đến 30 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên tài khoản chỉ chứa chữ cái, số và dấu gạch dưới")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Email")]
    [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không trùng khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
