
using System.ComponentModel.DataAnnotations;

namespace PawPuffPractice.Api.Models.Dtos;

public class LoginRequestDto
{
    [Required(ErrorMessage ="請輸入帳號")]
    public string Account{get;set;} = string.Empty;

    [Required(ErrorMessage ="請輸入密碼")]
    public string Password{get;set;} = string.Empty;
}
