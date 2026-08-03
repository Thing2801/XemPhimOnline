using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Module Phim Chiếu Rạp",
    Author = "MovieOnline Team",
    Website = "https://movieonline.com",
    Version = "1.0.0",
    Description = "Module hiển thị danh sách Phim Chiếu Rạp mới nhất, phim đang chiếu và sắp chiếu tại rạp.",
    Category = "Movie Portal"
)]

[assembly: Feature(
    Id = "MovieWeb.Modules.Cinema",
    Name = "Module Phim Chiếu Rạp",
    Description = "Module Phim Chiếu Rạp",
    Category = "Movie Portal",
    IsAlwaysEnabled = true
)]
