using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Tasks;
using PawPuffPractice.Api.Services;
using Pomelo.EntityFrameworkCore.MySql.Storage.Internal;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous] //這支 API「不用登入也能呼叫」
    public class CalendarController : ControllerBase
    {
        // 取得「後端專案根目錄」的工具，讓 API 能找到你的 CSV
        // 1.先想：CSV 放在哪裡？
        // 後端根目錄/Data/Calendars/2027.csv
        // 框架提供一個能回答這個問題的物件
        // 型別：IWebHostEnvironment
        // 取得它之後，可以用：environment.ContentRootPath //查出應用程式的內容根目錄
        //
        // 但「後端根目錄」部署到別人的主機後可能不同，所以需要向執行環境取得。
        // 2.IWebHostEnvironment 提供環境資訊
        private readonly IWebHostEnvironment _environment;

        //「錯誤紀錄」 --> 讓你知道 CSV 究竟哪裡出錯
        // 使用者 → 看固定錯誤訊息：格式錯誤時，使用者收到：行事曆資料格式有誤，請聯絡維護者
        // 維護者 → 從伺服器紀錄看詳細原因
        // ILogger 是框架提供的紀錄工具
        // <CalendarController> 是告訴記錄工具：「這些紀錄屬於 CalendarController」
        private readonly ILogger<CalendarController> _logger;

        // 先準備一個欄位(自取名：_environment)，讓 Controller 能保存它。

        // 3.建構子接收工具，再保存起來
        // 框架建立 Controller 時，從這裡把環境物件傳進來。
        // 框架看的是 IWebHostEnvironment 型別，才知道要傳入環境物件；
        public CalendarController(
            IWebHostEnvironment environment,
            ILogger<CalendarController> logger
        )
        {
            _environment = environment;
            _logger = logger;
        }

        // 同步是正常的執行方式；非同步主要用來處理等待
        // 1. 資料已經在記憶體裡，就直接同步處理
        // 2. 要等待外部資源，才考慮非同步
        //
        // 3. 即使是讀檔，同步仍有用途
        // 例如一次性的 CSV 轉換小工具：使用者只要「讀完 → 轉換 → 結束」，沒有同時服務其他請求的需求，同步寫法可以很直接。
        // 有些工具也只提供同步介面，例如我們原本使用的TextFieldParser.ReadFields()
        // 等待 CSV 檔案讀取 → 非同步
        // 拆欄位、檢查代碼、建立 DTO → 同步

        // 多人同時使用 API 時，非同步通常有助於避免大量執行緒卡在等待，不代表單次查詢一定更快。
        // 多人呼叫 → 優先採用非同步 I/O；不是所有程式碼都必須非同步。
        // 非同步讀 CSV → 同步解析 → Controller 回傳 JSON

        // 接收年份
        // → 找到對應 CSV
        // → 找不到就回傳 404
        // → 找到就呼叫 Read()
        // → 回傳年度資料

        // 非同步：等待資料庫查詢、等待其他 API 回應、等待檔案讀取
        // 同步：將放假代碼 2 轉成 true
        // 「是否需要等待外部資源？→ 工具有沒有非同步版本？→ 再決定是否用 await／`async`。」
        // 日曆查詢 API
        [HttpGet("{year:int}")]
        public async Task<IActionResult> GetCalendar(int year)
        {
            // 檢查使用者傳入的年份是有否為效年份？
            if (year < 1 || year > 9999)
            {
                return BadRequest(new { message = "請提供有效年份" });
            }
            // 要讀哪個檔案？
            // 使用者查詢：GET /api/calendar/2027
            // 方法收到：year = 2027;
            // 後端根目錄 -> Data -> Calendars -> 2027.csv

            // 「組出 CSV 的完整檔案路徑」
            // _environment.ContentRootPath :取得這個後端應用程式的內容根目錄
            // ex: /Users/hosan/Desktop/vue_api_練習重新寫/PawPuffPractice.Api
            // 內容根目錄/Data/Calendars/2027.csv
            // string csvPath = Path.Combine(
            //     _environment.ContentRootPath,
            //     "Data",
            //     "Calendars",
            //     $"{year}.csv"
            // );

            // 檔案位置是否存在？
            // System.IO.File 是 .NET 處理檔案的類別；
            // `Exists()` 是檢查檔案是否存在的方法；
            // `csvPath` 是要檢查的位置。
            // if (!System.IO.File.Exists(csvPath))
            // {
            //     return NotFound(new { message = "尚未提供該年度行事曆" });
            // }

            // 建立「CSV 讀取工具」
            // var converter = new CalendarCsvConverter();

            // 使用者要求 2027，沒問題
            // → 但我們提供的 CSV 日期錯誤
            // → 是伺服器資料問題
            // → 回傳 500，不是 400
            //
            // HTTP 400：使用者送來的資料不符合要求
            // HTTP 500：伺服器處理時發生內部問題
            try
            {
                // 建立CalendarFileService物件
                var calendarFileService = new CalendarFileService();
                // 呼叫calendarFileService物件的LoadYearAsync -> 回傳 calendar物件
                var calendar = await calendarFileService.LoadYearAsync(
                    _environment.ContentRootPath,
                    year
                );
                // 404
                if (calendar is null)
                {
                    return NotFound(new { message = "尚未提供該年度行事曆" });
                }
                // 200
                return Ok(calendar);
            }
            // 如果裡面要用 _logger.LogError(error, ...)，就需要接住例外並取名字
            catch (FormatException error) //catch(error) 是寫在vue <script setup> -> JavaScript寫法
            {
                // 把「錯誤詳細內容」和「查詢哪一年」一起寫進紀錄
                // 呼叫傳入三個引數：例外物件、訊息模板、年份。
                // catch 的括號是在指定捕捉的例外型別；`LogError(error, ...)` 則需要實際例外物件
                _logger.LogError(error, "讀取{Year}年行事曆時發生格式錯誤", year);
                // C# 代入：使用實際變數 year
                // _logger.LogError(error, $"讀取{year}年行事曆時發生格式錯誤");
                //
                // Logger 代入：Year 是空位名稱，year 提供值
                // _logger.LogError(error, "讀取{Year}年行事曆時發生格式錯誤", year);
                // 多保留一個「年份欄位」。以後使用支援這種紀錄的工具，就能篩選「Year 等於 2027」，不必分析整句文字。
                // {Year} 是欄位名；後面的 year 按位置提供值，不靠同名配對。
                // Logger 就把 year 的值填進第一個空位{Year}。

                return StatusCode(500, new { message = "行事曆資料格式有誤，請聯絡維護者" });
            }
            #region static
            // 當一個方法不需要使用「某個物件自己保存的資料」，可以考慮用 static。不是因為不想寫 new() 才用它。
            //
            // 1. 需要物件自己的資料 → 一般方法 -> 要寫new xxx()
            // 先建立一個物件
            // var converter = new CalendarCsvConverter();
            //
            //  再透過同一個物件呼叫方法
            // var calendar2027 = converter.Read(path2027, 2027);
            // var calendar2028 = converter.Read(path2028, 2028);
            //
            // 2. 所需資料都從參數傳入 → 可以考慮 static
            //
            // 例如你的 Controller：
            // _environment.ContentRootPath
            // 需要使用這個 Controller 保存的 _environment，因此使用它的方法不能直接改成 static。
            #endregion
        }
    }
}
