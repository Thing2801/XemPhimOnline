using Microsoft.AspNetCore.Identity;
using MovieWeb.Core.Models;
using System.Text.Json;

namespace MovieWeb.Core.Services;

public class UserService : IUserService
{
    private static readonly object _fileLock = new();
    private readonly string _filePath;
    private readonly PasswordHasher<User> _passwordHasher = new();
    private List<User> _users = new();

    public UserService(string dataDirPath)
    {
        Directory.CreateDirectory(dataDirPath);
        _filePath = Path.Combine(dataDirPath, "users.json");
        LoadUsers();
    }

    private void LoadUsers()
    {
        lock (_fileLock)
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath);
                    _users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
                }
                catch
                {
                    _users = new List<User>();
                }
            }
            else
            {
                _users = new List<User>();
                SaveUsersInternal();
            }
        }
    }

    private void SaveUsersInternal()
    {
        string json = JsonSerializer.Serialize(_users, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    public Task<User?> GetUserByIdAsync(string id)
    {
        lock (_fileLock)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
        }
    }

    public Task<User?> GetUserByUsernameAsync(string username)
    {
        lock (_fileLock)
        {
            return Task.FromResult(_users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<(bool Success, string Message, User? User)> RegisterAsync(RegisterViewModel model)
    {
        lock (_fileLock)
        {
            if (_users.Any(u => string.Equals(u.Username, model.Username, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult<(bool, string, User?)>((false, "Tên tài khoản này đã được sử dụng", null));
            }

            if (_users.Any(u => string.Equals(u.Email, model.Email, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult<(bool, string, User?)>((false, "Địa chỉ Email này đã được đăng ký", null));
            }

            var user = new User
            {
                Id = Guid.NewGuid().ToString("N"),
                Username = model.Username.Trim(),
                Email = model.Email.Trim().ToLowerInvariant(),
                FullName = model.FullName.Trim(),
                AvatarUrl = $"https://api.dicebear.com/7.x/avataaars/svg?seed={Uri.EscapeDataString(model.Username)}",
                CreatedAt = DateTime.UtcNow
            };

            // Hash password securely with PBKDF2
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            _users.Add(user);
            SaveUsersInternal();

            return Task.FromResult<(bool, string, User?)>((true, "Đăng ký tài khoản thành công!", user));
        }
    }

    public Task<(bool Success, string Message, User? User)> ValidateUserAsync(string usernameOrEmail, string password)
    {
        lock (_fileLock)
        {
            string query = usernameOrEmail.Trim().ToLowerInvariant();
            var user = _users.FirstOrDefault(u => 
                u.Username.Equals(query, StringComparison.OrdinalIgnoreCase) || 
                u.Email.Equals(query, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                return Task.FromResult<(bool, string, User?)>((false, "Tên tài khoản hoặc Email không tồn tại", null));
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                return Task.FromResult<(bool, string, User?)>((false, "Mật khẩu không chính xác", null));
            }

            return Task.FromResult<(bool, string, User?)>((true, "Đăng nhập thành công!", user));
        }
    }

    public Task<(bool Success, string Message, User? User)> UpdateProfileAsync(string userId, string fullName, string email, string avatarUrl)
    {
        lock (_fileLock)
        {
            var cleanEmail = (email ?? "").Trim();
            var user = _users.FirstOrDefault(u => u.Id == userId
                || (!string.IsNullOrEmpty(cleanEmail) && string.Equals(u.Email, cleanEmail, StringComparison.OrdinalIgnoreCase))
                || string.Equals(u.Username, userId, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                user = new User
                {
                    Id = userId,
                    FullName = string.IsNullOrWhiteSpace(fullName) ? "Thành viên" : fullName.Trim(),
                    Email = string.IsNullOrWhiteSpace(email) ? "" : email.Trim(),
                    AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? $"https://api.dicebear.com/7.x/avataaars/svg?seed={userId}" : avatarUrl.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                _users.Add(user);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    user.FullName = fullName.Trim();
                }
                if (!string.IsNullOrWhiteSpace(email))
                {
                    user.Email = email.Trim();
                }
                if (!string.IsNullOrWhiteSpace(avatarUrl))
                {
                    user.AvatarUrl = avatarUrl.Trim();
                }
            }

            SaveUsersInternal();
            return Task.FromResult<(bool, string, User?)>((true, "Cập nhật thông tin cá nhân thành công!", user));
        }
    }

    public Task<(bool Success, string Message)> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        lock (_fileLock)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return Task.FromResult((false, "Tài khoản không tồn tại."));
            }

            if (user.IsGoogleAccount)
            {
                return Task.FromResult((false, "Tài khoản đăng nhập qua Google không thể sử dụng chức năng đổi mật khẩu."));
            }

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                return Task.FromResult((false, "Vui lòng nhập mật khẩu hiện tại."));
            }

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                return Task.FromResult((false, "Mật khẩu hiện tại không chính xác."));
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                return Task.FromResult((false, "Mật khẩu mới phải có độ dài từ 6 ký tự trở lên."));
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            SaveUsersInternal();
            return Task.FromResult((true, "Đổi mật khẩu thành công! Mật khẩu mới của bạn đã được cập nhật."));
        }
    }

    public Task MarkGoogleAccountAsync(string userId)
    {
        lock (_fileLock)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.IsGoogleAccount = true;
                SaveUsersInternal();
            }
            return Task.CompletedTask;
        }
    }

    public Task<bool> HasPurchasedMovieAsync(string userId, string movieId)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(movieId))
            return Task.FromResult(false);

        lock (_fileLock)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null || user.PurchasedMovieIds == null)
                return Task.FromResult(false);

            bool purchased = user.PurchasedMovieIds.Contains(movieId, StringComparer.OrdinalIgnoreCase);
            return Task.FromResult(purchased);
        }
    }

    public Task<(bool Success, string Message)> BuyMovieAsync(string userId, string movieId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult((false, "Vui lòng đăng nhập để thực hiện giao dịch."));

        if (string.IsNullOrWhiteSpace(movieId))
            return Task.FromResult((false, "Thông tin phim không hợp lệ."));

        lock (_fileLock)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return Task.FromResult((false, "Tài khoản không tồn tại."));

            if (user.PurchasedMovieIds == null)
                user.PurchasedMovieIds = new List<string>();

            if (user.PurchasedMovieIds.Contains(movieId, StringComparer.OrdinalIgnoreCase))
                return Task.FromResult((true, "Bạn đã sở hữu phim này rồi!"));

            user.PurchasedMovieIds.Add(movieId);
            SaveUsersInternal();
            return Task.FromResult((true, "Thanh toán vé phim thành công! Bạn có thể xem phim ngay bây giờ."));
        }
    }
}
