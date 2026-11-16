using System.ComponentModel.DataAnnotations;

namespace PawPuffPractice.Api.Models.Dtos;

// 建立「請假申請 DTO」，定義前端可以送給後端的資料
public class CreateAdminLeaveRequestDto
{
    // 假別編號
    [Range(1, 5)]
    public int LeaveType { get; set; }

    // 允許先接收「沒有傳時間」的情況，再由 [Required] 判斷缺少必填資料
    [Required]
    public DateTime? StartTime { get; set; }

    [Required]
    public DateTime? EndTime { get; set; }

    // 請假原因
    [Required]
    [StringLength(100)]
    public string Reason { get; set; } = string.Empty;

    // 1：09:00～18:00；2：10:00～19:00
    [Required]
    [Range(1, 2, ErrorMessage = "請選擇有效班別")]
    public sbyte ClassType { get; set; }

    // 不放 AdminId 與 ApprovalStatus：申請人應從登入身分取得，審核狀態由後端設定，避免前端自行冒用身分或核准。
}
