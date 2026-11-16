#region  什麼時候需要 DTO？
// 1. 前端用 JSON 傳資料給後端
// 例如修改管理員狀態，前端傳：
// {
//   "status": 8
// }
// Controller 需要一個物件接住 JSON：
// public async Task<IActionResult> UpdateAdminsStatus(
//     int id,
//     UpdateAdminStatusRequestDto request
// )
// DTO：
// public class UpdateAdminStatusRequestDto
// {
//     public sbyte Status { get; set; }
// }

// 2. 需要欄位驗證
// 例如註冊資料：
// public class RegisterRequestDto
// {
//     [Required]
//     public string Account { get; set; } = string.Empty;

//     [EmailAddress]
//     public string Email { get; set; } = string.Empty;
// }
// DTO 可以集中處理：
// 必填
// 字數
// Email 格式
// 數字範圍
// 正規表示式

// 3. 不希望前端直接修改整個資料庫模型
// 資料庫 User 可能有：
// Id
// Account
// PasswordHash
// PointBalance
// Status
// Role
// CreatedAt
// 修改狀態時，前端只應該傳：
// Status
// 如果直接接收：
// User user
// 惡意使用者可能額外傳入：
// {
//   "status": 8,
//   "role": "Admin",
//   "pointBalance": 999999
// }
#endregion
#region  什麼時候不一定需要 DTO？
// 例如：
// GET /api/AdminUsers/admins/54
// 可以直接使用：
// public async Task<IActionResult> GetAdmin(int id)
// 因為只有一個簡單的路由參數，不必為 id 建立 DTO

// 例如：
// GET /api/AdminUsers/admins?status=8
// 可以先直接寫：
// public IActionResult GetAdmins(sbyte? status)

// 如果搜尋條件變很多，才適合整理成 DTO：
// public class AdminSearchRequestDto
// {
//     public string? Keyword { get; set; }
//     public sbyte? Status { get; set; }
//     public int Page { get; set; }
// }
#endregion
#region 為什麼會員和管理員需要不同 DTO？
// 會員 DTO：
// public class UpdateUserStatusRequestDto
// {
//     [Range(1, 7)]
//     public sbyte Status { get; set; }
// }
// 它的規則是會員專用的：允許 1～7
// 管理員允許的是：1、8、9、10
// ===> 如果管理員繼續使用會員 DTO，8～10 會被 [Range(1, 7)] 擋掉。
//
// 因此才需要分開：
// UpdateUserStatusRequestDto
// → 接收會員狀態
//
// UpdateAdminStatusRequestDto
// → 接收管理員狀態
#endregion
// 使用DTO的判斷原則可以記成：
// API 要接收一組資料＋需要限制欄位或驗證規則＝適合建立 DTO
namespace PawPuffPractice.Api.Models.Dtos;

public class UpdateAdminStatusRequestDto
{
    public sbyte Status { get; set; }
}
