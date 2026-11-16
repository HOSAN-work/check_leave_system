// 假單審核人員 ->審核假單 可以選擇"已核准"或"已駁回"
using System.ComponentModel.DataAnnotations;

namespace PawPuffPractice.Api.Models.Dtos;

public class ReviewAdminLeaveRequestDto
{
    // 2:已核准 、3:已駁回
    [Range(2, 3, ErrorMessage = "審核結果只能是核准或駁回")]
    public sbyte ApprovalStatus { get; set; }

    [StringLength(500)]
    public string? RejectionReason { get; set; }
    // string? 允許核准時不傳原因
}
