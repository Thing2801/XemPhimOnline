using MovieWeb.Core.Models;

namespace MovieWeb.Core.Services;

/// <summary>Kết quả điểm danh</summary>
public class CheckinResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalDaysThisMonth { get; set; }
    public bool AlreadyCheckedIn { get; set; }
    /// <summary>Voucher nhận được (null nếu không đạt cột mốc)</summary>
    public UserVoucher? RewardVoucher { get; set; }
}

/// <summary>Trạng thái điểm danh tháng hiện tại</summary>
public class CheckinStatus
{
    public int TotalDaysThisMonth { get; set; }
    public List<int> CheckedDays { get; set; } = new();
    public bool CheckedToday { get; set; }
    public int CurrentMonth { get; set; }
    public int CurrentYear { get; set; }
    public int CurrentDay { get; set; }
    public int DaysInMonth { get; set; }
    public int FirstDayOffset { get; set; }
    public List<MilestoneInfo> Milestones { get; set; } = new();
    public List<UserVoucher> ActiveVouchers { get; set; } = new();
}

public class MilestoneInfo
{
    public int Day { get; set; }
    public int DiscountPercent { get; set; }
    public bool Reached { get; set; }
    public string? VoucherCode { get; set; }
    public string Label { get; set; } = string.Empty;
}

/// <summary>Kết quả áp dụng voucher</summary>
public class ApplyVoucherResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public int DiscountPercent { get; set; }
}

public interface ICheckinService
{
    /// <summary>Điểm danh hôm nay</summary>
    Task<CheckinResult> CheckinAsync(string userId);

    /// <summary>Lấy trạng thái điểm danh tháng hiện tại</summary>
    Task<CheckinStatus> GetCheckinStatusAsync(string userId);

    /// <summary>Lấy danh sách voucher còn hiệu lực của user</summary>
    Task<List<UserVoucher>> GetUserVouchersAsync(string userId);

    /// <summary>Lấy voucher theo mã code</summary>
    Task<UserVoucher?> GetVoucherByCodeAsync(string userId, string voucherCode);

    /// <summary>Áp dụng voucher khi thanh toán</summary>
    Task<ApplyVoucherResult> ApplyVoucherAsync(string userId, string voucherCode, string movieId, decimal originalPrice);

    /// <summary>Kiểm tra và lấy voucher để tính giá (không đánh dấu đã dùng)</summary>
    Task<UserVoucher?> ValidateVoucherAsync(string userId, string voucherCode);
}
