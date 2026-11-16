// 只允許前端修改:Nickname,Email,Phone,Biography

using System.ComponentModel.DataAnnotations;
using Microsoft.Build.Tasks;

namespace PawPuffPractice.Api.Models.Dtos;

public class UpdateProfileRequestDto
{
    [Required(ErrorMessage = "暱稱為必填")]
    [StringLength(50,ErrorMessage ="暱稱最多50個字")]
    public string Nickname {get;set;} = string.Empty;
    [Required(ErrorMessage = "Email為必填")]
    [StringLength(255,ErrorMessage ="Email最多255個字")]
    [EmailAddress(ErrorMessage ="Email格式不正確")]
    public string Email{get;set;} = string.Empty;
    [StringLength(10,ErrorMessage ="電話最多10個字")]
    [Phone(ErrorMessage ="電話格式不正確")]
    public string? Phone {get;set;}
    [StringLength(500,ErrorMessage ="簡介最多500個字")]
    public string? Biography {get;set;}
}