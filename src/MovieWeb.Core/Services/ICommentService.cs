using MovieWeb.Core.Models;

namespace MovieWeb.Core.Services;

public interface ICommentService
{
    Task<List<MovieComment>> GetCommentsByMovieIdAsync(string movieId);
    Task<MovieComment> AddCommentAsync(string movieId, string userId, string userName, string userAvatar, int rating, string content, string? parentId = null, string? replyToUserId = null, string? replyToUserName = null);
    Task<int> LikeCommentAsync(string commentId, string userId);
    Task<int> GetCommentsCountAsync(string movieId);
    Task<double> GetAverageRatingAsync(string movieId, double defaultRating = 0.0);
    Task<Dictionary<int, int>> GetRatingBreakdownAsync(string movieId);
    Task<(bool Success, string Message)> DeleteCommentAsync(string commentId, string userId);
    Task<(bool Success, string Message, MovieComment? Comment)> UpdateCommentAsync(string commentId, string userId, string newContent, int newRating);
}
