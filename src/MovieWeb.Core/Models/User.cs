namespace MovieWeb.Core.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = "/images/default-avatar.png";
    public string Role { get; set; } = "User";
    public bool IsGoogleAccount { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<string> PurchasedMovieIds { get; set; } = new();

    public bool IsGoogleUser => IsGoogleAccount || string.IsNullOrWhiteSpace(PasswordHash) || PasswordHash.StartsWith("GOOGLE_");
}
