using MovieWeb.Core.Models;

namespace MovieWeb.Core.Data;

public static class MovieSeedData
{
    public static List<Genre> GetDefaultGenres()
    {
        return new List<Genre>
        {
            new Genre { Id = "hanh-dong", Name = "Hành động", Slug = "hanh-dong", Description = "Phim hành động kịch tính, nghẹt thở", IconClass = "fas fa-fire", BadgeColor = "badge-red" },
            new Genre { Id = "sieu-anh-hung", Name = "Siêu anh hùng", Slug = "sieu-anh-hung", Description = "Vũ trụ siêu anh hùng giải cứu thế giới", IconClass = "fas fa-mask", BadgeColor = "badge-amber" },
            new Genre { Id = "tinh-cam", Name = "Tình cảm", Slug = "tinh-cam", Description = "Những câu chuyện tình yêu sâu lắng, lãng mạn", IconClass = "fas fa-heart", BadgeColor = "badge-pink" },
            new Genre { Id = "ngon-tinh", Name = "Ngôn tình", Slug = "ngon-tinh", Description = "Phim truyền hình lãng mạn, thanh xuân vườn trường", IconClass = "fas fa-feather-alt", BadgeColor = "badge-purple" },
            new Genre { Id = "kinh-di", Name = "Kinh dị", Slug = "kinh-di", Description = "Phim rùng rợn, giật gân, ám ảnh", IconClass = "fas fa-ghost", BadgeColor = "badge-dark" },
            new Genre { Id = "trinh-tham", Name = "Trinh thám", Slug = "trinh-tham", Description = "Phá án bí ẩn, đấu trí gay go", IconClass = "fas fa-search-location", BadgeColor = "badge-blue" },
            new Genre { Id = "huyen-thoai", Name = "Huyền thoại / Truyền thuyết", Slug = "huyen-thoai", Description = "Thần thoại, thế giới thần tiên, truyền thuyết cổ đại", IconClass = "fas fa-dragon", BadgeColor = "badge-gold" },
            new Genre { Id = "hoat-hinh", Name = "Phim hoạt hình", Slug = "hoat-hinh", Description = "Hoạt hình Anime, 3D sống động, phiêu lưu giải trí", IconClass = "fas fa-magic", BadgeColor = "badge-emerald" },
            new Genre { Id = "khoa-hoc", Name = "Khoa học / Viễn tưởng", Slug = "khoa-hoc", Description = "Khám phá vũ trụ, công nghệ tương lai, du hành thời gian", IconClass = "fas fa-user-astronaut", BadgeColor = "badge-cyan" },
            new Genre { Id = "co-trang", Name = "Cổ Trang", Slug = "co-trang", Description = "Phim lịch sử, kiếm hiệp, triều đình xưa", IconClass = "fas fa-fan", BadgeColor = "badge-orange" }
        };
    }

    public static List<Movie> GetDefaultMovies()
    {
        return new List<Movie>
        {
            // 1. Featured Hero Banner 1 - Siêu anh hùng / Hành động
            new Movie
            {
                Id = "avengers-endgame",
                Title = "Avengers: Hồi Kết",
                OriginalTitle = "Avengers: Endgame",
                Slug = "avengers-endgame",
                Tagline = "Trận chiến cuối cùng để lập lại trật tự vũ trụ",
                Description = "Sau sự kiện quét sạch nửa vũ trụ của Thanos, các Avengers còn sống sót phải tập hợp một lần nữa để đảo ngược thảm họa và phục hồi lại trật tự cho thực tại.",
                PosterUrl = "https://images.unsplash.com/photo-1635805737707-575885ab0820?auto=format&fit=crop&w=600&q=80",
                BannerUrl = "https://images.unsplash.com/photo-1534447677768-be436bb09401?auto=format&fit=crop&w=1600&q=80",
                TrailerUrl = "https://www.youtube.com/embed/TcMBFSGVi1c",
                ReleaseYear = 2024,
                Duration = "181 phút",
                Rating = 4.7,
                AgeRating = "13+",
                Quality = "4K HDR",
                LanguageMode = "Thuyết Minh + Vietsub",
                Country = "Âu Mỹ",
                Director = "Anthony Russo, Joe Russo",
                Cast = new List<string> { "Robert Downey Jr.", "Chris Evans", "Mark Ruffalo", "Chris Hemsworth", "Scarlett Johansson" },
                GenreIds = new List<string> { "hanh-dong", "sieu-anh-hung", "khoa-hoc" },
                GenreNames = new List<string> { "Hành động", "Siêu anh hùng", "Khoa học" },
                IsFeatured = true,
                FeaturedOrder = 1,
                IsSeries = false,
                EpisodeInfo = "Full 4K",
                ViewsCount = 1450000,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },

            // 2. Featured Hero Banner 2 - Huyền thoại / Truyền thuyết / Cổ Trang
            new Movie
            {
                Id = "tay-du-ky-dai-nao-thien-cung",
                Title = "Tây Du Ký: Đại Náo Thiên Cung",
                OriginalTitle = "The Monkey King: Legend Reborn",
                Slug = "tay-du-ky-dai-nao-thien-cung",
                Tagline = "Tề Thiên Đại Thánh đại náo ba giới",
                Description = "Hành trình khởi nguồn của Tôn Ngộ Không từ lúc sinh ra từ đá thần đến khi đại náo Thiên Cung, thách thức thần tiên và trở thành huyền thoại bất tử.",
                PosterUrl = "https://images.unsplash.com/photo-1578632767115-351597cf2477?auto=format&fit=crop&w=600&q=80",
                BannerUrl = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?auto=format&fit=crop&w=1600&q=80",
                TrailerUrl = "https://www.youtube.com/embed/d9MyW72ELq0",
                ReleaseYear = 2025,
                Duration = "128 phút",
                Rating = 4.6,
                AgeRating = "P",
                Quality = "4K Ultra HD",
                LanguageMode = "Lồng Tiếng Chuẩn",
                Country = "Trung Quốc",
                Director = "Trịnh Bảo Thụy",
                Cast = new List<string> { "Chân Tử Đan", "Châu Nhuận Phát", "Quách Phú Thành", "Trần Kiều Ân" },
                GenreIds = new List<string> { "huyen-thoai", "co-trang", "hanh-dong" },
                GenreNames = new List<string> { "Huyền thoại / Truyền thuyết", "Cổ Trang", "Hành động" },
                IsFeatured = true,
                FeaturedOrder = 2,
                IsSeries = false,
                EpisodeInfo = "Bản Đẹp 4K",
                ViewsCount = 980000,
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            },

            // 3. Featured Hero Banner 3 - Ngôn Tình / Tình Cảm
            new Movie
            {
                Id = "vuon-sao-bang-thanh-xuan",
                Title = "Yêu Em Từ Cái Nhìn Đầu Tiên",
                OriginalTitle = "Love O2O - Special Edition",
                Slug = "yeu-em-tu-cai-nhin-dau-tien",
                Tagline = "Mối tình ngọt ngào từ game ra ngoài đời thực",
                Description = "Câu chuyện tình yêu lãng mạn giữa đại thần máy tính Tiêu Nại và hoa khôi khoa CNTT Bối Vi Vi, kết duyên qua thế giới game nhập vai võ lâm.",
                PosterUrl = "https://images.unsplash.com/photo-1516589178581-6cd7833ae3b2?auto=format&fit=crop&w=600&q=80",
                BannerUrl = "https://images.unsplash.com/photo-1518199266791-5375a83190b7?auto=format&fit=crop&w=1600&q=80",
                TrailerUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ",
                ReleaseYear = 2025,
                Duration = "30 Tập",
                Rating = 4.6,
                AgeRating = "P",
                Quality = "Full HD",
                LanguageMode = "Thuyết Minh",
                Country = "Trung Quốc",
                Director = "Lâm Ngọc Phân",
                Cast = new List<string> { "Dương Dương", "Trịnh Sảng", "Trương Bân Bân", "Mao Hiểu Đồng" },
                GenreIds = new List<string> { "ngon-tinh", "tinh-cam" },
                GenreNames = new List<string> { "Ngôn tình", "Tình cảm" },
                IsFeatured = true,
                FeaturedOrder = 3,
                IsSeries = true,
                EpisodeInfo = "Trọn bộ 30/30 Tập",
                ViewsCount = 1200000,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },

            // 4. Featured Hero Banner 4 - Khoa Học / Viễn Tưởng
            new Movie
            {
                Id = "interstellar-du-hanh-khong-thoi-gian",
                Title = "Interstellar: Du Hành Liên Sao",
                OriginalTitle = "Interstellar",
                Slug = "interstellar-du-hanh-lien-sao",
                Tagline = "Tương lai của nhân loại nằm ở ngoài vũ trụ bao la",
                Description = "Khi trái đất bước vào thời kỳ diệt vong, một nhóm nhà phi hành vũ trụ dấn thân vào chuyến du hành qua hố giun thiên hà để tìm kiếm hành tinh mới sống được cho con người.",
                PosterUrl = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?auto=format&fit=crop&w=600&q=80",
                BannerUrl = "https://images.unsplash.com/photo-1446776811953-b23d57bd21aa?auto=format&fit=crop&w=1600&q=80",
                TrailerUrl = "https://www.youtube.com/embed/zSWdZVtXT7E",
                ReleaseYear = 2024,
                Duration = "169 phút",
                Rating = 4.8,
                AgeRating = "13+",
                Quality = "IMAX 4K",
                LanguageMode = "Vietsub Thuyết Minh",
                Country = "Âu Mỹ",
                Director = "Christopher Nolan",
                Cast = new List<string> { "Matthew McConaughey", "Anne Hathaway", "Jessica Chastain", "Michael Caine" },
                GenreIds = new List<string> { "khoa-hoc", "hanh-dong", "trinh-tham" },
                GenreNames = new List<string> { "Khoa học", "Hành động", "Trinh thám" },
                IsFeatured = true,
                FeaturedOrder = 4,
                IsSeries = false,
                EpisodeInfo = "Full IMAX",
                ViewsCount = 1680000,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            },

            // 5. Featured Hero Banner 5 - Phim Hoạt Hình / Anime
            new Movie
            {
                Id = "your-name-ten-bo-la-gi",
                Title = "Your Name: Tên Cậu Là Gì?",
                OriginalTitle = "Kimi no Na wa.",
                Slug = "your-name-ten-cau-la-gi",
                Tagline = "Ký ức mơ hồ nối liền hai số phận",
                Description = "Taki ở Tokyo và Mitsuha ở một vùng quê hẻo lánh bất ngờ bị hoán đổi thân xác qua những giấc mơ. Cả hai cùng nhau tìm kiếm sự thật đằng sau hiện tượng kỳ lạ và thảm họa sắp giáng xuống.",
                PosterUrl = "https://images.unsplash.com/photo-1534447677768-be436bb09401?auto=format&fit=crop&w=600&q=80",
                BannerUrl = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=1600&q=80",
                TrailerUrl = "https://www.youtube.com/embed/xU47nhruN-Q",
                ReleaseYear = 2025,
                Duration = "106 phút",
                Rating = 4.8,
                AgeRating = "P",
                Quality = "Full HD Ultra",
                LanguageMode = "Lồng Tiếng + Vietsub",
                Country = "Nhật Bản",
                Director = "Makoto Shinkai",
                Cast = new List<string> { "Ryunosuke Kamiki", "Mone Kamishibai", "Ryo Narita" },
                GenreIds = new List<string> { "hoat-hinh", "tinh-cam", "huyen-thoai" },
                GenreNames = new List<string> { "Phim hoạt hình", "Tình cảm", "Huyền thoại / Truyền thuyết" },
                IsFeatured = true,
                FeaturedOrder = 5,
                IsSeries = false,
                EpisodeInfo = "Bản Đẹp Vietsub",
                ViewsCount = 1320000,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },

            // 6. Kinh dị / Trinh thám
            new Movie
            {
                Id = "conan-tham-tu-lung-danh",
                Title = "Thám Tử Lừng Danh Conan: Tàu Ngầm Sắt Nốt Đen",
                OriginalTitle = "Detective Conan: Black Iron Submarine",
                Slug = "tham-tu-lung-danh-conan-tau-ngam-sat",
                Tagline = "Đấu trí căng thẳng tại trạm giám sát hải quân",
                Description = "Conan và tổ chức Áo Đen đụng độ nảy lửa tại cơ sở kết nối camera giám sát toàn cầu trên biển Pacific Buoy. Haibara bị đe dọa tính mạng khi thân phận thật bị hé lộ.",
                PosterUrl = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?auto=format&fit=crop&w=600&q=80",
                BannerUrl = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?auto=format&fit=crop&w=1600&q=80",
                TrailerUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ",
                ReleaseYear = 2025,
                Duration = "110 phút",
                Rating = 4.5,
                AgeRating = "P",
                Quality = "Full HD",
                LanguageMode = "Lồng Tiếng",
                Country = "Nhật Bản",
                Director = "Yuzuru Tachikawa",
                Cast = new List<string> { "Minami Takayama", "Megumi Hayashibara", "Rikiya Koyama" },
                GenreIds = new List<string> { "trinh-tham", "hoat-hinh", "hanh-dong" },
                GenreNames = new List<string> { "Trinh thám", "Phim hoạt hình", "Hành động" },
                IsFeatured = false,
                IsSeries = false,
                EpisodeInfo = "Full HD Lồng Tiếng",
                ViewsCount = 850000,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },

            // 7. Kinh dị
            new Movie
            {
                Id = "the-conjuring-ma-ca-rong",
                Title = "Ám Ảnh Kinh Hoàng: Nghi Lễ Ác Quỷ",
                OriginalTitle = "The Conjuring: Devil's Rite",
                Slug = "am-anh-kinh-hoang-nghi-le-ac-quy",
                Tagline = "Dựa trên hồ sơ điều tra có thật nguy hiểm nhất",
                Description = "Hai vợ chồng nhà điều tra hiện tượng siêu nhiên Ed và Lorraine Warren đối mặt với một thực thể quỷ dữ cổ xưa đang đe dọa một gia đình nghèo vùng nông thôn.",
                PosterUrl = "https://images.unsplash.com/photo-1509248961158-e54f6934749c?auto=format&fit=crop&w=600&q=80",
                BannerUrl = "https://images.unsplash.com/photo-1509248961158-e54f6934749c?auto=format&fit=crop&w=1600&q=80",
                TrailerUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ",
                ReleaseYear = 2024,
                Duration = "112 phút",
                Rating = 4.3,
                AgeRating = "18+",
                Quality = "4K HD",
                LanguageMode = "Vietsub",
                Country = "Âu Mỹ",
                Director = "Michael Chaves",
                Cast = new List<string> { "Patrick Wilson", "Vera Farmiga", "Ruairi O'Connor" },
                GenreIds = new List<string> { "kinh-di", "trinh-tham" },
                GenreNames = new List<string> { "Kinh dị", "Trinh thám" },
                IsFeatured = false,
                IsSeries = false,
                EpisodeInfo = "Bản Vietsub 18+",
                ViewsCount = 740000,
                CreatedAt = DateTime.UtcNow.AddDays(-12)
            },

            // 8. Cổ Trang / Võ Thuật / Huyền Thoại
            new Movie
            {
                Id = "truong-nguyet-tan-minh",
                Title = "Trường Nguyệt Tấn Minh",
                OriginalTitle = "Till The End Of The Moon",
                Slug = "truong-nguyet-tan-minh",
                Tagline = "Chuyện tình 3 kiếp ngược tâm giữa Ma Thần và Thần Nữ",
                Description = "Để cứu thế giới khỏi sự tàn phá của Ma Thần Đạm Đài Tẫn, Lê Tô Tô du hành về 500 năm trước, biến thành diệp tịnh để ngăn chặn hắn thức tỉnh ma cốt.",
                PosterUrl = "https://images.unsplash.com/photo-1534447677768-be436bb09401?auto=format&fit=crop&w=600&q=80",
                BannerUrl = "https://images.unsplash.com/photo-1578632767115-351597cf2477?auto=format&fit=crop&w=1600&q=80",
                TrailerUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ",
                ReleaseYear = 2025,
                Duration = "40 Tập",
                Rating = 4.7,
                AgeRating = "13+",
                Quality = "Full HD",
                LanguageMode = "Thuyết Minh",
                Country = "Trung Quốc",
                Director = "Cúc Giác Liang",
                Cast = new List<string> { "La Vân Hi", "Bạch Lộc", "Đặng Vi", "Trần Đô Linh" },
                GenreIds = new List<string> { "co-trang", "huyen-thoai", "tinh-cam", "ngon-tinh" },
                GenreNames = new List<string> { "Cổ Trang", "Huyền thoại / Truyền thuyết", "Tình cảm", "Ngôn tình" },
                IsFeatured = false,
                IsSeries = true,
                EpisodeInfo = "Trọn Bộ 40/40 Tập",
                ViewsCount = 1150000,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },

            // 9. Phim Việt Nam / Tình Cảm
            new Movie
            {
                Id = "mai-tran-thanh",
                Title = "Mai: Câu Chuyện Tình Yêu & Phận Người",
                OriginalTitle = "Mai",
                Slug = "mai-cau-chuyen-tinh-yeu",
                Tagline = "Ranh giới mong manh giữa tình yêu và định kiến",
                Description = "Mai - một người phụ nữ làm nghề mát-xa chịu nhiều định kiến xã hội, gặp gỡ Dương - một chàng trai đào hoa trẻ tuổi. Câu chuyện tình yêu chân thành nhưng đầy trắc trở.",
                PosterUrl = "https://images.unsplash.com/photo-1518199266791-5375a83190b7?auto=format&fit=crop&w=600&q=80",
                BannerUrl = "https://images.unsplash.com/photo-1516589178581-6cd7833ae3b2?auto=format&fit=crop&w=1600&q=80",
                TrailerUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ",
                ReleaseYear = 2024,
                Duration = "131 phút",
                Rating = 4.4,
                AgeRating = "18+",
                Quality = "Full HD",
                LanguageMode = "Tiếng Việt Bản Gốc",
                Country = "Việt Nam",
                Director = "Trấn Thành",
                Cast = new List<string> { "Phương Anh Đào", "Tuấn Trần", "Trấn Thành", "NSND Ngọc Giàu" },
                GenreIds = new List<string> { "tinh-cam", "ngon-tinh" },
                GenreNames = new List<string> { "Tình cảm", "Ngôn tình" },
                IsFeatured = false,
                IsSeries = false,
                EpisodeInfo = "Full HD Chiếu Rạp",
                ViewsCount = 2100000,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },

            // 10. Siêu anh hùng / Hành động
            new Movie
            {
                Id = "spiderman-no-way-home",
                Title = "Người Nhện: Không Còn Nhà",
                OriginalTitle = "Spider-Man: No Way Home",
                Slug = "nguoi-nhen-khong-con-nha",
                Tagline = "Đa vũ trụ mở ra - Ba Người Nhện hội tụ",
                Description = "Danh tính bị tiết lộ, Peter Parker nhờ Doctor Strange giúp thế giới quên đi câu chú nguyền. Tuy nhiên thảm họa đa vũ trụ xảy ra kéo theo các ác nhân từ những thế giới khác.",
                PosterUrl = "https://images.unsplash.com/photo-1635805737707-575885ab0820?auto=format&fit=crop&w=600&q=80",
                BannerUrl = "https://images.unsplash.com/photo-1534447677768-be436bb09401?auto=format&fit=crop&w=1600&q=80",
                TrailerUrl = "https://www.youtube.com/embed/rt-2cxAiPJk",
                ReleaseYear = 2024,
                Duration = "148 phút",
                Rating = 4.7,
                AgeRating = "13+",
                Quality = "4K HDR",
                LanguageMode = "Thuyết Minh",
                Country = "Âu Mỹ",
                Director = "Jon Watts",
                Cast = new List<string> { "Tom Holland", "Zendaya", "Benedict Cumberbatch", "Andrew Garfield", "Tobey Maguire" },
                GenreIds = new List<string> { "sieu-anh-hung", "hanh-dong", "khoa-hoc" },
                GenreNames = new List<string> { "Siêu anh hùng", "Hành động", "Khoa học" },
                IsFeatured = false,
                IsSeries = false,
                EpisodeInfo = "Full 4K HDR",
                ViewsCount = 1890000,
                CreatedAt = DateTime.UtcNow.AddDays(-18)
            }
        };
    }
}
