using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Module Phim",
    Author = "MovieOnline Team",
    Website = "https://movieonline.com",
    Version = "1.0.0",
    Description = "Module Phim",
    Category = "Movie Portal"
)]

[assembly: Feature(
    Id = "MovieWeb.Modules.Movies",
    Name = "Module Phim",
    Description = "Module Phim",
    Category = "Movie Portal",
    IsAlwaysEnabled = true
)]
