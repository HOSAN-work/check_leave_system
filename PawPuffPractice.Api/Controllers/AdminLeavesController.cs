using System.Globalization;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Build.Tasks;
using Microsoft.EntityFrameworkCore;
using PawPuffPractice.Api.Data;
using PawPuffPractice.Api.Hubs;
using PawPuffPractice.Api.Models.Dtos;
using PawPuffPractice.Api.Models.EfModels;
using PawPuffPractice.Api.Services;

namespace PawPuffPractice.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminLeavesController : ControllerBase
    {
        private readonly PawPuffPracticeMySQLDbContext _dbcontext;
        private readonly IWebHostEnvironment _environment; //加入環境欄位

        // 宣告一個工具，讓後端可以透過 SignalR 發送請假通知
        private readonly IHubContext<AdminLeaveHub> _leaveHub;

        // ASP.NET Core 建立 Controller 時，會提供這兩個工具：
        // - dbcontext：讀寫假單。
        // - leaveHub：向已連線的前端發通知。
        public AdminLeavesController(
            PawPuffPracticeMySQLDbContext dbcontext,
            IHubContext<AdminLeaveHub> leaveHub,
            IWebHostEnvironment environment
        )
        {
            _dbcontext = dbcontext;
            _leaveHub = leaveHub;
            _environment = environment;
        }

        // 「通知假單已變動」
        // 通知失敗不改變已完成的資料庫儲存結果
        private async Task NotifyLeavesChangeAsync()
        {
            try
            {
                // 等待後端把「假單有變動」通知，發給所有連到這個 Hub 的用戶。
                // Clients：要通知哪些連線用戶？ 這裡的用戶是 SignalR 連線
                // All：選擇所有已連線的用戶。「所有連到 AdminLeaveHub 的用戶」
                await _leaveHub.Clients.All.SendAsync("LeavesChanged");
                // SendAsync(...)：非同步發送通知。
                // "LeavesChanged"：自己取的通知名稱
                // 後端發送：SendAsync("LeavesChanged")
                // 前端接收：leaveConnection.on('LeavesChanged', refreshLeaveData)
                // 假單儲存成功後，後端通知各畫面「資料變了，請重新查詢」，前端收到才執行 refreshLeaveData()
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"請假已儲存,但及時通知失敗：{ex}");
                // $"..."：允許在文字裡插入變數
                // {ex}：把例外 ex 轉成文字
                //
                //
                // Console.WriteLine("一般訊息");
                // 標準輸出，適合一般訊息
                //
                // Console.Error.WriteLine("錯誤訊息");
                // 標準錯誤輸出，適合錯誤紀錄，方便終端機或日誌工具分開處理
                // ----------------------------------
                // Console.WriteLine("你好", "小明");
                // 顯示：你好
                // 因為文字裡沒有指定位置，"小明" 不會被加上去
                //
                // Console.WriteLine("你好，{0}", "小明");
                // 顯示：你好，小明
                // {0} 表示：放入後面的第一個值
                //
                // Console.Error.WriteLine("通知失敗：{0}", ex);
                //
                // JavaScript／Vue -> catch (error) -> console.error(...)
                // C#／Controller  -> catch (Exception ex) -> console.WriteLine(...)
            }
        }

        //「起訖都必須是工作日」的規則：
        // 週六單日 → 不可送出。
        // 週五到週日 → 不可送出，因為結束日是假日。
        // 週五到下週一 → 可以送出，中間六、日不計時數。

        //「開始日、結束日都不能是假日」的共用驗證方法
        // 開始日是假日嗎？
        // 結束日是假日嗎？
        //
        // private，是因為 ValidateLeaveCalendarAsync() 只是這個 Controller 內部的驗證方法，不是要開放給前端呼叫的 API。
        // IActionResult? 的意思是：可能回傳一個 HTTP 回應，也可能回傳 null
        private async Task<IActionResult?> ValidateLeaveCalendarAsync(
            DateTime startTime,
            DateTime endTime
        )
        {
            var service = new CalendarFileService(); //「按年份讀日曆」工具
            // CultureInfo.InvariantCulture：用固定的格式規則，不受伺服器地區設定影響。
            string startDate = startTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string endDate = endTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            // startTime、endTime -> DateTime，可以直接取 .Year
            // startDate、endDate -> string，例如 "20271231"
            // 目的是把假單碰到的每個年度都走一遍，因為你的日曆是一年一份 CSV。
            for (int year = startTime.Year; year <= endTime.Year; year++)
            {
                // 某一年讀日曆存入calendar物件
                var calendar = await service.LoadYearAsync(_environment.ContentRootPath, year);

                #region calendar is null 404 vs 503
                // 使用者在做什麼？
                // GET /api/calendar/2026 我要查 2026 年日曆
                //
                // 找不到日曆時，為什麼這樣回？ -> 404：你要查的日曆沒有提供。
                // CalendarController.cs
                // 日曆查詢 API
                // [HttpGet("{year:int}")]
                // if (calendar is null)
                // {
                //     return NotFound(new{...})
                // }
                //
                //
                // 使用者在做什麼？
                // POST /api/AdminLeaves/submit 我要送出假單
                //
                // 找不到日曆時，為什麼這樣回？
                // -> 503：日曆是後端拿來驗證假單的資料。缺了它，後端目前不能安全地完成送出。
                // -> 表示「暫時無法驗證」的回應
                //
                // if (calendar is null)
                // {
                //     return StatusCode(503,new {
                // message = $"缺少{year}年行事曆,暫時無法驗證假單"
                // }
                // );
                #endregion

                // 503 Service Unavailable 用在「請求沒有填錯，但伺服器現在暫時無法處理，之後可以再試」的情況。
                // 常見例子是伺服器正在維護、暫時過載，或這個功能依賴的服務暫時離線。不必整個網站都停擺
                if (calendar is null)
                {
                    return StatusCode(
                        503,
                        new { message = $"缺少{year}年行事曆,暫時無法驗證假單" }
                    );
                }

                // Any() -> true or false
                // AnyAsync() -> true or false
                // Days 裡有沒有至少一筆，日期是 startDate，而且是假日？
                // 例如：
                // 找到 true && 不是假日 false => false
                // 找到 true && 是假日 true => true => "請假開始日不能是假日"
                if (calendar.Days.Any(day => day.Date == startDate && day.IsHoliday))
                {
                    return BadRequest(new { message = "請假開始日不能是假日" });
                }
                // 找到 true && 是假日 true => true => "請假結束日不能是假日"
                if (calendar.Days.Any(day => day.Date == endDate && day.IsHoliday))
                {
                    return BadRequest(new { message = "請假結束日不能是假日" });
                }
            }

            return null;
        }

        // 「送出請假申請」API
        // 誰呼叫？ -> Vue 透過 HTTP 呼叫
        [HttpPost("submit")]
        public async Task<IActionResult> CreateLeave(CreateAdminLeaveRequestDto request)
        {
            // 從Cookie驗證取得Claim的Id且預設為string, 存入adminInText
            string? adminIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // adminIdText
            // -轉換成Int-> 可以轉換的話-> true , 且輸出adminId -> !true=false , 輸出adminId
            // -轉換成Int-> 不可以轉換的話-> false , 且輸出adminId=null -> !false=true,輸出adminId=null
            //
            // ！int.TryParse()-> true => true || xxxx = true => "無法取得管理員身份"
            // ！int.TryParse()=true or false || adminId<=0 = true => "無法取得管理員身份"
            if (!int.TryParse(adminIdText, out int adminId) || adminId <= 0)
            {
                return Unauthorized(new { message = "無法取得管理員身份" });
            }

            #region  .HasValue 確認時間存在 與 .Value 才取出實際時間
            //
            // request.StartTime.HasValue = true -> 有StartTime值
            // ！true = false
            // request.EndTime.HasValue = true -> 有EndTime值
            // ！true = false
            //  false || false => 不會進if
            // --------------------------------
            // request.StartTime.HasValue = true -> 有StartTime值
            // ！true = false
            // request.EndTime.HasValue = false -> 沒有EndTime值
            // ！false = true
            // false || true => "請填寫請假起訖時間"
            #endregion
            if (!request.StartTime.HasValue || !request.EndTime.HasValue)
            {
                return BadRequest(new { message = "請填寫請假起訖時間" });
            }

            DateTime startTime = request.StartTime.Value;
            DateTime endTime = request.EndTime.Value;

            if (endTime <= startTime)
            {
                return BadRequest(new { message = "結束時間必須晚於開始時間" });
            }

            // 最早上班時間為 09:00，請假開始時間不可更早
            // if (startTime.TimeOfDay < new TimeSpan(9, 0, 0))
            // {
            //     return BadRequest(new { message = "請假開始時間不能早於09:00" });
            // }

            // 依班別決定每日工作起訖時間
            TimeSpan shiftStart =
                request.ClassType == 1 ? new TimeSpan(9, 0, 0) : new TimeSpan(10, 0, 0);

            TimeSpan shiftEnd =
                request.ClassType == 1 ? new TimeSpan(18, 0, 0) : new TimeSpan(19, 0, 0);

            // 請假開始時間必須在班別開始之後、結束之前
            // 只想檢查「幾點」，所以用 .TimeOfDay
            if (startTime.TimeOfDay < shiftStart || startTime.TimeOfDay >= shiftEnd)
            {
                return BadRequest(new { message = "請假開始時間必須在所選班別的工作時間內" });
            }

            if (endTime.TimeOfDay <= shiftStart || endTime.TimeOfDay > shiftEnd)
            {
                return BadRequest(new { message = "請假結束必須在所選班別的工作時間內" });
            }

            // 同一天，而且整段都落在午休內，不需要請假
            if (
                startTime.Date == endTime.Date
                && startTime.TimeOfDay >= new TimeSpan(12, 0, 0)
                && endTime.TimeOfDay <= new TimeSpan(13, 0, 0)
            )
            {
                return BadRequest(new { message = "此時段為午休時間,不需要申請請假" });
            }
            // 驗證請假開始、請假結束不能為假日 -> calendarError HTTP物件
            var calendarError = await ValidateLeaveCalendarAsync(startTime, endTime);
            if (calendarError is not null)
            {
                return calendarError;
            }

            #region ---- 檢查重疊，是為了避免同一位管理員對同一段時間重複請假 ----
            // 例如你已送出：10/21 09:00～12:00　病假　待審核
            // 又送出： 10/21 10:00～11:00　事假
            //
            //
            // AnyAsync() 檢查同一人的待審核或已核准申請是否重疊。
            // 條件：
            // 1. 既有假單的管理員 ID == 目前登入的管理員 ID
            // 2. !leave.IsDeleted = true -> 未刪除、保留
            // 3. 既有假單是「待審核(1)」或「已核准(2)」
            // 4. 既有假單的開始時間 < 這次申請的結束時間
            // 5. 既有假單的結束時間 > 這次申請的開始時間
            #endregion
            bool hasOverlap = await _dbcontext.AdminLeaves.AnyAsync(leave =>
                leave.AdminId == adminId
                && !leave.IsDeleted
                && (leave.ApprovalStatus == 1 || leave.ApprovalStatus == 2)
                && leave.StartTime < endTime
                && leave.EndTime > startTime
            );
            #region  先列出「沒重疊」的情況，只有兩種。
            // 舊假單在新申請之前結束：
            // 舊：08:00──09:00
            // 新：10:00──12:00
            // => leave.EndTime <= startTime
            //
            // 舊假單在新申請之後才開始：
            // 新：10:00──12:00
            // 舊：13:00──14:00
            // => leave.StartTime >= endTime
            //
            // 假設這次要請 10:00～12:00
            // 1. 既有假單是 08:00～09:00：已經結束，不重疊。
            // leave.EndTime > startTime // 09:00 > 10:00 → false
            //  => 「我開始請假時，舊假單還沒結束嗎？」-> 沒有，所以排除。
            //
            // 2. 既有假單是 13:00～14:00：還沒開始，不重疊。
            // leave.StartTime < endTime  // 13:00 < 12:00 → false
            //  => 「我結束請假之前，舊假單已經開始了嗎？」沒有，所以排除。
            #endregion

            // 如果舊假單與準備申請的時間 => 時間重疊
            if (hasOverlap)
            {
                return Conflict(new { message = "此時段已有待審核或已核准的請假申請" });
            }

            // 建立一筆假單：使用類別名稱
            AdminLeaf leave = new AdminLeaf
            {
                AdminId = adminId,
                LeaveType = request.LeaveType,
                ClassType = request.ClassType,
                StartTime = startTime,
                EndTime = endTime,
                Reason = request.Reason.Trim(),
                ApprovalStatus = 1, //待審核
            };

            //  PawPuffPracticeMySQLDbContext.cs =>
            //   public virtual DbSet<AdminLeaf> AdminLeaves { get; set; }
            await _dbcontext.AdminLeaves.AddAsync(leave);
            await _dbcontext.SaveChangesAsync();
            await NotifyLeavesChangeAsync();

            // 回覆前端：「請假申請已儲存成功」，並附上這張假單的編號。
            return Ok(new { message = "請假申請已送出,等待審核", id = leave.Id });
        }

        // 「查詢自己的請假紀錄」API
        // 誰呼叫？ -> Vue 透過 HTTP 呼叫
        [HttpGet("me")]
        public async Task<IActionResult> GetMyLeaves()
        {
            string? adminIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(adminIdText, out int adminId) || adminId <= 0)
            {
                return Unauthorized(new { message = "無法取得管理員身份" });
            }

            // AsNoTracking() 是告訴 EF Core：「這次查出的資料只拿來讀，不用記錄它之後有沒有被修改。」跟 GET、POST 無關。
            // 篩選條件：請假紀錄leave.AdminId == Claim的adminId
            // 以leave.id遞減排序
            // 保留：leave.Id,leave.LeaveType,leave.StartTime,...
            var records = await _dbcontext
                .AdminLeaves.AsNoTracking()
                // 假單所屬的管理員ID == 目前登入者的ID && IsDeleted = false -> 未刪除 => !false = true ===> 保留
                .Where(leave => leave.AdminId == adminId && !leave.IsDeleted)
                .OrderByDescending(leave => leave.Id)
                .Select(leave => new
                {
                    leave.Id,
                    leave.LeaveType,
                    leave.ClassType,
                    leave.StartTime,
                    leave.EndTime,
                    leave.Reason,
                    leave.ApprovalStatus,
                    leave.RejectionReason,
                })
                .ToListAsync();

            return Ok(new { records });
        }

        // 軟刪除 -> 只能刪除自己"待審核"假單 API
        // Delete api/AdminLeaves/id:xxx
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteLeave(int id)
        {
            string? adminIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(adminIdText, out int adminId) || adminId <= 0)
            {
                return Unauthorized(new { message = "無法取得管理員身份" });
            }

            AdminLeaf? leave = await _dbcontext.AdminLeaves.SingleOrDefaultAsync(leave =>
                leave.Id == id && leave.AdminId == adminId && !leave.IsDeleted
            );
            // 至少找到一筆假單：
            // leave.Id == id -> 是你指定的假單編號
            // leave.AdminId == adminId -> 屬於目前登入的管理員
            // ！leave.IsDeleted -> 尚未被刪除

            if (leave is null)
            {
                return NotFound(new { message = "找不到這筆請假紀錄" });
            }

            // ApprovalStatus=1 待審核
            if (leave.ApprovalStatus != 1)
            {
                return Conflict(new { message = "目前只允許刪除待審核假單" });
            }

            leave.IsDeleted = true; // 刪除
            leave.DeletedAt = DateTime.Now; //現在時間

            await _dbcontext.SaveChangesAsync();
            await NotifyLeavesChangeAsync();

            return Ok(new { message = "請假紀錄已刪除" });
        }

        // 管理員可以修改自己的待審核假單 API
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateLeave(int id, CreateAdminLeaveRequestDto request)
        {
            string? adminIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(adminIdText, out int adminId) || adminId <= 0)
            {
                return Unauthorized(new { message = "無法取得管理員身份" });
            }

            // 至少找到一筆
            // 條件：你要刪除的假單編號 && 目前登入者自己的假單 && 這筆還沒有被刪除
            // 假設要刪除假單編號7：管理員ID=1且尚未刪除
            AdminLeaf? leave = await _dbcontext.AdminLeaves.SingleOrDefaultAsync(item =>
                item.Id == id && item.AdminId == adminId && !item.IsDeleted
            );

            if (leave is null)
            {
                return NotFound(new { message = "找不到這筆請假紀錄" });
            }
            if (leave.ApprovalStatus != 1)
            {
                return Conflict(new { message = "目前只允許修改待審核假單" });
            }

            if (!request.StartTime.HasValue || !request.EndTime.HasValue)
            {
                return BadRequest(new { message = "請填寫起訖時間" });
            }

            // 起訖時間
            DateTime startTime = request.StartTime.Value;
            DateTime endTime = request.EndTime.Value;

            if (endTime <= startTime)
            {
                return BadRequest(new { message = "結束時間必須晚於開始時間" });
            }

            // 最早上班時間為 09:00，請假開始時間不可更早
            // if (startTime.TimeOfDay < new TimeSpan(9, 0, 0))
            // {
            //     return BadRequest(new { message = "請假開始時間不能早於09:00" });
            // }

            // 依班別決定每日工作起訖時間
            TimeSpan shiftStart =
                request.ClassType == 1 ? new TimeSpan(9, 0, 0) : new TimeSpan(10, 0, 0);

            TimeSpan shiftEnd =
                request.ClassType == 1 ? new TimeSpan(18, 0, 0) : new TimeSpan(19, 0, 0);

            // 請假開始時間必須在班別開始之後、結束之前
            // 只想檢查「幾點」，所以用 .TimeOfDay
            if (startTime.TimeOfDay < shiftStart || startTime.TimeOfDay >= shiftEnd)
            {
                return BadRequest(new { message = "請假開始時間必須在所選班別的工作時間內" });
            }

            if (endTime.TimeOfDay <= shiftStart || endTime.TimeOfDay > shiftEnd)
            {
                return BadRequest(new { message = "請假結束時間必須在所選班別的工作時間內" });
            }

            // 同一天，而且整段都落在午休內，不需要請假
            if (
                startTime.Date == endTime.Date
                && startTime.TimeOfDay >= new TimeSpan(12, 0, 0)
                && endTime.TimeOfDay <= new TimeSpan(13, 0, 0)
            )
            {
                return BadRequest(new { message = "此時段為午休時間,不需要申請請假" });
            }

            // 驗證請假開始、請假結束不能為假日 -> calendarError HTTP物件
            var calendarError = await ValidateLeaveCalendarAsync(startTime, endTime);
            if (calendarError is not null)
            {
                return calendarError;
            }

            // 檢查「新時間是否與自己的其他假單重疊」
            // 不是正在修改的假單
            // && 屬於目前管理員ID
            // && 尚未刪除
            // && 審核狀態=1(待審核)或2(已核准)
            // && 檢查時間是否重疊(Db裡開始時間 StartTime < 前端送來的新結束時間 endTime )
            // && 檢查時間是否重疊(Db裡結束時間 EndTime > 前端送來的新開始時間 startTime )
            bool hasOverlap = await _dbcontext.AdminLeaves.AnyAsync(item =>
                item.Id != id
                && item.AdminId == adminId
                && !item.IsDeleted
                && (item.ApprovalStatus == 1 || item.ApprovalStatus == 2)
                && item.StartTime < endTime
                && item.EndTime > startTime
            // item.StartTime、item.EndTime 來自資料庫 AdminLeaves 裡已經存在的假單
            // startTime、endTime 前端這次送來、準備修改的新時間
            );

            if (hasOverlap)
            {
                return Conflict(new { message = "此時段與其他假單重疊" });
            }

            leave.LeaveType = request.LeaveType;
            leave.ClassType = request.ClassType;
            leave.StartTime = startTime;
            leave.EndTime = endTime;
            leave.Reason = request.Reason.Trim();

            await _dbcontext.SaveChangesAsync();
            await NotifyLeavesChangeAsync();

            return Ok(new { message = "請假申請已修改" });
        }

        // 送出新申請 -> POST -> 新增一筆假單
        // 修改假單 -> PUT -> 更新假單
        // 審核假單 -> PUT  -> 更新假單的審核狀態

        // 審核假單 API
        // 管理員可以互審核假單、但是禁止審核自己的假單
        [HttpPut("{id:int}/review")]
        public async Task<IActionResult> ReviewLeave(int id, ReviewAdminLeaveRequestDto request)
        {
            // 1.誰在操作？——權限
            string? adminIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(adminIdText, out int adminId) || adminId <= 0)
            {
                return Unauthorized(new { message = "無法取得管理員身份" });
            }
            // 2.操作哪筆資料？——對象
            #region 例如： 前端傳入int id
            // |
            // v
            // 例如：item.Id == 7 && !item.IsDeleted
            // 前端提供：id = 7
            // 資料庫提供：item.Id、item.IsDeleted 等假單欄位
            #endregion
            AdminLeaf? leave = await _dbcontext.AdminLeaves.SingleOrDefaultAsync(item =>
                item.Id == id && !item.IsDeleted
            );
            // -> 用了 SingleOrDefaultAsync()，就要想到「可能找不到」=> return null
            if (leave is null)
            {
                return NotFound(new { message = "找不到這筆假單" });
            }
            // 3.現在是什麼狀態？——允許的變化
            // Dto只允許ApprovalStatus=2,3
            // 2:已核准 、3:已駁回
            // 不允許什麼？ => if(ApprovalStatus!= 1){...}
            // 反例：2.審核完成後,再次審核已核准的假單
            if (leave.ApprovalStatus != 1)
            {
                return Conflict(new { message = "只能審核「待審核」假單" });
            }
            // 反例：1.審核自己的假單
            if (leave.AdminId == adminId)
            {
                return StatusCode(403, new { message = "不能審核自己的假單" });
            }
            // 反例：3.從前端傳來的 request.ApprovalStatus=非2,3
            if (request.ApprovalStatus != 2 && request.ApprovalStatus != 3)
            {
                return BadRequest(new { message = "審核結果只能是核准或駁回" });
            }
            // 反例： 狀態＝ 駁回 && 駁回原因= null or " " =>  BadRequest
            if (request.ApprovalStatus == 3 && string.IsNullOrWhiteSpace(request.RejectionReason))
            {
                return BadRequest(new { message = "駁回時請填寫原因" });
            }
            // 4.操作後應改變什麼？——結果
            // 從前端傳入的request.ApprovalStatus => 準備存入leave.ApprovalStatus
            // => leave.ApprovalStatus改變
            leave.ApprovalStatus = request.ApprovalStatus;

            // 狀態＝駁回 ？ 如果前端有駁回原因 : null
            leave.RejectionReason =
                request.ApprovalStatus == 3 ? request.RejectionReason?.Trim() : null;

            await _dbcontext.SaveChangesAsync(); //資料庫資料改變->儲存
            await NotifyLeavesChangeAsync();

            return Ok(new { message = "假單審核完成" });
            // 5.拿反例挑戰規則——補漏洞
            #region 規則
            // 先把審核規則寫成中文，再把「不允許的情況」逐一轉成 if。全部通過後，才修改資料。
            // 規則怎麼想：
            // 1.誰在操作？——權限
            // |
            // v
            // [Authorize(Roles="Admin")] => 限制管理員使用
            // string? adminIdText = ...... => 取得「現在登入的是誰」
            // if(..){...} => 比較申請人與登入者，落實「禁止自審」
            // -----------------------
            // 2.操作哪筆資料？——對象
            // |
            // v
            // AdminLeaf? leave = await .....加上條件 => 找到符合條件的假單
            // -----------------------
            // 3.現在是什麼狀態？——允許的變化
            //
            // -> 待審核=1狀態的假單才能審核
            // |
            // v
            // 所以 if(狀態!=1的假單) => 不能審核 => return Conflict(new{...})
            //
            // Dto:
            // 待審核 1 只能審核兩種狀態
            // → 已核准 2
            // → 已駁回 3
            // -----------------------
            // 4.操作後應改變什麼？——結果
            //
            // |
            // v
            // 審核後,改變的是審核狀態=2 or 3
            // leave.ApprovalStatus = request.ApprovalStatus;
            //
            // await .....SaveChangeAsync();
            // 回覆前端：HTTP 200 OK
            //
            // | 處理結果    | 回傳程式                 | HTTP 狀態 |
            // |---         |---                     |---|
            // | 審核成功    | `Ok(...)`              | 200 |
            // | 審自己的假單 | `StatusCode(403, ...)` | 403 |
            // | 找不到假單   | `NotFound(...)`        | 404 |
            // | 假單已審過   | `Conflict(...)`        | 409 |
            // -----------------------
            // 5.拿反例挑戰規則——補漏洞
            //
            // | 反例              | 對應防護 |
            // |---               |---|
            // | 非管理員送出審核   | `[Authorize]` |
            // | 審不存在的假單    | `leave is null` |
            // | 審自己的假單      | `leave.AdminId == adminId` |
            // | 再次審核已核准假單 | `ApprovalStatus != 1` |
            // | 傳入狀態 `99`     | DTO 的 `[Range(2, 3)]` |
            #endregion
            #region 1.審核自己的假單
            // if (leave.AdminId == adminId)
            // {
            //     return StatusCode(403, new { message = "不能審核自己的假單" });
            // }
            // 2.審核完成後,再次審核已核准的假單
            // 3. 後端透過 DTO 的 [Range(2, 3)] 驗證審核結果。
            // 前端即使送入 ApprovalStatus = 99，也會因驗證失敗而回傳 HTTP 400
            #endregion
        }

        // 查待審核清單  GetPendingReviewLeaves() -> 查多筆，不指定一張
        // 審核某張假單  ReviewLeave(int id, …) -> 必須指定要審哪張
        // 修改某張假單 UpdateLeave(int id, …) -> 必須指定要改哪張

        // 查詢「可由我審核的假單」，取得其他管理員的待審核申請 API
        [HttpGet("pending-review")]
        public async Task<IActionResult> GetPendingReviewLeave()
        {
            // 從Cookie取得管理員ID
            string? adminIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // 如果找不到管理員ID
            if (!int.TryParse(adminIdText, out int adminId) || adminId <= 0)
            {
                return Unauthorized(new { message = "無法取得管理員身份" });
            }

            // AnyAsync() 只回答「有沒有」，不會回傳假單內容
            //
            // 查出來只拿來看、不會修改 => AsNoTracking()
            // Where()篩選條件：
            // 1. 假單AdminId != 目前登入的管理員ID
            // 2. leave.IsDeleted 預設0 -> 尚未刪除 => !leave.IsDeleted -> 已刪除
            // 3. leave.ApprovalStatus =1
            //
            // == 是比較；= 是賦值
            //
            // Select 保留欄位
            //
            // AdminAccount = leave.Admin.Account
            // -> AdminLeaves table 的 AdminId fk-> Users 的Id
            // Admin 這個名稱通常是 EF 反向工程產生模型時，根據外鍵 AdminId 推導出來的
            // 產生模型的工具通常會把 AdminId 的 Id 去掉，用 Admin 命名導覽屬性
            // ==> 所以可以leave.Admin.Account

            var records = await _dbcontext
                .AdminLeaves.AsNoTracking()
                .Where(leave =>
                    leave.AdminId != adminId && !leave.IsDeleted && leave.ApprovalStatus == 1
                )
                .OrderBy(leave => leave.Id)
                .Select(leave => new
                {
                    leave.Id,
                    AdminAccount = leave.Admin.Account,
                    leave.LeaveType,
                    leave.ClassType,
                    leave.StartTime,
                    leave.EndTime,
                    leave.Reason,
                    leave.ApprovalStatus,
                    // 待審核假單 -> 不需要回傳駁回原因，因為這些假單還沒被審核。
                })
                .ToListAsync();
            // 執行查詢並取得清單。沒有符合資料時，得到空清單

            return Ok(new { records });
        }
    }
}
