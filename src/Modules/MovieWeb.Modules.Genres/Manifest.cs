using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Module Thể Loại",
    Author = "MovieOnline Team",
    Website = "https://movieonline.com",
    Version = "1.0.0",
    Description = "Module Thể Loại",
    Category = "Movie Portal"
)]

[assembly: Feature(
    Id = "MovieWeb.Modules.Genres",
    Name = "Module Thể Loại",
    Description = "Module Thể Loại",
    Category = "Movie Portal",
    IsAlwaysEnabled = true
)]
