using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawPuffPractice.Api.Data;
using PawPuffPractice.Api.Models.Dtos;
using PawPuffPractice.Api.Models.EfModels;
using PawPuffPractice.Api.Services;

namespace PawPuffPractice.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminClocksController : ControllerBase
    {
        #region 把 EF Core 的資料庫物件注入 Controller，讓 AdminClocksController 可以查詢或修改資料庫。
        // 建立一個欄位：
        // 型別：PawPuffPracticeMySQLDbContext
        // 名稱：_dbcontext
        // 之後整個 Controller 都可以使用：
        // _dbcontext.Users
        // _dbcontext.AdminClocks
        // _dbcontext.SaveChangesAsync()
        //
        // readonly 表示這個欄位只能在：宣告時、建構子內
        //
        //
        // _dbcontext = dbcontext;
        // 右邊：dbcontext --> 「ASP.NET Core 傳進建構子的 DbContext」
        // 左邊：_dbcontext --> 「Controller 自己保存的欄位」
        // ==> 把 ASP.NET Core 提供的資料庫物件，保存到 Controller 的 _dbcontext 欄位。
        #endregion
        private readonly PawPuffPracticeMySQLDbContext _dbcontext;

        // 如果沒有這個欄位，建構子收到的 dbcontext 只能在建構子內使用，其他 API 方法拿不到它
        // 為什麼需要 _dbcontext 欄位？
        // 你希望 Controller 裡的 API 方法可以寫：_dbcontext.AdminClocks
        public AdminClocksController(PawPuffPracticeMySQLDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        // 從目前登入 Cookie 的 Claims 取得管理員 ID
        // 嘗試從目前登入者的 Cookie 身分資料中，取得管理員 ID，並確認它是大於 0 的整數
        // return true/false , 取得的adminId帶出去
        private bool TryGetCurrentAdminId(out int adminId)
        {
            #region 複習 ClaimTypes
            //
            // .NET官方就有事先設定ClaimTypes.xxx 是string
            //
            // ClaimTypes.NameIdentifier -> .NET 預先定義的標準類型
            // 表示：使用者的唯一識別值
            //
            // ClaimTypes.Name -> NET 預先定義的標準類型
            // 表示目前使用者的名稱
            //
            // "nickname" -> 不是 .NET 預先定義的標準 Claim
            // 使用自訂名稱： "nickname"
            //在 C# 裡，只要用雙引號包起來，就是 string 字串常值
            //
            // "status" -> 不是 .NET 預先定義的標準 Claim
            // 使用自訂名稱： "status"
            //在 C# 裡，只要用雙引號包起來，就是 string 字串常值
            //
            // ClaimTypes.Role -> NET 預先定義的標準類型
            // 表示目前使用者的權限、角色
            #endregion
            #region User 是 Controller 目前 HTTP 請求的「登入者身分物件」，型別是：ClaimsPrincipal
            //
            // 流程：
            // 瀏覽器送出登入 Cookie
            //         ↓
            // Cookie Authentication 驗證 Cookie
            //         ↓
            // 還原登入時保存的 Claims
            //         ↓
            // 建立 ClaimsPrincipal
            //         ↓
            // 放進 HttpContext.User
            //         ↓
            // Controller 使用 User 簡寫取得
            //
            // HttpContext.User -> 在 Controller 裡可以簡寫成：User
            //
            //
            // User.FindFirstValue(ClaimTypes.Name)
            // |
            // 概念相同
            // v
            // HttpContext.User.FindFirstValue(ClaimTypes.Name)
            // 意思是：從目前登入者的 Claims 裡，尋找類型為 ClaimTypes.Name 的值。
            #endregion
            string? adminIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. adminIdText 能不能轉成 int && 2. 轉換後的 adminId 是否大於 0
            return int.TryParse(adminIdText, out adminId) && adminId > 0;
            #region 例如：
            // adminIdText = "100"
            //
            // int.TryParse("100", out adminId)
            // => return true , adminId = 100
            //
            // 100 > 0 => true
            //
            //  ===> return true && true = true
            // -------------------------------------
            // adminIdText = "0"
            //
            // int.TryParse("0",out adminId)
            // => return true, adminId = 0
            //
            // 0 > 0 => false
            //
            //  ===> return true && false = false
            #endregion
        }

        // 查詢目前登入管理員自己的打卡紀錄 API
        //  Get/api/AdminClocks/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMyClockRecords()
        {
            // 如果無法從目前登入Cookie取得管理員ID
            #region 401 和 403 的差別
            //
            // 401 Unauthorized
            // 不知道你是誰 或 你的登入身分無效
            //
            // 403 Forbidden
            // 知道你是誰,但是你沒有執行這個功能的權限
            #endregion
            if (!TryGetCurrentAdminId(out int adminId))
            {
                // 401 無法確認你是誰，或目前的登入身分無效
                return Unauthorized(new { message = "無法取得目前登入的管理員資料" });
            }

            #region  從目前登入Cookie取得管理員ID => true
            //
            // 每一筆為AdminClock
            // => 只查詢不追蹤
            // => .Where()：篩選資料 =>  資料表裡的AdminId == 目前登入的管理員Id
            // => .Select()：轉換資料 => AdminClock -> 轉換成 AdminClockDto
            // ===> records裡, 每一筆都是AdminClockDto
            #endregion
            List<AdminClockDto> records = await _dbcontext
                .AdminClocks.AsNoTracking()
                .Where(clock => clock.AdminId == adminId)
                .OrderByDescending(clock => clock.WorkDate)
                .ThenByDescending(clock => clock.ClockInTime)
                .Select(clock => new AdminClockDto
                {
                    // 新建立的 AdminClockDto.Id = 資料庫查到的 AdminClock.Id
                    Id = clock.Id,
                    AdminAccount = clock.Admin.Account,
                    WorkDate = clock.WorkDate,
                    ClockInTime = clock.ClockInTime,
                    ClockOutTime = clock.ClockOutTime,
                })
                // 執行前述的資料庫查詢方式後,最後轉成一個清單
                .ToListAsync();
            // 回傳 HTTP 200，並將打卡紀錄清單交給前端
            return Ok(new { records });
            // return
            // {
            //   "records": [
            //     {
            //       "id": 1,
            //       "adminAccount": "admin",
            //       "workDate": "2026-10-15",
            //       "clockInTime": "2026-10-15T08:55:00",
            //       "clockOutTime": null
            //     }
            //   ]
            // }
        }

        // 上班打卡API
        // 正式打卡：Vue → ClockIn() → 存入 MySQL
        //                         → LineMessageService → LINE
        [HttpPost("clock-in")]
        public async Task<IActionResult> ClockIn(
            LineMessageService service,
            IHttpClientFactory clients,
            IConfiguration config
        )
        // clients 取得發送 HTTP 的工具
        //
        // IConfiguration config 是用來讀後端已載入的設定：
        // secret.json : "LineBot:ChannelAccessToken"、"LineBot:UserId"
        {
            // 如果無法從目前登入Cookie取得管理員ID
            if (!TryGetCurrentAdminId(out int adminId))
            {
                return StatusCode(401, new { message = "無法取得目前登入的管理員資料" });
            }
            // 取得目前日期與時間
            // 2026-10-15 08:35:42
            DateTime now = DateTime.Now;

            // 判斷上班時間
            // 0900以前不能打卡
            // TimeOfDay 是 C# DateTime 的屬性，用來取得「當天的時間部分」，不包含日期。
            // TimeSpan(時,分,秒) =>new TimeSpan(9, 0, 0) 建立一段「9 小時、0 分鐘、0 秒」的時間長度。
            if (now.TimeOfDay < new TimeSpan(9, 0, 0))
            {
                return BadRequest(new { message = "上班時間打卡不能早於9:00,請於9:00在打卡" });
            }

            // DateOnly → 只保存日期的型別
            // FromDateTime → 從 DateTime 建立 DateOnly 的方法名稱
            //
            // DateOnly today = 2026-10-15
            DateOnly today = DateOnly.FromDateTime(now);

            // 重複打卡檢查：檢查今天是否已經有打卡紀錄
            // 條件：
            // AdminClock裡的AdminId == 目前登入管理員ID
            // &&
            // AdminClock裡的 DateOnly WorkDate == DateOnly today
            bool alreadyClockedIn = await _dbcontext.AdminClocks.AnyAsync(clock =>
                clock.AdminId == adminId && clock.WorkDate == today
            );
            // 今天打卡過了，不能再新增第二筆
            if (alreadyClockedIn)
            {
                // 409 Conflict 目前資料狀態產生衝突
                return Conflict(new { message = "今天已經完成上班打卡" });
            }

            // 建立一筆新的上班打卡資料
            AdminClock adminClock = new AdminClock
            {
                AdminId = adminId,
                WorkDate = today,
                ClockInTime = now,
                ClockOutTime = null,
            };

            // 告訴EF Core這是一筆準備新增的資料
            await _dbcontext.AdminClocks.AddAsync(adminClock);
            // 真正寫入 MySQL
            await _dbcontext.SaveChangesAsync();

            // 從Cookie取得目前管理員帳號
            // AdminClockDto裡有帳號欄位,但是前面Code沒有同時取得：adminAccount,所以才在這裡取得adminAccount
            string adminAccount = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

            string? token = config["LineBot:ChannelAccessToken"];

            // 測試「每次都傳給同一個 LINE 帳號」
            // 它存的是一個固定的 LINE 收件人，例如你的個人 LINE
            // string? userId = config["LineBot:UserId"];

            // 從資料庫找登入者的LinUserId
            // SingleOrDefaultAsync() 在這裡的意思是「執行查詢，取出唯一一筆的 LINE User ID。查不到就給 null。」
            string? userId = await _dbcontext
                .Users.Where(user => user.Id == adminId)
                .Select(user => user.LineUserId)
                .SingleOrDefaultAsync();

            // token、userId 有值
            if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(userId))
            {
                // clients 是工廠，`CreateClient()` 取得真正能發 HTTP 的 client
                using var client = clients.CreateClient();
                string message = $"上班打卡成功：{now:HH:mm}";

                try
                {
                    bool sent = await service.SendLineMessageAsync(client, token, userId, message);

                    // 「LINE 有回應，但回的是失敗嗎？」
                    if (!sent)
                        Console.WriteLine("LINE 未接受上班打卡通知。");
                }
                catch (HttpRequestException) //網路連線失敗
                {
                    Console.WriteLine("無法連線到 LINE，打卡紀錄已儲存。");
                }
                catch (TaskCanceledException) //等待 LINE 回應逾時，或請求被取消
                {
                    Console.WriteLine("LINE 通知等待逾時或已取消，打卡紀錄已儲存。");
                }
            }

            // 新增完成的Entity 轉換成回傳用DTO
            AdminClockDto record = new AdminClockDto
            {
                Id = adminClock.Id,
                AdminAccount = adminAccount,
                WorkDate = adminClock.WorkDate,
                ClockInTime = adminClock.ClockInTime,
                ClockOutTime = adminClock.ClockOutTime,
            };

            return Ok(new { message = "上班打卡成功", record });
        }

        // 下班打卡API
        [HttpPost("clock-out")]
        public async Task<IActionResult> ClockOut(
            LineMessageService service,
            IHttpClientFactory clients,
            IConfiguration config
        )
        {
            // 如果無法從目前登入Cookie取得管理員ID
            if (!TryGetCurrentAdminId(out int adminId))
            {
                return StatusCode(401, new { message = "無法取得目前登入的管理員資料" });
            }

            // 取得目前日期與時間
            DateTime now = DateTime.Now;
            DateOnly today = DateOnly.FromDateTime(now);

            // 查詢目前管理員今天的打卡紀錄
            // .SingleOrDefaultAsync(...) 符合條件的資料最多只能有一筆
            // 2筆以上 => throw InvalidOperationException
            // 1筆 => true => return AdminClock 物件
            // 0筆 => false => null
            //
            // 條件：
            // AdminClock裡的AdminId == 目前登入管理員ID
            // &&
            // AdminClock裡的 DateOnly WorkDate == DateOnly today
            AdminClock? adminClock = await _dbcontext.AdminClocks.SingleOrDefaultAsync(clock =>
                clock.AdminId == adminId && clock.WorkDate == today
            );

            // 今天尚未上班打卡
            if (adminClock is null)
            {
                return BadRequest(new { message = "今天尚未進行上班打卡" });
            }

            // 今天上班打卡了
            // 今天已經完成下班打卡
            if (adminClock.ClockOutTime is not null)
            {
                return Conflict(new { message = "今天已經完成下班打卡" });
            }

            // 今天已經上班打卡，但尚未下班打卡
            // 修改今天這筆紀錄的下班時間
            adminClock.ClockOutTime = now;

            // 將修改寫入 MySQL
            await _dbcontext.SaveChangesAsync();

            // 從 Cookie 取得目前管理員帳號
            string adminAccount = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

            string? token = config["LineBot:ChannelAccessToken"];

            // 測試「每次都傳給同一個 LINE 帳號」
            // 它存的是一個固定的 LINE 收件人，例如你的個人 LINE
            // string? userId = config["LineBot:UserId"];

            // 從資料庫找登入者的LinUserId
            // SingleOrDefaultAsync() 在這裡的意思是「執行查詢，取出唯一一筆的 LINE User ID。查不到就給 null。」
            string? userId = await _dbcontext
                .Users.Where(user => user.Id == adminId)
                .Select(user => user.LineUserId)
                .SingleOrDefaultAsync();

            // token、userId 有值
            if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(userId))
            {
                // clients 是工廠，`CreateClient()` 取得真正能發 HTTP 的 client
                using var client = clients.CreateClient();
                string message = $"上班打卡成功：{now:HH:mm}";

                try
                {
                    bool sent = await service.SendLineMessageAsync(client, token, userId, message);

                    // 「LINE 有回應，但回的是失敗嗎？」
                    if (!sent)
                        Console.WriteLine("LINE 未接受上班打卡通知。");
                }
                catch (HttpRequestException) //網路連線失敗
                {
                    Console.WriteLine("無法連線到 LINE，打卡紀錄已儲存。");
                }
                catch (TaskCanceledException) //等待 LINE 回應逾時，或請求被取消
                {
                    Console.WriteLine("LINE 通知等待逾時或已取消，打卡紀錄已儲存。");
                }
            }

            // 將修改完成的 Entity 轉成回傳用 DTO
            AdminClockDto record = new AdminClockDto
            {
                Id = adminClock.Id,
                AdminAccount = adminAccount,
                WorkDate = adminClock.WorkDate,
                ClockInTime = adminClock.ClockInTime,
                ClockOutTime = adminClock.ClockOutTime,
            };

            return Ok(new { message = "下班打卡成功", record });
        }

        // 今日狀態 API
        // GET/api/AdminClocks/today
        // 查詢目前登入管理員今天的打卡狀態
        // 前端開啟打卡頁面時，必須知道目前是哪種狀態：
        // 狀態一：今天尚未上班 -> 上班按鈕可按 、下班按鈕不可按
        // 狀態二：今天已上班，但尚未下班 -> 上班不可按、下班可按
        // 狀態三：今天上下班都完成 -> 上班不可按 、下班不可按
        [HttpGet("today")]
        public async Task<IActionResult> GetTodayClockStatus()
        {
            // 如果無法從目前登入Cookie取得管理員ID
            if (!TryGetCurrentAdminId(out int adminId))
            {
                return StatusCode(401, new { message = "無法取得目前登入的管理員資料" });
            }

            // 取得今天日期
            DateTime now = DateTime.Now;
            DateOnly today = DateOnly.FromDateTime(now);

            // 查詢"目前管理員"今天的打卡紀錄
            #region 為什麼是 AdminClockDto?
            //問號表示今天的紀錄可能存在，也可能不存在
            //如果今天已上班打卡：record = 一筆 AdminClockDto
            //如果今天尚未打卡：record = null
            #endregion
            AdminClockDto? record = await _dbcontext
                .AdminClocks.AsNoTracking()
                .Where(clock => clock.AdminId == adminId && clock.WorkDate == today)
                .Select(clock => new AdminClockDto
                {
                    Id = clock.Id,
                    AdminAccount = clock.Admin.Account,
                    WorkDate = clock.WorkDate,
                    ClockInTime = clock.ClockInTime,
                    ClockOutTime = clock.ClockOutTime,
                })
                .SingleOrDefaultAsync();
            // record 查詢結果 有1筆 => return record
            // 0筆 => return null
            // 2筆以上 => throw 例外

            // 今天是否已上班打卡
            // record not null = record 有資料 -> true
            // record null = record 無資料 -> false
            bool hasClockedIn = record is not null;

            // 今天是否已經下班打卡
            // record null -> 程式不會繼續讀取 ClockOutTime，結果直接視為沒有下班時間
            bool hasClockedOut = record?.ClockOutTime is not null;

            // 沒有上班紀錄 -> 可以上班打卡
            bool canClockIn = record is null;

            // 有上班紀錄,但無下班紀錄時 -> 可以下班打卡
            bool canClockOut = record is not null && record.ClockOutTime is null;

            return Ok(
                new
                {
                    workDate = today,
                    hasClockedIn,
                    hasClockedOut,
                    canClockIn,
                    canClockOut,
                    record,
                }
            );
        }

        // 查詢所有管理員的打卡紀錄
        //
        // 這支 API 與自己的紀錄[HttpGet("me")] 差別只有查詢範圍:
        // .Where(clock => clock.AdminId == adminId)
        //
        // Get/api/AdminClocks/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAllClockRecords()
        {
            List<AdminClockDto> records = await _dbcontext
                .AdminClocks.AsNoTracking()
                .OrderByDescending(clock => clock.WorkDate)
                .ThenByDescending(clock => clock.ClockInTime)
                .Select(clock => new AdminClockDto
                {
                    Id = clock.Id,
                    AdminAccount = clock.Admin.Account,
                    WorkDate = clock.WorkDate,
                    ClockInTime = clock.ClockInTime,
                    ClockOutTime = clock.ClockOutTime,
                })
                .ToListAsync();

            return Ok(new { records });
        }
    }
}
