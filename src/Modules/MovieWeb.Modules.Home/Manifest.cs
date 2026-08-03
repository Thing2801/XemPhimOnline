using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Module Trang Chủ",
    Author = "MovieOnline Team",
    Website = "https://movieonline.com",
    Version = "1.0.0",
    Description = "Module Trang chủ",
    Category = "Movie Portal"
)]

[assembly: Feature(
    Id = "MovieWeb.Modules.Home",
    Name = "Module Trang Chủ",
    Description = "Module Trang Chủ",
    Category = "Movie Portal",
    IsAlwaysEnabled = true
)]
