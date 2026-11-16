// 建立「共用狀態定義檔案」是為了解決：
// 同一組會員狀態數字與文字，目前重複寫在後端、前端和其他程式裡，之後容易不一致
// 例如你目前可能有兩份定義
// 後端 C#：
// return status switch
// {
//     1 => "正常啟用",
//     2 => "會員自行申請停用",
//     3 => "違反使用規範",
//     _ => "未知狀態"
// };
//
// 前端 Vue：
// const statusOptions = [
//   { value: 2, label: '會員自行申請停用' },
//   { value: 3, label: '違反使用規範' }
// ]
//
// 不建立共用檔案會怎樣？
// 假設以後把狀態 3 改成：違反社群規範
// 後端：3 => "違反社群規範"
// 前端忘記修改：
// { value: 3, label: '違反使用規範' }
//
// 特別注意：
// C# 後端和 JavaScript 前端不能直接共用同一個普通程式檔。
// 因此通常是各自集中管理。

#region 這個檔案負責什麼？
// UserStatusOption
// → 代表一個狀態選項
// → 例如 Value = 3、Label = 違反使用規範

// UserStatusDefinitions.All
// → 保存狀態 1～7

// GetText(3)
// → 取得「違反使用規範」

// GetDisabledOptions()
// → 取得狀態 2～7
// → 不包含正常狀態 1
#endregion


namespace PawPuffPractice.Api.Services;

// 代表「一個會員狀態選項」
#region  record 是 C# 的一種型別，主要用來保存「一組資料」
// 例如會員狀態選項需要保存：
// 狀態值：2
// 顯示文字：會員自行申請停用
// 可以定義：
// public record UserStatusOption(
//     sbyte Value,
//     string Label
// );
// 然後建立資料：
// var option = new UserStatusOption(
//     2,
//     "會員自行申請停用"
// );
// 取得內容：
// option.Value  // 2
// option.Label  // "會員自行申請停用"
//
// 不使用 record
// 可以直接用普通 class：
// public sealed class UserStatusOption
// {
//     public sbyte Value { get; }

//     public string Label { get; }

//     public UserStatusOption(sbyte value,string label)
//     {
//         Value = value;
//         Label = label;
//     }
// }
// 建立資料：
// var option = new UserStatusOption(
//     2,
//     "會員自行申請停用"
// );
// 取得資料：
// option.Value
// option.Label
// -------------------
// 使用 record
// public sealed record UserStatusOption(
//     sbyte Value,
//     string Label
// );
// 建立:
// var option = new UserStatusOption(
//     2,
//     "會員自行申請停用"
// );
// 取得:
// option.Value
// option.Label
//
// 差別一：程式碼比較短 =>因此 record 最直觀的優點是少寫很多固定程式碼
// 差別二：比较方式不同
// 建立兩筆內容一樣的資料：
// var first = new UserStatusOption(
//         2,
//         "會員自行申請停用"
//     );

// var second = new UserStatusOption(
//         2,
//         "會員自行申請停用"
//     );
//
// 普通 class -> first == second
// => false  => 因為它們是兩次 new 建立的不同物件
//
// record -> first == second
// => true
// Value 都是 2
// Label 都是會員自行申請停用
// → 兩筆資料相等
//
// 差別三：record 的属性默認不能随意重新指定
// public record UserStatusOption(sbyte Value,string Label);
// 建立後不能在普通程式中寫：
// option.Value = 3;
// option.Label = "違反使用規範";
// => 主要在建立物件時設定，建立完成後不允許一般重新指定。
//
// 差別四：record 支援 with
// var original = new UserStatusOption( 2,"會員自行申請停用");
// 想建立一份新資料，只修改 Label：
// var changed = original with
// {
//     Label = "會員申請停用"
// };
// 結果：
// original
// Value = 2
// Label = 會員自行申請停用
//
// changed
// Value = 2
// Label = 會員申請停用
// 它不是修改 original，而是根據原資料建立另一筆
//
// 差別五：ToString() 顯示比較有用
// 使用 record：Console.WriteLine(option);
// 可能顯示：
// UserStatusOption {
//     Value = 2,
//     Label = 會員自行申請停用
// }
// 普通 class : Console.WriteLine(option);
// 內部會呼叫：option.ToString();
// 普通 class 沒有自己寫 ToString() 時，會使用 object 預設的 ToString()，大致回傳：
// PawPuffPractice.Api.Models.UserStatusOption
// 它確實「轉成字串」了，只是轉出的字串是型別名稱。
//
// 如果 class 沒有 namespace，例如：
// public class UserStatusOption{}
// 則可能只顯示：UserStatusOption
//
// 想顯示屬性，可以直接指定
// Console.WriteLine(option.Value);
// 顯示：2
// Console.WriteLine(option.Label);
// 顯示：會員自行申請停用
//
// 沒有自行處理 ToString() 時，
// 通常會顯示「完整型別名稱」，而不是屬性內容
// 通常只顯示：UserStatusOption
//
// virtual 是什麼？
// virtual 寫在父類別
// : 我先提供一個預設版本，但允許子類別日後改寫。
// 父類別：
// public class GameCharacter
// {
//     public virtual void Attack()
//     {
//         Console.WriteLine("普通攻擊");
//     }
// }
// 表示 Attack() 有預設功能：普通攻擊
// 但是繼承 GameCharacter 的其他 class 可以重新實作
//
//
// override 是什麼？
// 建立戰士：
// public class Warrior : GameCharacter
// {
//     public override void Attack()
//     {
//         Console.WriteLine("使用長劍攻擊");
//     }
// }
//
// 建立魔法師：
// public class Mage : GameCharacter
// {
//     public override void Attack()
//     {
//         Console.WriteLine("發射火球");
//     }
// }
// 這裡的 override 表示改寫從 GameCharacter 繼承來的 Attack()
// 使用：
// var normalCharacter = new GameCharacter();

// var warrior = new Warrior();

// var mage = new Mage();

// normalCharacter.Attack(); //普通攻擊
// warrior.Attack(); //使用長劍攻擊
// mage.Attack(); //發射火球
#endregion
#region  sealed 就是：到這裡為止，不准再被繼承，或不准再繼續改寫。
// 1. sealed class：禁止繼承整個類別
// public sealed class FinalBoss
// {
//     public void Attack()
//     {
//         Console.WriteLine("最終攻擊");
//     }
// }
// 不能再建立它的子類別：
// public class StrongerBoss : FinalBoss
// {
// }
// 上面會編譯失敗，因為：FinalBoss 已封閉，不能被繼承
//
// 但仍然可以正常建立和使用：
// var boss = new FinalBoss();
// boss.Attack();
//
// 2. sealed override：這次可以改寫，但不准下一層再改
// 先建立父類別：
// public class GameCharacter
// {
//     public virtual void Attack()
//     {
//         Console.WriteLine("普通攻擊");
//     }
// }
// 第二層子類別：
// public class Warrior : GameCharacter
// {
//     public sealed override void Attack()
//     {
//         Console.WriteLine("使用長劍攻擊");
//     }
// }
//
// 第三層仍然可以繼承 Warrior：
// public class Knight : Warrior
// {
// }
// 但是不能再改寫,會編譯失敗。
//
// virtual → 父類別說：這個方法可以被改寫
// override → 子類別說：我要改寫父類別的方法
// sealed override → 子類別說：我改寫，但下一層不准再改
// sealed class → 這個類別不准被繼承
//
// 除錯時 record 更容易看到裡面的內容。
#endregion

#region 建構子的用途是：建立物件時，接收初始資料，並把資料放進新物件裡。
// var option = new UserStatusOption(
//     2,
//     "會員自行申請停用"
// );
//
// new UserStatusOption(...)
// 代表建立一個新的 UserStatusOption 物件
// 當你使用 new 時，C# 就會執行這個型別的建構子
//
// public class UserStatusOption
// {
//     public sbyte Value { get; set; }
//
//     public string Label { get; set; }
//
// 這一段就是建構子
//  public UserStatusOption(sbyte value, string label)
//     {
//         Value = value;
//         Label = label;
//     }
// }
// 建構子有什麼特徵？
// class 名稱：UserStatusOption
// 建構子名稱： UserStatusOption 而且建構子沒有回傳型別。
// 執行過程:
// var option = new UserStatusOption(2,"會員自行申請停用");
// 傳入的資料是：
// 2
// 會員自行申請停用
//
// 建構子收到：
// value = 2;
// label = "會員自行申請停用";
// 接著執行：
// Value = value;
// Label = label;
// 最後的新物件是：
// option
// ├── Value = 2
// └── Label = 會員自行申請停用
// 之後可以取得：
// option.Value
// option.Label
//
// 沒有建構子參數時，可能這樣寫：
// var option = new UserStatusOption();
//
// option.Value = 2;
// option.Label = "會員自行申請停用";
//
// 問題是可能忘記指定資料：
// var option = new UserStatusOption();
// option.Value = 2;
//  忘記設定 Label
//
// 使用需要參數的建構子：
// new UserStatusOption(2,"會員自行申請停用");
//  => 建立時就必須提供 Value 和 Label，比較不容易產生缺少資料的物件
#endregion
public sealed record UserStatusOption(sbyte Value, string Label);
#region record 為什麼看不到建構子？
// 括號內的：
// (
//     sbyte Value,
//     string Label
// )
// 就是主要建構子的參數
//
// C# 會自動幫你產生建構子，所以不必自己寫：
// public UserStatusOption(sbyte value,string label)
// {
//     Value = value;
//     Label = label;
// }
// 仍然可以這樣建立物件：
// var option = new UserStatusOption(
//         2,
//         "會員自行申請停用"
//     );
// 執行結果：
// Value = 2
#endregion Label = 會員自行申請停用

// 集中管理所有會員狀態
// 定義整個 class
//
// 為什麼 static class 裡還要寫 static？
// 因為 C# 語法規定：
// static class 裡的成員也必須明確標記為 static。
public static class UserStatusDefinitions
{
    // 兩種方式，結果基本相同
    // 1. 使用常數 2.使用數字
    // 以下是用數字方式
    public const sbyte Active = 1; // 正常啟用的狀態值

    // 這整段是在建立一個「公開、共用、只能取得、保存多筆會員狀態選項」的屬性
    //
    // 先看最簡化版本：
    // public static int Number { get; } = 10;
    // 建立一個名為 Number 的公開共用屬性，型別是 int，初始值是 10，外部只能讀取。
    //
    //
    // IReadOnlyList<...>
    // 代表：一份可以按顺序读取，但不能透過這個介面新增或刪除資料的清單。
    //
    // IReadOnlyList<UserStatusOption>
    // 完整意思是：一份唯讀的 UserStatusOption 清單。
    //
    // 建立一份會員狀態清單，名稱叫做 All，裡面放狀態 1～7
    // 可以改名：Options、AllStatuses
    // 那使用時就變成：UserStatuses.Options
    //
    // 如何取得 All 裡的資料？
    // 假設:
    // public static class UserStatuses
    // {
    //   public static IReadOnlyList<UserStatusOption>
    //   All { get; } =
    //     [
    //         new(1, "正常啟用"),
    //         new(2, "會員自行申請停用"),
    //         new(3, "違反使用規範")
    //     ];
    // }
    //
    // 取得整份清單：var statuses = UserStatuses.All;
    // 取得資料數量：int count = UserStatuses.All.Count; 結果：3
    //
    // 取得第一筆：UserStatusOption first = UserStatuses.All[0];
    // 第一筆內容：
    // first.Value // 1
    // first.Label // "正常啟用"
    //
    // 取得第二筆：UserStatusOption second = UserStatuses.All[1];
    // 第二筆內容：
    // second.Value // 2
    // second.Label // "會員自行申請停用"
    //
    // { get; } 外部可以取得這份清單，但不能把 All 重新指定成另一份清單。
    public static IReadOnlyList<UserStatusOption> All { get; } =
    [
        // new UserStatusOption(1,"正常啟用")
        // 建立一筆資料：
        // Value = 1
        // Label = 正常啟用
        //
        // 底下是簡寫
        //
        // [ ] 代表把多筆資料放在一起 => 表示建立一份集合
        new(1, "正常啟用"),
        // 會員停用選項狀態
        new(2, "會員自行申請停用"),
        new(3, "違反平台使用規範"),
        new(4, "疑似詐騙行為"),
        new(5, "騷擾其他會員"),
        new(6, "發布不當內容"),
        new(7, "帳號安全異常"),
    ];

    // 取得某個狀態的中文文字
    // 傳入會員狀態數字，從 All 清單找到相同的狀態，然後回傳中文文字
    public static string GetText(sbyte status)
    {
        // ？ ：option 可以保存 UserStatusOption，也可以是 null
        //
        // ？. 是在安全存取成員：
        // 如果 option 不是 null，才取得 Label；
        // 如果是 null，直接得到 null。
        UserStatusOption? option = All.FirstOrDefault(option => option.Value == status);

        return option?.Label ?? "未知的帳號狀態";
    }

    // IReadOnlyList<T>
    // 可以理解成：除了能一筆一筆讀，我還保證它是一個有順序、能用索引取得資料、知道數量的唯讀清單。

    // IEnumerable<T>
    // 可以理解成：我只保證你能一筆一筆讀。

    #region  只取得可以用來停用帳號的狀態
    //
    // IEnumerable<T> 可以先理解成：一串可以逐筆列舉、逐筆讀取的资料
    #endregion 從 All 的所有會員狀態中，排除正常啟用狀態，回傳剩下可以用來停用帳號的狀態
    public static IEnumerable<UserStatusOption> GetDisabledOptions()
    {
        return All.Where(option => option.Value != Active);
    }
    #region  假使執行：GetDisabledOptions()
    // 結果會包含：
    // 2，會員自行申請停用
    // 3，違反使用規範
    // 4，停用原因 4
    // ...
    // 不包含：1，正常啟用
    #endregion 用途是檢查傳入的狀態是否存在於 All


    //用途是檢查傳入的狀態是否存在於 All
    public static bool IsValid(sbyte status)
    {
        return All.Any(option => option.Value == status);
    }
}
