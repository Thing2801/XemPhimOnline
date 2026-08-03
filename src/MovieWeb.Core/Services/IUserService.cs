using MovieWeb.Core.Models;

namespace MovieWeb.Core.Services;

public interface IUserService
{
    Task<(bool Success, string Message, User? User)> RegisterAsync(RegisterViewModel model);
    Task<(bool Success, string Message, User? User)> ValidateUserAsync(string usernameOrEmail, string password);
    Task<User?> GetUserByIdAsync(string id);
    Task<User?> GetUserByUsernameAsync(string username);
}
