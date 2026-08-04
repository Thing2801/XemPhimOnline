using MovieWeb.Core.Models;
using System.Text.Json;

namespace MovieWeb.Core.Services;

public class CommentService : ICommentService
{
    private static readonly object _fileLock = new();
    private readonly string _filePath;
    private List<MovieComment> _comments = new();

    public CommentService(string dataDirPath)
    {
        Directory.CreateDirectory(dataDirPath);
        _filePath = Path.Combine(dataDirPath, "comments.json");
        LoadComments();
    }

    private void LoadComments()
    {
        lock (_fileLock)
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath);
                    _comments = JsonSerializer.Deserialize<List<MovieComment>>(json) ?? new List<MovieComment>();
                }
                catch
                {
                    _comments = new List<MovieComment>();
                }
            }
            else
            {
                _comments = new List<MovieComment>();
                SaveCommentsInternal();
            }
        }
    }

    private void SaveCommentsInternal()
    {
        string json = JsonSerializer.Serialize(_comments, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    public Task<List<MovieComment>> GetCommentsByMovieIdAsync(string movieId)
    {
        lock (_fileLock)
        {
            var list = _comments
                .Where(c => c.MovieId == movieId)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();
            return Task.FromResult(list);
        }
    }

    public Task<int> GetCommentsCountAsync(string movieId)
    {
        lock (_fileLock)
        {
            return Task.FromResult(_comments.Count(c => c.MovieId == movieId));
        }
    }

    public Task<double> GetAverageRatingAsync(string movieId, double defaultRating = 0.0)
    {
        lock (_fileLock)
        {
            var movieRatings = _comments.Where(c => c.MovieId == movieId && string.IsNullOrEmpty(c.ParentId) && c.Rating > 0).ToList();
            if (!movieRatings.Any())
            {
                return Task.FromResult(0.0);
            }
            double avg = movieRatings.Average(c => c.Rating);
            return Task.FromResult(Math.Round(avg, 1));
        }
    }

    public Task<Dictionary<int, int>> GetRatingBreakdownAsync(string movieId)
    {
        lock (_fileLock)
        {
            var movieRatings = _comments.Where(c => c.MovieId == movieId && string.IsNullOrEmpty(c.ParentId) && c.Rating > 0).ToList();
            int total = movieRatings.Count;
            var result = new Dictionary<int, int> { { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 } };

            if (total == 0) return Task.FromResult(result);

            for (int r = 1; r <= 5; r++)
            {
                int count = movieRatings.Count(c => c.Rating == r);
                result[r] = (int)Math.Round((double)count / total * 100);
            }

            return Task.FromResult(result);
        }
    }

    public Task<MovieComment> AddCommentAsync(string movieId, string userId, string userName, string userAvatar, int rating, string content, string? parentId = null, string? replyToUserId = null, string? replyToUserName = null)
    {
        lock (_fileLock)
        {
            // If it's a child reply, rating is 0 (does not count as a movie star rating vote)
            int finalRating = !string.IsNullOrEmpty(parentId) ? 0 : Math.Clamp(rating, 1, 5);

            var comment = new MovieComment
            {
                Id = Guid.NewGuid().ToString("N"),
                MovieId = movieId,
                UserId = userId,
                UserName = userName,
                UserAvatar = string.IsNullOrWhiteSpace(userAvatar) ? $"https://api.dicebear.com/7.x/avataaars/svg?seed={Uri.EscapeDataString(userName)}" : userAvatar,
                Rating = finalRating,
                Content = content.Trim(),
                LikesCount = 0,
                CreatedAt = DateTime.UtcNow,
                ParentId = parentId,
                ReplyToUserId = replyToUserId,
                ReplyToUserName = replyToUserName
            };

            _comments.Add(comment);
            SaveCommentsInternal();

            return Task.FromResult(comment);
        }
    }

    public Task<int> LikeCommentAsync(string commentId, string userId)
    {
        lock (_fileLock)
        {
            var comment = _comments.FirstOrDefault(c => c.Id == commentId);
            if (comment != null)
            {
                if (!comment.LikedUserIds.Contains(userId))
                {
                    comment.LikedUserIds.Add(userId);
                    comment.LikesCount = comment.LikedUserIds.Count;
                    SaveCommentsInternal();
                }
                return Task.FromResult(comment.LikesCount);
            }
            return Task.FromResult(0);
        }
    }

    public Task<(bool Success, string Message)> DeleteCommentAsync(string commentId, string userId)
    {
        lock (_fileLock)
        {
            var comment = _comments.FirstOrDefault(c => c.Id == commentId);
            if (comment == null)
                return Task.FromResult((false, "Bình luận không tồn tại."));
            if (comment.UserId != userId)
                return Task.FromResult((false, "Bạn không có quyền xóa bình luận này."));

            _comments.RemoveAll(c => c.Id == commentId || c.ParentId == commentId);
            SaveCommentsInternal();
            return Task.FromResult((true, "Đã xóa bình luận."));
        }
    }

    public Task<(bool Success, string Message, MovieComment? Comment)> UpdateCommentAsync(string commentId, string userId, string newContent, int newRating)
    {
        lock (_fileLock)
        {
            var comment = _comments.FirstOrDefault(c => c.Id == commentId);
            if (comment == null)
                return Task.FromResult((false, "Bình luận không tồn tại.", (MovieComment?)null));
            if (comment.UserId != userId)
                return Task.FromResult((false, "Bạn không có quyền chỉnh sửa bình luận này.", (MovieComment?)null));

            comment.Content = newContent.Trim();
            comment.Rating = Math.Clamp(newRating, 1, 5);
            SaveCommentsInternal();
            return Task.FromResult((true, "Đã cập nhật bình luận.", (MovieComment?)comment));
        }
    }
}
