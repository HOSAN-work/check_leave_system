//var builder = ... → 建立設定工具
using System.Security.Claims;
using LineBot_Class_Library;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PawPuffPractice.Api.Data;
using PawPuffPractice.Api.Hubs;
using PawPuffPractice.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    //builder.Configuration → 讀取設定
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("找不到 ConnectionStrings:DefaultConnection");

// builder.Services → 註冊程式需要的服務
builder.Services.AddDbContext<PawPuffPracticeMySQLDbContext>(
    // ASP.NET Core 提供一個 DbContext 設定物件給我們，我們暫時把它叫作 options
    options =>
        //這個 DbContext 使用 MySQL
        //表示使用連線字串連接 MySQL，並自動判斷 MySQL Server 的版本
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// Add services to the container.

//builder.Services.Add... → 登記所需服務
//表示把 Controller 功能加入 ASP.NET Core
builder.Services.AddControllers();

// 註冊 LINE Bot 設定的讀取方式
//
// Configure<LineBotOptions> 註冊對應方式：
// LineBot:ChannelSecret 會對應到 ChannelSecret
// LineBot:AccessToken 會對應到 AccessToken
//
// GetSection("LineBot") 取得 API 設定中名為 LineBot 的區塊
builder.Services.Configure<LineBotOptions>(builder.Configuration.GetSection("LineBot"));

// 註冊 SignalR 需要的服務，放在 builder.Build() 之前。
builder.Services.AddSignalR();

builder
    .Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "PawPuff.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // 會員攜帶 Cookie 呼叫 API
        // → Cookie Authentication 解讀 Cookie
        // → 執行 OnValidatePrincipal
        // → 從 Cookie 取得會員 Id
        // → 查詢 MySQL 最新 Status
        // → Status = 1：繼續使用 API
        // → Status != 1：拒絕 Cookie 並登出
        //
        // options.Events
        // options 是 Cookie Authentication 的設定物件
        // Events 代表 Cookie 驗證過程中會發生的各種事件，
        // 例如：收到登入Cookie、驗證Cookie、登入成功、登出、拒絕Cookie
        // OnValidatePrincipal 代表：ASP.NET Core 已經讀取Cookie，準備確認這個登入身分是否仍然有效時，執行這段程式。
        // Principal 指目前Cookie裡面的登入者身分，也就是ClaimsPrincipal。
        // 例如 Cookie 解開後可能包含：
        // 會員 Id：52
        // 帳號：ho1999
        // 角色：Member
        // 狀態：1
        // 但 Cookie 裡的資料是登入當時保存的舊資料。假設會員登入後，管理員才把資料庫的狀態改成 3，Cookie 裡仍可能是 1。
        // => 因此需要 OnValidatePrincipal 重新查詢 MySQL。
        //
        // 非同步箭頭函式 async context =>
        // 可以先理解為：
        // async (context) =>
        // {
        //     要執行的程式
        // }
        // 概念上類似：
        // async Task ValidateCookie(CookieValidatePrincipalContext context)
        // {
        //     要執行的程式
        // }
        // 之所以使用 async，是因為裡面要用 EF Core 非同步查詢資料庫：
        // await dbContext.Users .SingleOrDefaultAsync(...);
        //
        // context 是什麼？
        // context 包含這一次 Cookie 驗證需要的資料及操作功能。
        // context.Principal 取得 Cookie 裡的登入身分和 Claims。
        // context.HttpContext 取得目前的 HTTP Request 環境。
        // context.RejectPrincipal() 宣布這張 Cookie 的登入身分無效。
        options.Events.OnValidatePrincipal = async context =>
        {
            // 從Cookie取得會員編號
            string? userIdText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            // 它是在目前登入者的 Claims 裡，尋找第一個指定類型的 Claim，然後直接回傳它的文字值。
            // 假設 Cookie 裡有：
            // NameIdentifier = "52"
            // Name = "ho1999"
            // Role = "Member"
            // 執行：
            // context.Principal.FindFirstValue(
            //     ClaimTypes.NameIdentifier
            // )
            // 結果是："52"
            // 因此接收型別是：string?
            //

            // Cookie 裡沒有合法會員編號時，
            // 拒絕這張 Cookie，並讓使用者登出
            //
            // 假設：userIdText = "52";
            // 執行後：int.TryParse("52", out int userId)
            // => return true , userId=52
            // => !true = false , userId=52
            // --------
            // userIdText = "abc";
            // => return false , userId=0
            // => !false = true , userId=0 => 進if
            // --------
            // userIdText = null,
            // => return false , userId=0
            // => !false = true , userId=0 => 進if
            if (!int.TryParse(userIdText, out int userId))
            {
                context.RejectPrincipal();
                // 拒絕目前從 Cookie 還原出來的登入身分，不再承認這個使用者已登入。
                // 可能造成這種情況的原因包括：
                // Cookie 裡沒有 NameIdentifier
                // NameIdentifier 是空值
                // 會員編號不是數字
                // Cookie 資料已經不合法

                // SignOutAsync(...):使用 Cookie Authentication 執行登出，要求刪除登入 Cookie。
                // 請使用名稱為 "Cookies" 的驗證方案執行登出
                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme
                // 實際值是："Cookies"
                // 為什麼需要方案名稱？
                // 所以執行登入、登出或驗證時，需要告訴 ASP.NET Core：這次要使用哪一套驗證系統？
                );

                return;
            }
            // | 語法                 | 從哪裡找                            | 找到後回傳     |
            // |---                  |---                                 |---           |
            // | `FindFirst()`       | `ClaimsPrincipal` 的 Claims        | 完整 `Claim`  |
            // | `FindFirstValue()`  | `ClaimsPrincipal` 的 Claims        | Claim 的字串值 |
            // | `List<T>.Find()`    | 記憶體中的 `List`                    | 第一個符合的元素 |
            // | `FirstOrDefault()`  | 集合或 EF Core 查詢                  | 第一個符合的元素 |
            // | `SingleOrDefault()` | 集合或 EF Core 查詢                  | 唯一符合的元素   |
            //
            // | 符合筆數  | `FirstOrDefault()` | `SingleOrDefault()` |
            // |---:      |---                 |---                  |
            // | 0 筆     | 回傳 `null`         | 回傳 `null`          |
            // | 1 筆     | 回傳該筆資料          | 回傳該筆資料          |
            // | 2 筆以上  | 回傳第一筆           | 丟出例外              |

            // 從 DI 容器取得這次 Request 使用的 DbContext

            // 為什麼不能直接使用 _dbcontext？
            // 在 Controller 裡，你可以透過建構子取得
            // 因為 AuthController 是一個 class，ASP.NET Core 建立 Controller 時，會把 DbContext 傳進建構子
            //
            // context.HttpContext 代表目前這一次 HTTP Request 的完整環境 包括：Request、Response、User、Services
            // RequestServices 代表這次 Request 可以使用的 DI 服務容器。
            //
            // --- 取得資料庫工具 ---
            // 在 Cookie 驗證事件裡取得 DbContext，然後用 Cookie 裡的會員編號重新查詢 MySQL
            var dbContext =
                context.HttpContext.RequestServices.GetRequiredService<PawPuffPracticeMySQLDbContext>();
            // 現在 Cookie 驗證程式需要查資料庫，所以向 ASP.NET Core 索取:
            // .GetRequiredService<PawPuffPracticeMySQLDbContext>()
            // 取得後放進：dbContext
            // 因此現在可以使用：dbContext.Users

            // --- 查詢會員 ---
            // dbContext.Users 代表要查詢 MySQL 的 Users 資料表
            var currentUser = await dbContext
                .Users.AsNoTracking() //只讀取會員,不追蹤
                .SingleOrDefaultAsync(user => user.Id == userId);
            // 找出唯一 一筆 Id == Cookie會員編號 的資料

            // 找不到當前會員 或者 當前會員是停用狀態
            if (currentUser is null || currentUser.Status != 1)
            {
                context.RejectPrincipal();
                // 拒絕目前從 Cookie 還原出來的登入身分，不再承認這個使用者已登入。

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme
                );
            }
        };
    });
builder.Services.AddAuthorization();

//後端註冊 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "VueCors",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    );
    //日曆專用規則
    options.AddPolicy(
        "PublicCalendar",
        policy =>
        {
            policy
                .AllowAnyOrigin() //允許任何網站讀取回應
                .WithMethods("GET"); //這個規則允許 GET
            // 不要加 AllowCredentials()，公開日曆不需要跨來源登入 Cookie。
        }
    );
});

// 註冊 API Endpoint 探索服務
builder.Services.AddEndpointsApiExplorer();

// 註冊 Swagger／OpenAPI 文件產生器
builder.Services.AddSwaggerGen();

//macOS 的快速修復按鍵 Command + .
//但是Swashbuckle 是nuget套件
//Swashbuckle.AspNetCore

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//把產生 OpenAPI 文件所需的服務註冊進 DI 容器
// builder.Services.AddOpenApi();

// OpenAPI
// ＝ API 說明文件的標準格式
// = 說明書的格式規定

// Swagger
// ＝ 圍繞 OpenAPI 的一組工具
// = 把說明書顯示成可操作網頁的工具
//
// => Swagger UI 讀取 OpenAPI 文件，再把它顯示成可以點擊測試的網頁

// 註冊發送 HTTP 請求的工具
builder.Services.AddHttpClient();

// 為什麼加這行？
// 下一步我們會在 ClockIn() 的參數寫 [FromServices] LineMessageService lineMessageService，意思是「請 ASP.NET Core 給我一個 LineMessageService」。
//
// AddScoped<LineMessageService>() 是先登記「有人要用 LineMessageService 時，請 ASP.NET Core 建一個給他」。
builder.Services.AddScoped<LineMessageService>();

// var app → 真正要執行的 Web 應用程式
// builder.Build() → 根據設定建立應用程式
// builder = 還在組裝
// app = 已經組裝完成
var app = builder.Build();

// 只在開發環境提供 Swagger
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // 建立測試會員
    //
    // 建立服務使用範圍
    //
    // 可以把 Scope 想成一個「臨時工作區」
    // using 表示程式離開區塊後，會自動清理這個工作區及其中的 DbContext
    // 因此資料庫連線等資源不會一直被占用
    using var scope = app.Services.CreateScope();

    // 從服務範圍取得 dbContext
    // 從剛才建立的工作區，取得已設定完成的資料庫物件
    //
    // GetRequiredService<PawPuffPracticeMySQLDbContext>()
    // 它會使用你先前在 Program.cs 設定的連線字串和資料庫選項，建立正確的 dbContext。
    var dbContext = scope.ServiceProvider.GetRequiredService<PawPuffPracticeMySQLDbContext>();

    // 執行開發環境假資料初始化
    // 把 dbContext 交給 SeedAsync()
    // 把 dbContext 傳給你定義的 DevelopmentDataSeeder.SeedAsync() 方法
    await DevelopmentDataSeeder.SeedAsync(dbContext);

    // 啟用 Swagger
    //
    //建立可以取得 OpenAPI JSON 的網址
    // app.MapOpenApi();
    //http://localhost:5140/api/Practice/hello
    //顯示:{"mesage":"PawPuff練習API 連線成功"}

    // 提供 /swagger/v1/swagger.json
    app.UseSwagger();

    // 提供 /swagger 測試頁
    app.UseSwaggerUI();
}

// 將 HTTP Request 重新導向 HTTPS
app.UseHttpsRedirection();

//Program.cs 預設沒有這個 ?
//因為新版 .NET 10 的 dotnet new webapi 預設通常建立的是 Minimal API，不是 Controller API，所以預設 Program.cs 可能沒有
//
//為什麼你有 Controller，Program.cs 卻沒有？
//可能是一開始建立的是 Minimal API 範本 → 範本沒有 AddControllers、MapControllers
//→ 後來你自己建立 PracticeController.cs

// 按照「請求網址」選擇 CORS 規則：日曆 API 使用公開規則，其他 API 維持原本 Vue 的規則。
// CORS 檢查站
// 它負責確認：發出 Request 的前端網站，是否被後端允許？  app.UseCors("VueCors");
// UseWhen 表示條件成立時執行該分支：
// /api/calendar/... → PublicCalendar
// 其他網址           → VueCors
app.UseWhen(
    // 檢查這次請求的路徑是否屬於 /api/calendar
    context => context.Request.Path.StartsWithSegments("/api/calendar"),
    // 套用名為 PublicCalendar 的 CORS 規則
    branch => branch.UseCors("PublicCalendar")
);
app.UseWhen(
    // 檢查這次請求的路徑是否不屬於 /api/calendar -> 其他API
    context => !context.Request.Path.StartsWithSegments("/api/calendar"),
    // 套用名為 VueCors 的 CORS 規則
    branch => branch.UseCors("VueCors")
);

// 為什麼要分開？
// 你的其他 API 可能只允許自己的 Vue 網站跨來源呼叫；日曆則希望其他網站也能讀取。

// 它負責確認：這個 Request 是哪一位會員送來的？
app.UseAuthentication();

//當 Request 進入時，執行授權檢查
//也就是[Authorize]
//協助檢查：使用者是否登入、是否具有指定角色、是否符合 Policy、是否有需要的 Claim
app.UseAuthorization();

//表示讓 [Route] 和 [HttpGet] 真的能對應到網址
//如果缺少 app.MapControllers()，即使 PracticeController 寫對了，也會得到 404
app.MapControllers();

// 設定「即時通知的連線網址」
app.MapHub<AdminLeaveHub>("/hubs/admin-leaves");

// 「前端連到 /hubs/admin-leaves 時，交由 AdminLeaveHub 處理。」

// MapHub 註冊 SignalR 即時連線端點
// <AdminLeaveHub> 使用 AdminLeaveHub 這個 Hub 類別處理連線
// "/hubs/admin-leaves" 前端要連線的網址路徑，由你命名

// 啟動 Web 應用程式
app.Run();
