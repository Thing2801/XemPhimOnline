using System.Text.Json;
using MovieWeb.Core.Models;

namespace MovieWeb.Core.Services;

public class CheckinService : ICheckinService
{
    private readonly string _dataFolderPath;
    private readonly string _checkinsFilePath;
    private readonly string _vouchersFilePath;
    private readonly List<UserCheckin> _checkins = new();
    private readonly List<UserVoucher> _vouchers = new();
    private readonly object _lockObj = new();
    private static readonly Random _random = new();

    // Cột mốc phần thưởng
    private static readonly (int Day, int DiscountPercent, string Label)[] Milestones = new[]
    {
        (7, 20, "Giảm 20%"),
        (14, 40, "Giảm 40%"),
        (21, 80, "Giảm 80%")
    };

    public CheckinService()
    {
        _dataFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
        _checkinsFilePath = Path.Combine(_dataFolderPath, "checkins.json");
        _vouchersFilePath = Path.Combine(_dataFolderPath, "vouchers.json");
        LoadDataInternal();
    }

    private void LoadDataInternal()
    {
        lock (_lockObj)
        {
            if (!Directory.Exists(_dataFolderPath))
                Directory.CreateDirectory(_dataFolderPath);

            // Load checkins
            if (File.Exists(_checkinsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_checkinsFilePath);
                    var list = JsonSerializer.Deserialize<List<UserCheckin>>(json);
                    if (list != null)
                    {
                        _checkins.Clear();
                        _checkins.AddRange(list);
                    }
                }
                catch { }
            }

            // Load vouchers
            if (File.Exists(_vouchersFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_vouchersFilePath);
                    var list = JsonSerializer.Deserialize<List<UserVoucher>>(json);
                    if (list != null)
                    {
                        _vouchers.Clear();
                        _vouchers.AddRange(list);
                    }
                }
                catch { }
            }
        }
    }

    private void SaveCheckinsInternal()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_checkins, options);
            File.WriteAllText(_checkinsFilePath, json);
        }
        catch { }
    }

    private void SaveVouchersInternal()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_vouchers, options);
            File.WriteAllText(_vouchersFilePath, json);
        }
        catch { }
    }

    private static string GenerateVoucherCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var code = new char[8];
        lock (_random)
        {
            for (int i = 0; i < 8; i++)
                code[i] = chars[_random.Next(chars.Length)];
        }
        return new string(code);
    }

    public Task<CheckinResult> CheckinAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(new CheckinResult { Success = false, Message = "Vui lòng đăng nhập để điểm danh." });

        lock (_lockObj)
        {
            var now = DateTime.Now;
            var today = now.Date;
            int currentMonth = now.Month;
            int currentYear = now.Year;

            // Kiểm tra đã điểm danh hôm nay chưa
            bool alreadyChecked = _checkins.Any(c =>
                c.UserId == userId &&
                c.CheckinDate.Date == today);

            if (alreadyChecked)
            {
                int existingTotal = _checkins.Count(c =>
                    c.UserId == userId && c.Month == currentMonth && c.Year == currentYear);
                return Task.FromResult(new CheckinResult
                {
                    Success = false,
                    AlreadyCheckedIn = true,
                    Message = "Bạn đã điểm danh hôm nay rồi! Quay lại vào ngày mai nhé 🌟",
                    TotalDaysThisMonth = existingTotal
                });
            }

            // Lưu record checkin mới
            var checkin = new UserCheckin
            {
                UserId = userId,
                CheckinDate = today,
                DayOfMonth = today.Day,
                Month = currentMonth,
                Year = currentYear
            };
            _checkins.Add(checkin);
            SaveCheckinsInternal();

            // Đếm tổng số ngày đã điểm danh trong tháng hiện tại
            int totalDays = _checkins.Count(c =>
                c.UserId == userId && c.Month == currentMonth && c.Year == currentYear);

            // Kiểm tra có đạt cột mốc không
            UserVoucher? reward = null;
            foreach (var milestone in Milestones)
            {
                if (totalDays == milestone.Day)
                {
                    // Kiểm tra chưa có voucher cho milestone này trong tháng
                    bool alreadyRewarded = _vouchers.Any(v =>
                        v.UserId == userId &&
                        v.MilestoneDay == milestone.Day &&
                        v.CreatedAt.Month == currentMonth &&
                        v.CreatedAt.Year == currentYear);

                    if (!alreadyRewarded)
                    {
                        reward = new UserVoucher
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            UserId = userId,
                            VoucherCode = GenerateVoucherCode(),
                            DiscountPercent = milestone.DiscountPercent,
                            MilestoneDay = milestone.Day,
                            OriginalPrice = 55000,
                            CreatedAt = DateTime.UtcNow,
                            ExpiresAt = DateTime.UtcNow.AddDays(14),
                            IsUsed = false
                        };
                        _vouchers.Add(reward);
                        SaveVouchersInternal();
                    }
                    break;
                }
            }

            string message = reward != null
                ? $"🎉 Chúc mừng! Bạn đã điểm danh ngày thứ {totalDays} và nhận được Mã giảm giá {reward.DiscountPercent}%!"
                : $"✅ Điểm danh thành công! Đã điểm danh {totalDays}/21 ngày trong tháng.";

            return Task.FromResult(new CheckinResult
            {
                Success = true,
                Message = message,
                TotalDaysThisMonth = totalDays,
                RewardVoucher = reward
            });
        }
    }

    public Task<CheckinStatus> GetCheckinStatusAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(new CheckinStatus());

        lock (_lockObj)
        {
            var now = DateTime.Now;
            int currentMonth = now.Month;
            int currentYear = now.Year;

            var monthCheckins = _checkins
                .Where(c => c.UserId == userId && c.Month == currentMonth && c.Year == currentYear)
                .ToList();

            var checkedDays = monthCheckins.Select(c => c.DayOfMonth).Distinct().OrderBy(d => d).ToList();
            bool checkedToday = monthCheckins.Any(c => c.CheckinDate.Date == now.Date);

            // Build milestone info
            var milestones = new List<MilestoneInfo>();
            foreach (var ms in Milestones)
            {
                bool reached = checkedDays.Count >= ms.Day;
                string? voucherCode = null;
                if (reached)
                {
                    var voucher = _vouchers.FirstOrDefault(v =>
                        v.UserId == userId &&
                        v.MilestoneDay == ms.Day &&
                        v.CreatedAt.Month == currentMonth &&
                        v.CreatedAt.Year == currentYear);
                    voucherCode = voucher?.VoucherCode;
                }

                milestones.Add(new MilestoneInfo
                {
                    Day = ms.Day,
                    DiscountPercent = ms.DiscountPercent,
                    Reached = reached,
                    VoucherCode = voucherCode,
                    Label = ms.Label
                });
            }

            // Active vouchers (chưa dùng, chưa hết hạn)
            var activeVouchers = _vouchers
                .Where(v => v.UserId == userId && v.IsValid)
                .OrderByDescending(v => v.CreatedAt)
                .ToList();

            var firstDayOfMonth = new DateTime(currentYear, currentMonth, 1);
            int firstDayOffset = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;

            return Task.FromResult(new CheckinStatus
            {
                TotalDaysThisMonth = checkedDays.Count,
                CheckedDays = checkedDays,
                CheckedToday = checkedToday,
                CurrentMonth = currentMonth,
                CurrentYear = currentYear,
                CurrentDay = now.Day,
                DaysInMonth = DateTime.DaysInMonth(currentYear, currentMonth),
                FirstDayOffset = firstDayOffset,
                Milestones = milestones,
                ActiveVouchers = activeVouchers
            });
        }
    }

    public Task<List<UserVoucher>> GetUserVouchersAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(new List<UserVoucher>());

        lock (_lockObj)
        {
            var vouchers = _vouchers
                .Where(v => v.UserId == userId && !v.IsUsed && v.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(v => v.CreatedAt)
                .ToList();
            return Task.FromResult(vouchers);
        }
    }

    public Task<UserVoucher?> GetVoucherByCodeAsync(string userId, string voucherCode)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(voucherCode))
            return Task.FromResult<UserVoucher?>(null);

        lock (_lockObj)
        {
            var voucher = _vouchers.FirstOrDefault(v =>
                v.UserId == userId &&
                v.VoucherCode.Equals(voucherCode, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(voucher);
        }
    }

    public Task<UserVoucher?> ValidateVoucherAsync(string userId, string voucherCode)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(voucherCode))
            return Task.FromResult<UserVoucher?>(null);

        lock (_lockObj)
        {
            var voucher = _vouchers.FirstOrDefault(v =>
                v.UserId == userId &&
                v.VoucherCode.Equals(voucherCode, StringComparison.OrdinalIgnoreCase) &&
                !v.IsUsed &&
                v.ExpiresAt > DateTime.UtcNow);
            return Task.FromResult(voucher);
        }
    }

    public Task<ApplyVoucherResult> ApplyVoucherAsync(string userId, string voucherCode, string movieId, decimal originalPrice)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(new ApplyVoucherResult { Success = false, Message = "Vui lòng đăng nhập." });

        if (string.IsNullOrWhiteSpace(voucherCode))
            return Task.FromResult(new ApplyVoucherResult { Success = false, Message = "Mã voucher không hợp lệ." });

        lock (_lockObj)
        {
            var voucher = _vouchers.FirstOrDefault(v =>
                v.UserId == userId &&
                v.VoucherCode.Equals(voucherCode, StringComparison.OrdinalIgnoreCase));

            if (voucher == null)
                return Task.FromResult(new ApplyVoucherResult { Success = false, Message = "Mã voucher không tồn tại." });

            if (voucher.IsUsed)
                return Task.FromResult(new ApplyVoucherResult { Success = false, Message = "Mã voucher này đã được sử dụng." });

            if (voucher.ExpiresAt <= DateTime.UtcNow)
                return Task.FromResult(new ApplyVoucherResult { Success = false, Message = "Mã voucher đã hết hạn." });

            // Áp dụng voucher
            decimal finalPrice = originalPrice * (1 - voucher.DiscountPercent / 100m);
            finalPrice = Math.Max(finalPrice, 1000); // MoMo minimum 1000 VNĐ

            voucher.IsUsed = true;
            voucher.UsedAt = DateTime.UtcNow;
            voucher.UsedForMovieId = movieId;
            SaveVouchersInternal();

            return Task.FromResult(new ApplyVoucherResult
            {
                Success = true,
                Message = $"Áp dụng mã giảm giá {voucher.DiscountPercent}% thành công!",
                OriginalPrice = originalPrice,
                FinalPrice = finalPrice,
                DiscountPercent = voucher.DiscountPercent
            });
        }
    }
}
