namespace PawPuffPractice.Api.Models.Dtos;

// 定義 CSV 轉成日曆資料後，每一天需要哪些欄位
// 西元日期,星期,是否放假,備註
// 20270101,五,2,開國紀念日
// 20270102,六,2,
// 20270103,日,2,
// 20270104,一,0,
// 20270105,二,0,
// 20270106,三,0,
// ...
public class CalendarDayDto
{
    // 日期
    // 星期幾
    // 是否為假日
    // 描述
    public string Date { get; set; } = string.Empty;
    public string Week { get; set; } = string.Empty;
    public bool IsHoliday { get; set; }
    public string Description { get; set; } = string.Empty;
}
