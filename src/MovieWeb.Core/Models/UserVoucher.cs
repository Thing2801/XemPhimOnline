namespace MovieWeb.Core.Models;

public class UserVoucher
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    /// <summary>Mã giảm giá duy nhất (8 ký tự uppercase)</summary>
    public string VoucherCode { get; set; } = string.Empty;
    /// <summary>Phần trăm giảm giá: 20, 40, hoặc 80</summary>
    public int DiscountPercent { get; set; }
    /// <summary>Giá gốc áp dụng (55000 VNĐ)</summary>
    public decimal OriginalPrice { get; set; } = 55000;
    /// <summary>Cột mốc ngày đạt được (7, 14, 21)</summary>
    public int MilestoneDay { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Hạn sử dụng (14 ngày kể từ khi nhận)</summary>
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? UsedForMovieId { get; set; }

    /// <summary>Giá sau khi giảm</summary>
    public decimal DiscountedPrice => OriginalPrice * (1 - DiscountPercent / 100m);
    /// <summary>Kiểm tra voucher còn hiệu lực</summary>
    public bool IsValid => !IsUsed && ExpiresAt > DateTime.UtcNow;
}
