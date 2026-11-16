// 產生會員、管理員假資料
// Nuget套件 ： Bogus
using Bogus;
using Microsoft.EntityFrameworkCore;
using PawPuffPractice.Api.Models.EfModels;

namespace PawPuffPractice.Api.Data;

#region 回傳型別的區別
// | 回傳型別      | 意思 |
// |---           |---|
// | `void`       | 一般方法，不回傳值 |
// | `bool`       | 一般方法，回傳 `true` 或 `false` |
// | `Task`       | 非同步方法，不回傳結果值 |
// | `Task<bool>` | 非同步方法，回傳布林值 |
// | `Task<int>`  | 非同步方法，回傳整數 |
#endregion

// Seeder 可以翻成「種子資料產生器」或「初始資料建立程式」
// 它的工作是：在資料庫裡自動放入一批預先準備好的資料

// Development：開發環境。
// Data：資料。
// Seeder：純粹是因為它負責產生與寫入初始假資料。
// ===> 開發環境使用的假資料建立程式

public static class DevelopmentDataSeeder
{
    // 接收一個資料庫內容物件 dbContext，執行資料初始化工作，而且不回傳結果值
    public static async Task SeedAsync(PawPuffPracticeMySQLDbContext dbContext)
    {
        // 檢查是否已經存在測試會員
        // 查詢資料庫的Users,有沒有任何一筆會員帳號是以test開頭的？
        // AnyAsync()
        // 找到至少一筆：true
        // 一筆都沒有：false
        bool testUsersAlreadyExist = await dbContext.Users.AnyAsync(user =>
            user.Account.StartsWith("test")
        );

        // 找到至少一筆
        // 已經存在測試會員，不再重複新增
        // if (testUsersAlreadyExist)
        // {
        //     return;
        // }
        // 原本：
        // 會員存在 → return → 會員和管理員都不處理
        // 下方優化

        #region  Faker 是 Bogus 套件提供的類別
        // 建立繁體中文假資料產生器，只能單獨產生姓名、電話等資料
        // var faker = new Faker("zh_TW");

        // Faker：一般假資料工具。
        // Faker<User>：專門用來產生 User 物件。
        // faker.IndexFaker：Faker<User> 產生資料時所提供的目前索引值。
        // 它從 0 開始計算：
        // | 第幾位會員 | `IndexFaker` 的值 |
        // |----------|-------------------|
        // | 第 1 位   | `0`               |
        // | 第 2 位   | `1`               |
        // | 第 3 位   | `2`               |
        // | 第 100 位 | `99`              |
        // 但我們希望第一個帳號是：test001
        // 而不是：test000
        // 所以加一：faker.IndexFaker + 1
        // 所以加一 + 三位數：faker.IndexFaker + 1:D3
        // 結果：
        // test001
        // test002
        // test003
        #endregion
        #region RuleFor 是 Bogus 提供的方法
        // 用來設定某個欄位的假資料產生規則
        // .RuleFor(要設定的欄位, 欄位的產生方式)
        //
        // .RuleFor(
        //     user => user.Account,
        //     faker => $"user{faker.IndexFaker + 1:D3}"
        // );
        //
        // 意思是：
        //  User.Account 欄位
        //         ↓
        // 產生 test001、test002……
        #endregion

        // 調整後：
        // 會員存在 → 跳過會員 → 繼續檢查管理員
        // 產生完整的User物件 假資料
        // .RuleFor(要設定哪個欄位,這個欄位的值怎麼產生);
        if (!testUsersAlreadyExist)
        {
            var userFaker = new Faker<User>("zh_TW")
                .RuleFor(
                    user => user.Account,
                    // 根據目前資料的編號，產生test這種帳號
                    f => $"test{f.IndexFaker + 1:D3}"
                )
                .RuleFor(user => user.Nickname, f => f.Name.FullName())
                //王小明、林美玲、陳志豪...
                .RuleFor(user => user.Email, (f, user) => $"{user.Account}@test.com")
                .RuleFor(user => user.Phone, f => f.Phone.PhoneNumber("09########"))
                // 每個 # 都會被替換成一個隨機數字
                .RuleFor(user => user.PointBalance, f => f.Random.Int(0, 10000))
                // 產生 0 到 10000 之間的隨機整數
                .RuleFor(user => user.Status, _ => (sbyte)1)
                // 只回傳固定值 status = 1
                // 因此一般會把沒使用的參數寫成 _
                // _ 可以讀成：Bogus 會給我一個參數，但我不需要使用它，直接回傳 1
                .RuleFor(user => user.Role, _ => "Member")
                .RuleFor(user => user.Biography, f => f.Lorem.Sentence())
                .RuleFor(user => user.CreatedAt, f => f.Date.Past(2))
                // 產生過去兩年內的隨機時間，設定到 CreatedAt
                .RuleFor(user => user.LastLoginAt, f => null)
                // 代表測試會員尚未登入，因此最後登入時間设為 null
                .RuleFor(
                    user => user.PasswordHash,
                    _ => BCrypt.Net.BCrypt.HashPassword("Test1234!")
                );

            // 完成全部規則後，真正產生 100 個 User：
            List<User> testUsers = userFaker.Generate(100);
            // 使用 Bogus 的 userFaker，一次產生 100 筆假的會員資料，並把這 100 筆資料放進 testUsers 清單，還沒有把資料新增到資料庫

            // AddRangeAsync：一次加入多筆資料
            // 主要是讓 EF Core 開始追蹤這 100 個物件，
            // 並把狀態標記為：Added
            // 告訴 EF Core：這100筆資料準備新增
            //
            // 如果只新增一個會員，可以使用：
            // await dbContext.Users.AddAsync(user);
            // 現在要新增 100 個，所以使用：
            // await dbContext.Users.AddRangeAsync(testUsers);
            await dbContext.Users.AddRangeAsync(testUsers);
        }

        // 管理員
        bool testAdminAlreadyExist = await dbContext.Users.AnyAsync(user =>
            user.Account == "admin"
        );

        if (!testAdminAlreadyExist)
        {
            // 建立一個新的 User 物件，並用變數 admin 保存它
            var admin = new User
            {
                Account = "admin",
                Nickname = "系統管理員",
                Email = "admin@test.com",
                Phone = "0900000000",
                PointBalance = 0,
                Status = (sbyte)1,
                Role = "Admin",
                Biography = "系統管理員帳號",
                CreatedAt = DateTime.Now,
                LastLoginAt = null,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234!"),
            };
            await dbContext.Users.AddAsync(admin);
        }
        // 把資料寫入資料庫
        await dbContext.SaveChangesAsync();

        // 產生管理員打卡假資料
        //
        // 取得目前所有管理員
        // 篩選條件：角色=Admin
        // -> 清單
        List<User> admins = await dbContext.Users.Where(user => user.Role == "Admin").ToListAsync();

        // 假資料日期範圍：昨天往前60天
        // DateTime.Today 取得今天日期 ,時間固定是凌晨 00:00:00
        // DateTime.Today.AddDays(-1) 往前減一天，得到昨天
        // DteOnly FromDateTime() => 取得昨天日期
        DateOnly yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

        // yesterday.AddDays(-59) 往前減59天 => 得到查詢範圍的開始日期
        DateOnly startDate = yesterday.AddDays(-59);

        // 取得這段期間已存在的打卡紀錄，避免重複新增
        // 篩選條件：
        // 資料庫裡的工作日 >= 開始日期 && 資料庫裡的工作日 <= 昨天日期
        // -> 清單
        List<AdminClock> existingClockRecords = await dbContext
            .AdminClocks.AsNoTracking()
            .Where(clock => clock.WorkDate >= startDate && clock.WorkDate <= yesterday)
            .ToListAsync();

        // 把資料庫原本已有的打卡紀錄，轉成一組「管理員 ID＋日期」的唯一鍵，方便快速檢查重複資料。
        // HashSet 有兩個重要特性：
        // 1. 不會保存重複值。
        // 2. 使用 Contains() 查詢某個值是否存在通常很快。
        HashSet<string> existingClockKeys = existingClockRecords
            .Select(clock => $"{clock.AdminId}-{clock.WorkDate:yyyy-MM-dd}")
            .ToHashSet();
        // 假設 existingClockRecords 內容是：
        // AdminId = 1，WorkDate = 2026-10-16
        // AdminId = 1，WorkDate = 2026-10-17
        // AdminId = 2，WorkDate = 2026-10-16
        //
        // 逐筆取出 clock，將它轉成字串：
        // "1-2026-10-16"
        // "1-2026-10-17"
        // "2-2026-10-16"
        //
        // .ToHashSet() : 將這些字串收集成 HashSet<string>
        // {
        //     "1-2026-10-16",
        //     "1-2026-10-17",
        //     "2-2026-10-16",
        // }
        //

        // 建立假資料產生器
        // Faker 來自 Bogus 套件，用來產生測試用假資料。
        var clockFaker = new Faker("zh_TW");
        // 建立一個只能存放 AdminClock 物件的空清單
        var fakeClockRecords = new List<AdminClock>();

        // 對每一位管理員，逐日建立最近 60 天內的假打卡紀錄；略過週末及資料庫已經存在的紀錄。
        // 第一層迴圈：逐一處理管理員
        foreach (User admin in admins)
        {
            // 第二層迴圈：處理最近 60 天
            // i 依序是：59、58、57……2、1、0 => 總共執行 60 次
            for (int i = 59; i >= 0; i--)
            {
                // 假設：yesterday = 2026-10-17
                // loop 1：
                // daysAgo = 59
                // yesterday.AddDays(-59) -> workDate = 2026-08-19
                //
                // loop 2：
                // daysAgo = 58
                // yesterday.AddDays(-58) -> workDate = 2026-08-20
                //
                // ...
                // loop 60：
                // daysAgo = 0
                // yesterday.AddDays(-0) -> workDate = yesterday
                //
                // 因此整個迴圈產生的日期順序是：
                // 最早日期 → …… → 前天 → 昨天
                // 也就是最近 60 天，而且不包含今天。

                DateOnly workDate = yesterday.AddDays(-i);

                // 週休二日不產生上班紀錄
                // workDate.DayOfWeek : .NET 會根據日期計算星期幾
                //
                // 例如：DateOnly workDate = new DateOnly(2026, 10, 17);
                // Console.WriteLine(workDate.DayOfWeek);
                // ===> DayOfWeek.Saturday
                //
                // 從 workDate 取得那一天是星期幾，再由你寫的條件決定星期六、星期日不產生紀錄。
                if (
                    workDate.DayOfWeek == DayOfWeek.Saturday
                    || workDate.DayOfWeek == DayOfWeek.Sunday
                )
                {
                    continue;
                    // 跳過這一天，不建立打卡資料。
                }

                string recordKey = $"{admin.Id}-{workDate:yyyy-MM-dd}";
                // 假設：
                // admin.Id = 5;
                // workDate = new DateOnly(2026, 10, 16);
                //
                // recordKey = 5-2026-10-16

                if (existingClockKeys.Contains(recordKey))
                {
                    // 同一位管理員、同一天已有紀錄時跳過
                    continue;
                }

                // 上班時間：08:30～09:30
                DateTime clockInTime = workDate
                    .ToDateTime(new TimeOnly(8, 30))
                    .AddMinutes(clockFaker.Random.Int(0, 60));
                // 建立 08:30
                // new TimeOnly(8, 30)
                //
                // 日期和時間合併 -> 使用 ToDateTime() 合併
                // workDate.ToDateTime(new TimeOnly(8, 30))
                // 假設：workDate = 2026-10-16
                // 合併後：2026-10-16 08:30:00
                //
                // 隨機增加分鐘 : 隨機產生 0～60 的整數
                // clockFaker.Random.Int(0, 60)
                //
                // .AddMinutes(...) 加到 08:30 上。
                // 例如隨機結果是：8  → 08:38

                // 下班時間：17:30～19:00
                DateTime clockOutTime = workDate
                    .ToDateTime(new TimeOnly(17, 30))
                    .AddMinutes(clockFaker.Random.Int(0, 90));

                // 建立一筆假的打卡紀錄，加入 fakeClockRecords 清單。
                fakeClockRecords.Add(
                    new AdminClock
                    {
                        AdminId = admin.Id,
                        WorkDate = workDate,
                        ClockInTime = clockInTime,

                        // 每 9 筆保留一筆未下班，測試畫面顯示
                        // % 取餘數
                        // 判斷i是否能被 9 整除
                        // 例如：
                        // 18 % 9 = 0 → true -> null
                        // 10 % 9 = 1 → false -> clockOutTime
                        // 9  % 9 = 0 → true -> null
                        // 8  % 9 = 8 → false -> clockOutTime
                        // 0  % 9 = 0 → true -> null
                        ClockOutTime = i % 9 == 0 ? null : clockOutTime,
                    }
                // 假設目前資料是：
                // admin.Id   = 3
                // workDate   = 2026-10-16
                // clockInTime = 2026-10-16 08:45
                //
                // 就會建立：
                // 管理員：3
                // 工作日期：2026-10-16
                // 上班時間：08:45
                );
            }
        }

        // var fakeClockRecords = new List<AdminClock>();
        // => List<T> 取得資料筆數的屬性是： .Count
        // | 資料型別      | 取得數量 |
        // |---           |---      |
        // | `List<T>`    | `.Count` |
        // | `HashSet<T>` | `.Count` |
        // | `Dictionary<TKey, TValue>` | `.Count` |
        // | 陣列 `T[]`    | `.Length` |
        // | `string`      | `.Length` |

        // 如果真的有產生假打卡紀錄，就一次加入資料庫並儲存。
        if (fakeClockRecords.Count > 0)
        {
            // 把清單中的多筆 AdminClock 交給 EF Core 追蹤，標示成「準備新增」。
            // 單筆 → Add() 或 AddAsync()
            // 多筆 → AddRange() 或 AddRangeAsync()
            await dbContext.AdminClocks.AddRangeAsync(fakeClockRecords);

            await dbContext.SaveChangesAsync();
        }
    }
}
