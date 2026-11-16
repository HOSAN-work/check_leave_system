using Humanizer;

namespace PawPuffPractice.Api.Models.Dtos;

public class CalendarYearDto
{
    public int Year { get; set; }
    public string Source { get; set; } = string.Empty;
    public List<CalendarDayDto> Days { get; set; } = new();
    // 每日清單：每個元素是一個 CalendarDayDto，代表一天。
    // [] 裡面放多筆資料，每個 {} 代表一天
}

// 命名方式 camelCase
//「小駝峰命名」：第一個單字小寫，後面的單字首字母大寫。
// C#         camelCase
// Year         year
// Source       source
// IsHoliday    isHoliday
//
// 序列化，就是把程式物件轉成可以儲存或傳送的文字
// C# 裡先有一個物件：
// var day = new CalendarDayDto
// {
//     Date = "20270101",
//     Week = "五",
//     IsHoliday = true,
//     Description = "開國紀念日"
// };
// 把它「序列化成 JSON」:
// JSON 的格式規定：欄位名稱必須用雙引號 "" 包住。
// {
//   "date": "20270101",
//   "week": "五",
//   "isHoliday": true,
//   "description": "開國紀念日"
// }
//
// ASP.NET Core 的 API 預設通常採用 camelCase
//
// json 就會顯示
// {
//   "year": 2027,
//   "source": "行政院人事行政總處",
//   "days": [
//     {
//       "date": "20270101",
//       "week": "五",
//       "isHoliday": true,
//       "description": "開國紀念日"
//     },
//     {
//       "date": "20270102",
//       "week": "六",
//       "isHoliday": true,
//       "description": ""
//     }
//   ]
// }
//
//
//
//
// }
