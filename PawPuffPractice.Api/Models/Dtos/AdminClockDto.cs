namespace PawPuffPractice.Api.Models.Dtos;

// 回傳管理員打卡紀錄
// Controller 用這個 DTO 建立 record : Id, AdminAccount, WorkDate,ClockInTime, ClockOutTime ,IsLate , ExpectedClockOutTime
//
// Controller 寫成：
// return Ok(new
// {
//     workDate = today,
//     hasClockedIn,
//     hasClockedOut,
//     canClockIn,
//     canClockOut,
//     record,
// }); ------> 外層
//
// {
//   workDate: '2026-11-02',
//   hasClockedIn: true,
//   canClockIn: false,
//   record: {  ------> 內層
//     clockInTime: '2026-11-02T10:10:00',
//     isLate: false,
//   },
// }

public class AdminClockDto
{
    // 打卡紀錄編號
    public int Id { get; set; }

    // 管理員帳號
    public string AdminAccount { get; set; } = string.Empty;

    // 工作日
    // DateOnly ：年月日
    public DateOnly WorkDate { get; set; }

    // 上班打卡時間
    public DateTime ClockInTime { get; set; }

    // 下班打卡時間
    // 尚未下班時是 null
    public DateTime? ClockOutTime { get; set; }

    // 超過 10:00:00 才算遲到
    // TimeOfDay：取得DateTime裡的時間部分,不包含日期
    //  => :判斷它是否符合條件
    public bool IsLate => ClockInTime.TimeOfDay > new TimeSpan(10, 0, 0);

    // 不遲到：上班時間加 9 小時，包含 1 小時午休
    // 遲到：當天固定 19:00 下班
    // ExpectedClockOutTime 是我們自己取的屬性名稱，意思是「應下班時間」
    public DateTime ExpectedClockOutTime =>
        IsLate ? ClockInTime.Date.AddHours(19) : ClockInTime.AddHours(9);

    // 遲到 IsLate = true ,ClockInTime.Date 先取得日期 ex:11/02 00:00 ,AddHours(19) ex:11/02 19:00
    // 不遲到 IsLate = false , ex:ClockInTime = 11/02 10:11 -AddHours(9)-> 11/02 19:11

    // 依目前規則，這筆紀錄是否早於允許打卡時間
    public bool IsBeforeAllowedTime => ClockInTime.TimeOfDay < new TimeSpan(9, 0, 0);

    // PATCH DTO：
    // public string? Nickname { get; set; }
    // public string? Email { get; set; }
    // public string? Phone { get; set; }
    //
    // 不加 = string.Empty，因為：
    // null → 前端沒有傳這個欄位→ 不修改
    // 有字串 → 前端有傳 → 修改該欄位
}
