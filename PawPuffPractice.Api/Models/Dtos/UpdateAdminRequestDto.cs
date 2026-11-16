// 修改管理員的後端資料
using System.ComponentModel.DataAnnotations;

namespace PawPuffPractice.Api.Models.Dtos;

public class UpdateAdminRequestDto
{
    #region [Required(ErrorMessage = "請輸入暱稱")]
    // 如果前端沒有傳 nickname，結果仍然是：Nickname = ""
    // ==> 後端無法判斷：前端沒有要修改暱稱 還是 前端想把暱稱改成空字串
    //
    // string? ：這個欄位可以是字串，也可以是 null
    //
    // = string.Empty ：代表建立物件時，先把屬性設定成空字串。
    #endregion Nickname = string.Empty; <==> Nickname = "";
    #region 為什麼 PATCH 不適合？ => PATCH 允許只修改一個欄位
    // 例如前端只送：
    // {
    //   "email": "new@test.com"
    // }
    // 如果 DTO 是：
    // public string Nickname { get; set; } = string.Empty;
    // 沒有傳 nickname 時，結果仍然是：Nickname = ""
    // 後端看到空字串，無法清楚知道它代表：
    // 沒有要修改暱稱or要把暱稱改成空字串
    // 改成：
    // public string? Nickname { get; set; }
    // 沒有傳時就會是：Nickname = null
    // 可以明確規定：
    // null → 不修改這個欄位
    // 有字串 → 修改這個欄位
    // 總結：
    // 完整新增／完整更新 DTO →string + string.Empty + [Required]
    // 部分更新 PATCH DTO → string?，預設 null
    #endregion

    [StringLength(100, ErrorMessage = "暱稱最多100個字元")]
    public string? Nickname { get; set; }

    // [Required(ErrorMessage = "請輸入電子郵件")]
    [EmailAddress(ErrorMessage = "Email格式不正確")]
    public string? Email { get; set; }

    // null 、"" => 可以
    // "09xxxxxxxx" => 可以
    // "abcdefghij" => 失敗
    // "09123" => 失敗
    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "電話必須是10個數字")]
    public string? Phone { get; set; }
}
