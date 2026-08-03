namespace MovieWeb.Core.Models;

public class Genre
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fas fa-film";
    public string BadgeColor { get; set; } = "bg-primary";
    public int MovieCount { get; set; }
}
