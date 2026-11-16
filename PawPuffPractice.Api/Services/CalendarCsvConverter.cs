using System.Globalization;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using PawPuffPractice.Api.Models.Dtos;

namespace PawPuffPractice.Api.Services;

// 關於 CSV 內容能否正確轉換，放 Converter
//
// 把政府的 CSV 日曆，轉成 C# 日曆物件。
// Converter 負責讀取並驗證 CSV，回傳整年度的 CalendarYearDto。
// 例如 CSV 的一列：20270101,五,2,開國紀念日
// 會轉成一個 CalendarDayDto：
// Date = "20270101"
// Week = "五"
// IsHoliday = true  // CSV 的放假代碼 2 轉成布林值 true
// Description = "開國紀念日"
// 每一天的物件都會加入 CalendarYearDto.Days。

// 讀取 CSV，把每一列整理成 C# 物件，最後回傳整年的 CalendarYearDto
public class CalendarCsvConverter
{
    // 讀取資料方法，return「年度日曆物件」
    // 一般方法,回傳型別 CalendarYearDto
    // CalendarYearDto 方法執行後回傳的資料型別
    // 提供資料：CSV檔案位置：csvPath ,年份：year
    #region  建構子
    // 名稱與類別完全相同，而且沒有回傳型別，連 void 都不寫
    // public CalendarCsvConverter()
    // {
    //   建立物件時執行
    // }
    //
    // 什麼時候需要自己寫建構子？
    // 當物件一建立，就必須取得資料或工具、完成初始化時。
    // 例如轉換器一定要知道 CSV 路徑：
    // public class CalendarCsvConverter
    // {
    //     private readonly string _csvPath;

    //     public CalendarCsvConverter(string csvPath)
    //     {
    //         _csvPath = csvPath;
    //     }
    // }
    //
    // 當你寫：
    // var converter = new CalendarCsvConverter("calendar.csv");
    //
    // 就會執行同名的建構子：
    // public CalendarCsvConverter(string csvPath)
    // {
    //     _csvPath = csvPath;
    // }
    //
    // ===> _csvPath = "calendar.csv" ;
    //
    //
    #endregion
    public async Task<CalendarYearDto> ReadAsync(string csvPath, int year)
    { //檔案在哪裡？
        //哪一年？
        // 完成後交回什麼？

        #region 如果CSV是UTF-8
        // 第一步：先取得檔案文字
        // File.ReadAllText() 是「讀取整個文字檔，把內容回傳成一個字串」
        // File.ReadAllText(要讀取的檔案位置,用 UTF-8 解讀)
        // 假設 CSV 裡面是：
        // 日期,名稱
        // 2026-01-01,元旦
        // 讀取後，csvText 就保存這些文字，包括換行。
        // string csvText = File.ReadAllText(csvPath, Encoding.UTF8);
        #endregion
        #region  如果CSV是Big5,「幫 .NET 加裝辨識 Big5 文字的能力。」
        // 「CSV 是 Big5，我要用 Big5 讀它」
        // 1. 真正要做的事：讀檔
        // var text = File.ReadAllText(csvPath, Encoding.GetEncoding(950));
        // 讀取 csvPath 的檔案，用 **Big5（代碼 950）**解讀。
        // 但目前的 .NET 環境可能尚未支援這個編碼，因此需要先登記額外支援。
        //
        // 2. 取得提供額外編碼的物件
        // var provider = CodePagesEncodingProvider.Instance;
        // 取得 .NET 提供的「Big5 等編碼支援物件」，叫它 provider
        //
        // 3. 把這個物件交給 Encoding
        // Encoding.RegisterProvider(provider);
        // Encoding，請登記這個 provider，之後查找編碼時也使用它。
        //
        // 2、3 步合併：
        // Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        #endregion
        // 建立「CSV 解析工具」
        // using var 離開目前程式區塊時，自動釋放工具、關閉檔案
        // new TextFieldParser(...) 建立文字欄位解析工具
        // Encoding：處理文字編碼的類別
        // GetEncoding(...)：取得指定的編碼。
        // 950：Big5 的編碼代碼
        // Encoding.GetEncoding(950) 是取得「Big5 編碼規則」
        // using var parser = new TextFieldParser(csvPath, Encoding.GetEncoding(950));

        // 非同步取得檔案文字
        // File.ReadAllTextAsync(...) 是 .NET 提供的「非同步讀取整個文字檔」方法。
        // (檔案位置, 文字編碼) 讀哪個檔案、用哪種編碼
        // 目前來源檔案混用 UTF-8 與 Big5：2026 是 UTF-8，2027 是 Big5。
        // 先用「遇到錯誤就拋出例外」的 UTF-8 解碼；確認不是 UTF-8 才退回 Big5。
        string csvText;
        try
        {
            csvText = await File.ReadAllTextAsync(csvPath, new UTF8Encoding(false, true));
        }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            csvText = await File.ReadAllTextAsync(csvPath, Encoding.GetEncoding(950));
        }

        // StringReader 和 TextFieldParser 是 .NET 提供的類別
        // StringReader：讀取字串內容的工具
        // 讓它從 csvText 這份文字開始讀
        using var reader = new StringReader(csvText);
        // TextFieldParser：按照規則切分欄位的工具
        // 讓它從 reader 取得文字。設定用逗號分欄後：parser.SetDelimiters(",");
        // 它就能把：20270101,五,2,開國紀念日
        // 讀成四個欄位："20270101"  "五"  "2"  "開國紀念日"
        using var parser = new TextFieldParser(reader);

        // 「用哪種分欄方式」、「用什麼符號分欄」、「是否辨認雙引號」
        //
        // 使用「分隔符號」來區分欄位。下一行再指定分隔符號是什麼。
        // .TextFieldType：這個讀取器的「欄位切分方式」屬性
        // FieldType.Delimited：代表「依照分隔符號切分」，不是每欄固定幾個字。
        //  ===> 將讀取器設定為「分隔符號模式」
        parser.TextFieldType = FieldType.Delimited;

        // .SetDelimiters(...)：呼叫設定分隔符號的方法。
        // 指定用","分欄
        // 例如：20270101,五,2,開國紀念日
        //  ===> 告訴讀取器，分隔符號使用逗號
        parser.SetDelimiters(",");

        // .HasFieldsEnclosedInQuotes：是否按照「欄位可被雙引號包住」的規則讀取
        // 允許欄位用雙引號包住，引號裡的逗號不算分欄。
        // 20270101,五,2,"開國紀念日"
        // ===> 遇到雙引號包住的欄位，要辨認它的範圍
        // 例如 "開國紀念日" 讀成 開國紀念日；如果引號內有逗號，也不會被拆成另一欄。不代表每個欄位都必須有引號。
        parser.HasFieldsEnclosedInQuotes = true;

        var calendar = new CalendarYearDto { Year = year, Source = "行政院人事行政總處" };

        // 讀取器原本停在檔案開頭，必須先讀過標題，才能往下一筆移動
        parser.ReadFields(); // 跳過標題列,不保存它的回傳結果
        // CSV 第一列是欄位名稱：
        // 西元日期,星期,是否放假,備註
        // 20270101,五,2,開國紀念日

        // 現在輪到 20270101,五,2,開國紀念日
        // string[]? fields = parser.ReadFields();
        // ① 讀取 20270101,五,2,開國紀念日 這一筆
        // ② 把這筆欄位回傳，交給 fields 保存
        // ③ 讀取位置往後移，下次才讀 20270102,六,2

        // EndOfData 表示 已經沒有資料可讀了嗎？，結果是 true 或 false
        // 沒有資料 EndOfData = true -> !true = false
        // 有資料 EndOfData = false -> !false = true

        // HashSet<DateOnly> 是保存「不重複日期」的集合。
        // 這行只建立空集合
        var dates = new HashSet<DateOnly>();

        while (!parser.EndOfData)
        {
            string[]? fields = parser.ReadFields();

            if (fields is null)
                continue;

            if (fields.Length != 4)
            {
                // throw 是拋出程式錯誤；
                throw new FormatException("日曆資料必須有4個欄位");
                // Exception vs FormatException
                // FormatException 更明確地表示：「資料格式不符合預期」
                // FormatException 本身也是 Exception 的一種
            }

            // fields[0]、fields[1]、fields[2]、fields[3]
            // 20270101,五,2,開國紀念日
            if (fields[2] != "0" && fields[2] != "2")
            {
                throw new FormatException("放假代碼必須是0或2");
            }

            // 反例：假設有人把 2028.csv 改名成 2027.csv
            // 查詢年份：2027
            // 檔案內日期：20280101
            // 因為CalendarController.cs 是以檔名、位置查找日曆 !!!
            //
            // fields[0]、fields[1]、fields[2]、fields[3]
            // 20270101,五,2,開國紀念日
            //
            // 這段是在檢查：「CSV 第一欄是不是有效的八碼日期？」
            // TryParseExact：依指定格式嘗試解析日期
            // 要驗證哪個值？ fields[0] ex:20270101
            // 要求它符合什麼格式？ "yyyyMMdd"
            // CultureInfo.InvariantCulture 固定日期解讀規則，不跟著電腦地區變動
            // DateTimeStyles.None 不啟用額外的解析選項；例如不特別允許前後空白。
            //
            // out DateOnly date：解析成功時，把日期存到 date
            //
            // 例如："20270101" -> TryParseExact return true -> !true = false -> 不進if
            // 就能使用：date.Year、date.Month、date.Day
            //
            // 例如："20270230" -> TryParseExact return false -> !false = true
            //  -> throw "無效的日曆日期：20280230“
            //

            if (
                !DateOnly.TryParseExact(
                    fields[0],
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateOnly date
                )
            )
            {
                throw new FormatException($"無效的日曆日期：{fields[0]}");
            }
            // 檢查CSV裡的日期屬於指定年份嗎？
            // date.Year != year：比較「資料內年份」與「呼叫者指定年份」，不同就停止轉換。
            if (date.Year != year)
            {
                throw new FormatException($"日期{fields[0]}不屬於{year}年");
            }

            // 檢查日期重複性
            // dates 是 HashSet<DateOnly> ,HashSet 為不重複集合
            // HashSet.Add() 的設計，是除了加入資料，還告訴你「這次有沒有成功加入」。
            //
            // List.Add(value) ->加入值，允許重複 ->  void，不回傳結果
            // HashSet.Add(value) -> 加入值，不保存重複值 -> return bool，是否新增成功
            //
            // 第一次加入 2027/01/01 -> true -> !true = false
            // 第二次加入 2027/01/01 -> false -> !false = true -> throw "日曆日期重複：20270101"
            if (!dates.Add(date))
            {
                throw new FormatException($"日曆日期重複：{fields[0]}");
            }

            // calendar.Days.Add(day);
            // calendar：前面建立的年度物件
            // var calendar = new CalendarYearDto { Year = year, Source = "行政院人事行政總處" };
            // .Days：年度物件裡的每日資料清單 CalendarYearDto裡的List<CalendarDayDto> Days
            // .Add(day)：將剛建立的這一天加入清單
            calendar.Days.Add(
                new CalendarDayDto
                {
                    Date = fields[0],
                    Week = fields[1],

                    // 第三欄是 "2" → 比較結果 true  → IsHoliday = true（放假）
                    // 第三欄是 "0" → 比較結果 false → IsHoliday = false（上班日）
                    IsHoliday = fields[2] == "2",
                    Description = fields[3],
                }
            );
        } // while 到這裡才結束，全部日期已讀完

        // 檢查整年有沒有缺天的?
        // 一般年份：365 天
        // 閏年：366 天
        // DateTime.IsLeapYear() 是 .NET 內建的方法
        // 但「閏年規則」不是 .NET 自己規定的；它按照公曆規則判斷
        // 公曆規則：
        // 能被 400 整除 → 閏年
        // else if 能被 100 整除 → 不是閏年
        // else 能被 4 整除 → 閏年
        // 例如： 2027年
        // 2027 / 400 = x
        // 2027 / 100 = x
        // 2027 / 4 = x
        // 2027 -> 365
        // expectedDays = 365
        // dates.Count -> 計算HashSet dates 有幾個 -> 假如有366個
        // if(366 != 365) -> throw 2027年應有365天,實際有366天
        int expectedDays = DateTime.IsLeapYear(year) ? 366 : 365;

        if (dates.Count != expectedDays)
        {
            throw new FormatException($"{year}年應有{expectedDays}天,實際有{dates.Count}天");
        }

        return calendar;
        // 型別：CalendarYearDto
        // 回傳的是整年度物件：裡面包含 Year、Source、Days
    }
}
