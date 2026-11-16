using PawPuffPractice.Api.Models.Dtos;

namespace PawPuffPractice.Api.Services;

// 跟 httpget{year:int} getCalendar (int year) 差在哪？
// 直接呼叫api 不行嗎
// -> 可以直接呼叫 API。 差別是：Vue 或別人的專案要跨程式取得資料，適合呼叫 API
// -> 你的請假功能和日曆功能在同一個 C# 後端，可以直接呼叫共用方法，不必繞一趟 HTTP。
//
// 為什麼請假後端不直接呼叫日曆 API？
// 可以，但它得先送 HTTP 請求，再處理狀態碼、讀取 JSON，才能拿回同一個後端本來就能讀到的資料。直接呼叫 LoadYearAsync()，就能拿到日曆物件，接著做請假判斷。

// 「按年份讀日曆」工具
// 用年份組出檔案路徑。找不到就回 null；找到就呼叫轉換器。
public class CalendarFileService
{
    // `?` 是告訴呼叫者：「等待完成後，可能拿到年度物件，也可能拿到 null。
    // -> 也就是到底有沒有某一年份的csv檔呢
    public async Task<CalendarYearDto?> LoadYearAsync(string ContentRootPath, int year)
    {
        // Path.Combine(...) 把根目錄和 Data/Calendars/2027.csv 接起來。這一步只是組路徑，還沒讀檔。
        string csvPath = Path.Combine(ContentRootPath, "Data", "Calendars", $"{year}.csv");

        // File 是 .NET 提供的檔案操作類別
        // `File.Exists()` 確認地址上有沒有檔案
        if (!File.Exists(csvPath))
            return null;

        // 讀取 CSV，把每一列整理成 C# 物件，最後回傳整年的 CalendarYearDto。
        return await new CalendarCsvConverter().ReadAsync(csvPath, year);
        // 等同於：
        // var converter = new CalendarCsvConverter();
        // var calendar = await converter.ReadAsync(csvPath, year);
        // return calendar;
        //
        //
    }
}
