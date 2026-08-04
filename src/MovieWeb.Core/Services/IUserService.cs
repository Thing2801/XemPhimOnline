using MovieWeb.Core.Models;

namespace MovieWeb.Core.Services;

public interface IUserService
{
    Task<(bool Success, string Message, User? User)> RegisterAsync(RegisterViewModel model);
    Task<(bool Success, string Message, User? User)> ValidateUserAsync(string usernameOrEmail, string password);
    Task<User?> GetUserByIdAsync(string id);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<(bool Success, string Message, User? User)> UpdateProfileAsync(string userId, string fullName, string email, string avatarUrl);
    Task<(bool Success, string Message)> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task MarkGoogleAccountAsync(string userId);
    Task<bool> HasPurchasedMovieAsync(string userId, string movieId);
    Task<(bool Success, string Message)> BuyMovieAsync(string userId, string movieId);
}
