using System.ComponentModel.DataAnnotations;

namespace PawPuffPractice.Api.Models.Dtos;
public class UpdateUserStatusRequestDto
{
// Status = 1：正常啟用
// Status = 2～7：停權 ,但不同原因
    [Range(1,7,ErrorMessage ="帳號狀態必須介於1到7")]
    // public int Status {get;set;}
    // 可以讓 DTO 也使用 sbyte 嗎？ => 可以
    // public sbyte Status { get; set; }
    // 這樣指定時就不用轉型：member.Status = request.Status;

    public sbyte Status{get;set;}
}
