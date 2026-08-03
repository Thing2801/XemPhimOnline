using MovieWeb.Core.Models;

namespace MovieWeb.Core.Services;

public interface ICommentService
{
    Task<List<MovieComment>> GetCommentsByMovieIdAsync(string movieId);
    Task<MovieComment> AddCommentAsync(string movieId, string userId, string userName, string userAvatar, int rating, string content);
    Task<int> LikeCommentAsync(string commentId, string userId);
    Task<int> GetCommentsCountAsync(string movieId);
    Task<double> GetAverageRatingAsync(string movieId, double defaultRating);
    Task<Dictionary<int, int>> GetRatingBreakdownAsync(string movieId);
}
