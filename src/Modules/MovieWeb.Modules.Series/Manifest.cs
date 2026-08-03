using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Module Phim Bộ",
    Author = "MovieOnline Team",
    Website = "https://movieonline.com",
    Version = "1.0.0",
    Description = "Module hiển thị danh sách Phim Bộ (IsPartMovie) mới nhất, cập nhật liên tục.",
    Category = "Movie Portal"
)]

[assembly: Feature(
    Id = "MovieWeb.Modules.Series",
    Name = "Module Phim Bộ",
    Description = "Module Phim Bộ (Phim nhiều tập / IsPartMovie)",
    Category = "Movie Portal",
    IsAlwaysEnabled = true
)]
