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
}
