// 新增管理員資料

using System.ComponentModel.DataAnnotations;

namespace PawPuffPractice.Api.Models.Dtos;

public class CreateAdminRequestDto
{
    [Required(ErrorMessage = "請輸入帳號")]
    [StringLength(20, MinimumLength = 6, ErrorMessage = "帳號必須是6~20個字元")]
    public string Account { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "密碼必須至少8個字元")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "請再次輸入密碼")]
    [Compare(nameof(Password), ErrorMessage = "密碼不一致")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入暱稱")]
    [StringLength(100, ErrorMessage = "暱稱最多100個字元")]
    public string Nickname { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入電子郵件")]
    [EmailAddress(ErrorMessage = "Email格式不正確")]
    public string Email { get; set; } = string.Empty;

    [StringLength(10, MinimumLength = 10, ErrorMessage = "電話必須是10個字元")]
    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "電話必須是10個數字")]
    public string? Phone { get; set; }

    // 測試 dotnet watch 自動更新 2026/10/13
}
