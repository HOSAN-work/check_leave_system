using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Execution;
using Microsoft.Build.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using PawPuffPractice.Api.Data;
using PawPuffPractice.Api.Models.Dtos;
using PawPuffPractice.Api.Models.EfModels;
using PawPuffPractice.Api.Services;

namespace PawPuffPractice.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // 只有管理員能查看會員清單、管理員清單
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly PawPuffPracticeMySQLDbContext _dbcontext;

        public AdminUsersController(PawPuffPracticeMySQLDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        // 取得所有會員狀態選項
        #region  沒有 API的話
        // Vue
        // └── 自己保存狀態 2～7
        // 後端
        // └── 也保存狀態 1～7
        //
        // 假設後端將狀態 3 改成：惡意使用平台
        //
        // 後端：
        // new(
        // RuleViolation,
        // "惡意使用平台"
        // )
        // 但是 Vue 忘了修改：
        // {
        //   value: 3,
        //   label: '違反使用規範'
        // }
        // 結果會不一致：
        // Vue 顯示：違反使用規範
        // 後端訊息：惡意使用平台
        #endregion => 有 API 時，Vue 直接取得後端資料，所以比較不容易不一致。
        #region  有 API的話
        // 後端
        // └── 保存正式狀態定義
        //       ↓
        // status-options API
        //       ↓
        // Vue 取得下拉選項
        #endregion => Vue 不必再自己寫一份相同資料

        // 兩種做法都正確
        #region  做法一：前端固定寫死
        // const statusOptions = [
        //   { value: 2, label: '會員自行申請停用' },
        //   { value: 3, label: '違反使用規範' }
        // ]
        // 優點：
        // - 簡單
        // - 不用多呼叫 API
        // - 不用處理選項載入失敗
        // - 適合固定、不常改的少量選項
        // 缺點：
        // - 前端、後端各自維護
        // - 文字改動時兩邊都要修改
        #endregion - 容易不一致
        #region 做法二：從後端 API 取得
        // const statusOptions = ref([])
        //
        // const response = await api.get(
        //   '/api/AdminUsers/status-options'
        // )
        // statusOptions.value = response.data.statusOptions
        // 優點：
        // - 後端是唯一正式來源
        // - 修改一次後端定義即可
        // - 多個前端可以取得同一份選項
        // - 比較適合狀態可能改變的系統
        // 缺點：
        // - 多一支 API
        // - 多一次 Request
        // - 要處理載入和錯誤
        #endregion - 程式較複雜

        #region IActionResult 與 Task<IActionResult> 區別
        //  IActionResult → 現在就能產生 HTTP 結果
        // Task<IActionResult> → 非同步工作完成後，才會產生 HTTP 結果
        #endregion

        // GET /api/AdminUsers/member-status-options
        [HttpGet("member-status-options")]
        // 這個方法不需要async await Task<IActionResult> 因為它沒有查詢 MySQL，只是讀取記憶體裡的共用狀態清單
        public IActionResult GetUsersStatusOptions()
        {
            // 取得後,return 200k => return statusOptions
            return Ok(new { statusOptions = UserStatusDefinitions.All });
        }

        // 取得所有管理員狀態選項
        // GET /api/AdminUsers/admin-status-options
        [HttpGet("admin-status-options")]
        public IActionResult GetAdminStatusOptions()
        {
            return Ok(new { adminStatusOptions = AdminStatusDefinitions.All });
        }

        //「取得Users的會員清單」
        // Get/api/AdminUsers/members
        [HttpGet("members")]
        public async Task<IActionResult> GetMembers()
        {
            // 查詢 Member
            var members = await _dbcontext
                .Users
                // 這次只是讀取會員資料，不需要 EF Core 追蹤每個會員物件的變化
                .AsNoTracking()
                .Where(user => user.Role == "Member")
                // 表示依照會員 Id 從小到大排序
                .OrderBy(user => user.Id)
                // 從每一筆會員資料中，只選出下面指定的欄位
                .Select(user => new
                {
                    // 只列出可以公開給管理員前端的資料
                    user.Id,
                    user.Account,
                    user.Nickname,
                    user.Email,
                    user.Phone,
                    user.PointBalance,
                    user.Status,
                    user.Role,
                    user.CreatedAt,
                    user.LastLoginAt,
                })
                // 才真正要求 EF Core 執行 SQL，從 MySQL 取得結果並組成清單
                .ToListAsync();

            return Ok(new { members });
        }

        // 操作會員的停用啟用功能
        [HttpPut("members/{id:int}/status")]
        public async Task<IActionResult> UpdateMembersStatus(
            int id,
            UpdateUserStatusRequestDto request
        )
        {
            // 只允許修改一般會員
            // 從 Users 資料表找出第一筆「Id 等於指定 Id，而且 Role 是 Member」的會員；找不到就回傳 null
            var member = await _dbcontext.Users.FirstOrDefaultAsync(user =>
                user.Id == id && user.Role == "Member"
            );

            if (member == null)
            {
                return NotFound(new { message = "找不到一般會員" });
            }

            #region
            // 把前端傳來的 int 狀態值，轉成 User.Status 欄位使用的 sbyte
            // sbyte 是什麼？
            // sbyte 是有正負號的 8 位元整數：
            // 最小值：-128
            // 最大值：127
            // int 範圍則大很多：
            // -2,147,483,648 到 2,147,483,647
            //
            // MySQL 的：TINYINT
            // 在 C# Entity 中可能被對應為：sbyte
            //
            // 所以如果資料表是：
            // Status TINYINT NOT NULL
            // 反向產生 Entity 時，就可能得到：
            // public sbyte Status { get; set; }
            //
            // 為什麼需要 (sbyte)？
            // C# 不會自動把範圍較大的 int 放進範圍較小的 sbyte
            // 如果直接寫：member.Status = request.Status;
            // 可能出現編譯錯誤：Cannot implicitly convert type 'int' to 'sbyte'
            //
            // 因為 request.Status 可能是：100000,但 sbyte 放不下
            // 所以必須明確寫：(sbyte)request.Status
            // 意思是：我知道要把這個 int 轉成 sbyte
            // =>「明確型別轉換」
            //
            // 例子 request.Status = 2;
            // 轉型：(sbyte)request.Status
            // 結果是：(sbyte)2
            // 再指定給：member.Status = 2;
            //
            // 例子 request.Status = 999; => 超出 sbyte 範圍
            // member.Status = (sbyte)request.Status;
            //
            // 更安全的轉型方式:
            // 如果不能保證數值已驗證，可以使用：
            // member.Status = checked((sbyte)request.Status);
            // 如果超出 sbyte 範圍，就會拋出溢位錯誤，而不是產生錯誤數值
            // 但你這裡最重要的還是先透過 DTO [Range] 限制合法狀態。
            #endregion
            member.Status = request.Status;

            await _dbcontext.SaveChangesAsync();

            return Ok(
                new
                {
                    message = request.Status == 1 ? "會員已啟用" : "會員已停權",

                    member = new
                    {
                        member.Id,
                        member.Account,
                        member.Nickname,
                        member.Status,
                        member.Role,
                    },
                }
            );
        }

        //「取得Users的管理員清單」
        // Get/api/AdminUsers/admins
        [HttpGet("admins")]
        public async Task<IActionResult> GetAdmins()
        {
            // 查詢 Admin
            var admins = await _dbcontext
                .Users.AsNoTracking()
                .Where(user => user.Role == "Admin")
                .OrderBy(user => user.Id)
                #region 匿名物件：只在這個方法用一次
                // Select(user => new
                // {
                //     user.Id,
                //     user.Account,
                //     user.Nickname,
                // })
                // 適合：
                // 只有這支 API 使用
                // 資料結構簡單
                // 不需要在其他方法重複使用
                // 不需要把型別傳給其他方法
                // 它只能在目前方法內使用，不能宣告成公開的方法回傳型別。
                // ----------
                // DTO：多個地方需要相同欄位
                // public class AdminClockDto
                // {
                //   public int Id { get; set; }
                //   public string AdminAccount { get; set; } = stringEmpty;
                //   public DateOnly WorkDate { get; set; }
                //   public DateTime ClockInTime { get; set; }
                //   public DateTime? ClockOutTime { get; set; }
                // }
                //  你的打卡功能有多支 API 都需要相同的紀錄結構：
                // GET  /me
                // POST /clock-in
                // POST /clock-out
                // GET  /all
                // GET  /today
                //
                // 所以使用：
                // new AdminClockDto
                // {
                //     Id = clock.Id,
                //     AdminAccount = clock.Admin.Account,
                //     WorkDate = clock.WorkDate,
                //     ClockInTime = clock.ClockInTime,
                //     ClockOutTime = clock.ClockOutTime,
                // }
                // ===> 好處是每支 API 都能保持相同欄位
                //
                // 如果不用 DTO，可能出现：
                // /me
                // new
                // {
                //     clock.Id,
                //     clock.WorkDate,
                //     clock.ClockInTime,
                // }
                //
                // /all
                // new
                // {
                //     clock.Id,
                //     clock.Admin.Account,
                //     clock.WorkDate,
                //     clock.ClockInTime,
                //     clock.ClockOutTime,
                // }
                //
                // 只在一個查詢中臨時使用 → 匿名物件可以
                // 多支 API 共用相同結構 → 建立 DTO
                // 接收前端傳入資料並需要驗證 → 建立 DTO
                // 希望 API 結構明確、方便維護 → 建立 DTO
                #endregion
                .Select(user => new
                {
                    user.Id,
                    user.Account,
                    user.Nickname,
                    user.Email,
                    user.Phone,
                    user.Status,
                    user.Role,
                    user.CreatedAt,
                    user.LastLoginAt,
                })
                .ToListAsync();

            return Ok(new { admins });
        }

        // 操作管理員員的停用啟用功能
        [HttpPut("admins/{id:int}/status")]
        public async Task<IActionResult> UpdateAdminsStatus(
            int id,
            UpdateAdminStatusRequestDto request
        )
        {
            // 只允許管理員定義中存在的狀態
            #region  假如：Status = 1
            // AdminStatusDefinitions.IsValid(1)
            // return All.Any(option => 1 == 1)
            // ==> return true
            // ==> !true ==> false
            //
            // 假如：Status = 11
            // AdminStatusDefinitions.IsValid(11)
            // return All.Any(option => x == 11)
            // ==> return false
            // ==> !false ==> true
            // return 400 , "無效的管理員狀態"
            #endregion
            if (!AdminStatusDefinitions.IsValid(request.Status))
            {
                return BadRequest(new { message = "無效的管理員狀態" });
            }

            var admin = await _dbcontext.Users.FirstOrDefaultAsync(user =>
                user.Id == id && user.Role == "Admin"
            );

            if (admin == null)
            {
                return NotFound(new { message = "找不到管理員" });
            }

            //「禁止管理員停用自己」的保護
            // 改善前端：自己的「停用」按鈕直接禁用，不必點擊後才看到錯誤
            string? currentAdminIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // currentAdminIdText="2"
            // true , currentAdminId=2
            // -> false ,currentAdminId=2 -> 不會進if
            //
            // currentAdminIdText="abc"
            // false, currentAdminId=null
            // true, currentAdminId=null -> 進if
            if (!int.TryParse(currentAdminIdText, out int currentAdminId))
            {
                return Unauthorized(new { message = "無法取得目前登入的管理員資料" });
            }

            // 禁止停用自己的判斷
            #region  假設目前是id=101
            // click 停用button
            // currentAdminId=101,選擇的狀態request.Status=2
            // 後端： id==currentAdminId -> 101==101 -> true
            // 2 != 1 -> true
            // if(true && true) => return BadRequest
            //
            // id=102
            // currentAdminId=101
            // 102==101 -> false  -> 可以停用id=102
            #endregion
            if (id == currentAdminId && request.Status != AdminStatusDefinitions.Active)
            {
                return BadRequest(new { message = "不能停用目前登入中的管理員帳號" });
            }

            admin.Status = request.Status;

            await _dbcontext.SaveChangesAsync();

            return Ok(
                new
                {
                    // 在上面的if,管理員狀態被改為：request.Status = 8
                    // message = request.Status = 8 == 1
                    // ==> false
                    // ==> "管理員狀態已改為：離職"
                    message = request.Status == AdminStatusDefinitions.Active
                        ? "管理員已啟用"
                        : $"管理員狀態已改為：{AdminStatusDefinitions.GetText(request.Status)}",

                    admin = new
                    {
                        admin.Id,
                        admin.Account,
                        admin.Nickname,
                        admin.Status,
                        admin.Role,
                    },
                }
            );
        }

        // 新增管理員
        [HttpPost("admins")]
        public async Task<IActionResult> CreateAdmin(CreateAdminRequestDto request)
        {
            string account = request.Account.Trim();
            bool accountExists = await _dbcontext.Users.AnyAsync(user => user.Account == account);
            if (accountExists)
            {
                return Conflict(new { message = "此帳號已被使用" });
            }

            string email = request.Email.Trim().ToLowerInvariant(); //把 Email 統一轉成小寫
            bool emailExists = await _dbcontext.Users.AnyAsync(user => user.Email == email);
            if (emailExists)
            {
                return Conflict(new { message = "此Email已被使用" });
            }

            string nickname = request.Nickname.Trim();

            string? phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
            if (phone is not null)
            {
                bool phoneExists = await _dbcontext.Users.AnyAsync(user => user.Phone == phone);

                if (phoneExists)
                {
                    return Conflict(new { message = "此電話已被使用" });
                }
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 保存的是一個 User 物件,變數取的名稱:admin
            // 「建立準備寫入資料庫的完整 User 物件」
            // 也相當於 var admin = new User {...}
            User admin = new User
            {
                Account = account,
                PasswordHash = passwordHash,
                Nickname = nickname,
                Email = email,
                Phone = phone,
                PointBalance = 0,
                Status = 1,
                Role = "Admin",
                Biography = null,
                CreatedAt = DateTime.Now,
                LastLoginAt = null,
            };

            _dbcontext.Users.Add(admin);

            await _dbcontext.SaveChangesAsync();

            // return Ok();
            // 代表:
            // HTTP 狀態碼：200 OK
            // 回傳內容：沒有資料
            // 由以下替換

            // 201 Created 比 200 OK 更精確，
            // 表示：請求成功，而且建立了一筆新資料
            // 「選擇哪些資料可以回傳給前端」
            #region 後端回傳結果會類似：
            //             {
            //   "message": "管理員新增成功",
            //   "admin": {
            //     "id": 309,
            //     "account": "admin02",
            //     "nickname": "第二位管理員",
            //     "email": "admin02@test.com",
            //     "status": 1,
            //     "role": "Admin"
            //   }
            #endregion }
            // 完整的 User admin 物件
            //         ↓ 寫入資料庫
            // SaveChangesAsync()
            //         ↓
            // 挑選安全欄位(不包含PasswordHash)
            //         ↓
            // 回傳給前端
            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    message = "管理員新增成功",
                    admin = new
                    {
                        admin.Id,
                        admin.Account,
                        admin.Nickname,
                        admin.Email,
                        admin.Phone,
                        admin.Status,
                        admin.Role,
                        admin.CreatedAt,
                        admin.LastLoginAt,
                    },
                }
            );
        }

        // POST：新增資料
        // PUT： 送出完整的可編輯資料，目前採用
        // PATCH 通常表示只更新部分欄位,例如只改 Email
        // 只送出發生變化的欄位，也可以，但 DTO 和後端邏輯要重新設計

        #region 如果是put的話 三個欄位是一定要寫嗎?
        // 如果三個欄位都是必填：[Required]
        // 那使用 PUT 時，三個欄位都要傳：
        // {
        //   "nickname": "新暱稱",
        //   "email": "new@test.com",
        //   "phone": "0912345678"
        // }
        // 即使只想修改暱稱，也要把原本的 Email、Phone 一起傳回去。
        //
        // 所以用put的話 前端畫面是先顯示原先的值？
        // 前端通常會先把管理員目前的完整資料放進表單：
        // editAdminForm.value = {
        //   nickname: admin.nickname,
        //   email: admin.email,
        //   phone: admin.phone
        // }
        // 畫面會顯示原本的暱稱、Email、電話。使用者只修改暱稱後，送出時仍然傳送三個欄位：
        // {
        //   nickname: editAdminForm.value.nickname,
        //   email: editAdminForm.value.email,
        //   phone: editAdminForm.value.phone
        // }
        #endregion

        // 部分修改管理員資料
        // "admins/{id:int}" => 管理員可以改其他管理員資料
        [HttpPatch("admins/{id:int}")]
        public async Task<IActionResult> UpdateAdmin(int id, UpdateAdminRequestDto request)
        {
            // Nickname、Email、Phone欄位都沒有傳,代表沒有資料要修改
            // 前端送{}
            // -> DTO會得到：Nickname=null\Email=null\Phone=null
            // true && true && true
            // => return 400 ,"請至少提供一個要修改的欄位"
            if (request.Nickname is null && request.Email is null && request.Phone is null)
            {
                return BadRequest(new { message = "請至少提供一個要修改的欄位" });
            }
            // 尋找第一筆符合id且為Admin的條件的資料
            // FirstOrDefaultAsync() 會回傳：
            // - 找到：回傳完整的 User 物件。
            // - 找不到：回傳 null。
            //
            // 如果改用 AnyAsync()：
            // 只會回傳：true or false
            // 不會取得管理員物件，所以不能這樣寫
            //
            // AnyAsync()：只問「有沒有？」
            // FirstOrDefaultAsync()：把符合條件的那筆資料「取出來」，還會讓 EF Core 追蹤這個 admin 物件
            var admin = await _dbcontext.Users.FirstOrDefaultAsync(user =>
                user.Id == id && user.Role == "Admin"
            );

            if (admin == null)
            {
                return NotFound(new { message = "找不到管理員" });
            }
            // 前端有傳 Nickname 時，才修改暱稱
            if (request.Nickname is not null)
            {
                string nickname = request.Nickname.Trim();
                // 為什麼還要檢查空白？
                // 前端可能傳：
                // {
                //   "nickname": "   "
                // }
                // 它不是 null，所以會進入 if
                // ==> Trim() 後會變成：""
                // 因此必須阻止它把管理員暱稱改成空白
                if (string.IsNullOrWhiteSpace(nickname))
                {
                    return BadRequest(new { message = "暱稱不能是空白" });
                }
                admin.Nickname = nickname;
            }
            // 前端有傳 Email 時，才修改 Email
            if (request.Email is not null)
            {
                string email = request.Email.Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest(new { message = "Email不能是空白" });
                }

                // AnyAsync()用來檢查「資料庫裡是否存在符合條件的資料」
                bool emailAlreadyExists = await _dbcontext.Users.AnyAsync(user =>
                    user.Email == email && user.Id != id
                );
                if (emailAlreadyExists)
                {
                    return Conflict(new { message = "此Email已被其他帳號使用" });
                }
                admin.Email = email;
            }
            // 前端有傳 Phone 時，才修改電話
            if (request.Phone is not null)
            {
                string phone = request.Phone.Trim();

                // 前端明確傳入空字串，代表清除原本電話
                if (string.IsNullOrWhiteSpace(phone))
                {
                    admin.Phone = null;
                }
                // 不為空字串
                // 驗證：此電話有無存在
                else
                {
                    bool phoneAlreadyExists = await _dbcontext.Users.AnyAsync(user =>
                        user.Phone == phone && user.Id != id
                    );
                    // 此電話存在
                    if (phoneAlreadyExists)
                    {
                        return Conflict(new { message = "此電話已被其他帳號使用" });
                    }
                    // 此電話不存在,可存入
                    admin.Phone = phone;
                }
            }

            // return Ok();
            // 由下方替代
            // 真正存入資料庫
            await _dbcontext.SaveChangesAsync();
            // 儲存的是被追蹤且已變更的 admin
            return Ok(
                new
                {
                    message = "管理員資料修改成功",
                    admin = new
                    {
                        admin.Id,
                        admin.Account,
                        admin.Nickname,
                        admin.Email,
                        admin.Phone,
                        admin.Status,
                        admin.Role,
                        admin.CreatedAt,
                        admin.LastLoginAt,
                    },
                }
            );
        }
    }
}
