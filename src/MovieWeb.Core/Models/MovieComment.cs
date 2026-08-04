namespace MovieWeb.Core.Models;

public class MovieComment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MovieId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserAvatar { get; set; } = "/images/default-avatar.png";
    public int Rating { get; set; } = 5;
    public string Content { get; set; } = string.Empty;
    public int LikesCount { get; set; } = 0;
    public List<string> LikedUserIds { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? ParentId { get; set; }
    public string? ReplyToUserId { get; set; }
    public string? ReplyToUserName { get; set; }
}
