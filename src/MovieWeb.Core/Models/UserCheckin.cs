namespace MovieWeb.Core.Models;

public class UserCheckin
{
    public string UserId { get; set; } = string.Empty;
    public DateTime CheckinDate { get; set; }
    /// <summary>Ngày trong tháng (1-31)</summary>
    public int DayOfMonth { get; set; }
    /// <summary>Tháng (1-12)</summary>
    public int Month { get; set; }
    /// <summary>Năm</summary>
    public int Year { get; set; }
}
