namespace PawPuffPractice.Api.Services;

public sealed record AdminStatusOption(sbyte Value, string Label);

public static class AdminStatusDefinitions
{
    // 兩種方式，結果基本相同
    // 1. 使用常數 2.使用數字 => 差別在可讀性
    // 以下是用常數方式
    public const sbyte Active = 1;
    public const sbyte Resigned = 8;
    public const sbyte UnpaidLeave = 9;
    public const sbyte SecurityLocked = 10;

    // IReadOnlyList<AdminStatusOption>
    // 完整意思是：一份唯讀的 AdminStatusOption 清單。
    public static IReadOnlyList<AdminStatusOption> All { get; } =
    [
        new(Active, "正常啟用"),
        new(Resigned, "離職"),
        new(UnpaidLeave, "留職停薪"),
        new(SecurityLocked, "帳號安全鎖定"),
    ];

    // 取得某個狀態的中文文字
    public static string GetText(sbyte status)
    {
        AdminStatusOption? option = All.FirstOrDefault(option => option.Value == status);

        return option?.Label ?? "未知的帳號狀態";
    }

    // 只取得可以用來停用帳號的狀態
    // IEnumerable<T>
    // 可以理解成：我只保證你能一筆一筆讀。
    public static IEnumerable<AdminStatusOption> GetDisabledOptions()
    {
        return All.Where(option => option.Value != Active);
    }

    // 用途是檢查傳入的狀態是否存在於 All
    // .Any() 會逐一檢查清單,只要找到一筆 true，.Any() 就回傳 true
    #region 資料傳遞流程：
    // 假設 Controller 呼叫：
    // AdminStatusDefinition.IsValid(request.Status)
    //
    // request.Status
    //       ↓ 傳入 IsValid()
    //    status
    //       ↓ 拿去和 All 裡每個 Value 比較
    // option.Value == status
    //
    // 例如前端傳來：
    // {
    //   "status": 8
    // }
    //
    //
    // DTO 得到：request.Status == 8
    //
    // 接著執行：
    // AdminStatusDefinition.IsValid(request.Status)
    // 相當於：
    // AdminStatusDefinition.IsValid(8)
    // All.Any(option => option.Value == 8)
    // => All 裡有：new(Resigned, "離職") // Resigned == 8
    // => return true
    #endregion
    public static bool IsValid(sbyte status)
    {
        return All.Any(option => option.Value == status);
    }
    #region  例如：AdminStatusDefinition.IsValid(8)
    // 有沒有任何一個 option 的 Value 等於 status？
    // return All.Any (option => 8 == 8)
    // return true
    //
    // AdminStatusDefinition.IsValid(2)
    // return All.Any(option => 1/8/9/10 == 2)
    // return false
    #endregion
}
