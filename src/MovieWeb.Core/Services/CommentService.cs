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

    public Task<double> GetAverageRatingAsync(string movieId, double defaultRating)
    {
        lock (_fileLock)
        {
            var movieComments = _comments.Where(c => c.MovieId == movieId && c.Rating > 0).ToList();
            if (!movieComments.Any())
            {
                return Task.FromResult(defaultRating);
            }
            double avg = movieComments.Average(c => c.Rating) * 2.0; // scale 5-star to 10-scale
            return Task.FromResult(Math.Round(avg, 1));
        }
    }

    public Task<Dictionary<int, int>> GetRatingBreakdownAsync(string movieId)
    {
        lock (_fileLock)
        {
            var movieComments = _comments.Where(c => c.MovieId == movieId).ToList();
            int total = movieComments.Count;
            var result = new Dictionary<int, int> { { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 } };

            if (total == 0) return Task.FromResult(result);

            for (int r = 1; r <= 5; r++)
            {
                int count = movieComments.Count(c => c.Rating == r);
                result[r] = (int)Math.Round((double)count / total * 100);
            }

            return Task.FromResult(result);
        }
    }

    public Task<MovieComment> AddCommentAsync(string movieId, string userId, string userName, string userAvatar, int rating, string content)
    {
        lock (_fileLock)
        {
            var comment = new MovieComment
            {
                Id = Guid.NewGuid().ToString("N"),
                MovieId = movieId,
                UserId = userId,
                UserName = userName,
                UserAvatar = string.IsNullOrWhiteSpace(userAvatar) ? $"https://api.dicebear.com/7.x/avataaars/svg?seed={Uri.EscapeDataString(userName)}" : userAvatar,
                Rating = Math.Clamp(rating, 1, 5),
                Content = content.Trim(),
                LikesCount = 0,
                CreatedAt = DateTime.UtcNow
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
}
