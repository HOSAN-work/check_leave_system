using System.ComponentModel.DataAnnotations;

namespace PawPuffPractice.Api.Models.Dtos;

public class ChangePasswordRequestDto
{
    [Required(ErrorMessage ="目前密碼為必填")]
    public string CurrentPassword{get;set;} = string.Empty;
    [Required(ErrorMessage ="新密碼為必填")]
    [StringLength(100,MinimumLength =6,ErrorMessage ="新密碼長度必須介於6到100個字")]
    public string NewPassword{get;set;} =string.Empty;
    [Required(ErrorMessage ="確認新密碼為必填")]
    [Compare(nameof(NewPassword),ErrorMessage ="新密碼與確認密碼不一致")]
    public string ConfirmNewPassword{get;set;} =string.Empty;
    
}