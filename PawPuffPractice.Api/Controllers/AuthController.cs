using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using PawPuffPractice.Api.Data;
using PawPuffPractice.Api.Models.Dtos;
using PawPuffPractice.Api.Models.EfModels;
using PawPuffPractice.Api.Services;

namespace PawPuffPractice.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        //AuthController 需要操作 MySQL，所以請 ASP.NET Core 把 DbContext 交給它，並保存起來供後面的註冊、登入方法使用。

        //readonly 是要避免 _dbcontext 在 Controller 執行途中被不小心換掉。
        private readonly PawPuffPracticeMySQLDbContext _dbcontext;

        //建構函式
        //Controller 的初始化程序
        //當 ASP.NET Core 建立 AuthController 時，取得資料庫操作物件，然後存進 _dbcontext，讓登入和註冊方法可以操作 MySQL。
        public AuthController(PawPuffPracticeMySQLDbContext dbcontext)
        {
            //我把收到的 DbContext 保存起來，等等登入和註冊要用
            _dbcontext = dbcontext;
        }

        // 沒有 static 的方法
        // public class Dog
        // {
        //    public string Name { get; set; }

        // public void Bark()
        //  {
        //    Console.WriteLine($"{Name}：汪！");
        //  }
        // }
        // Bark() 是「某一隻 Dog 物件」的功能
        // Dog dog = new Dog(); //產生一隻狗的物件
        //
        // dog.Name = "小白";
        // dog.Bark();  //小白：汪！
        // ------------------
        // static 的方法
        // public class Dog
        // {
        //   public static string GetAnimalType()
        //   {
        //      return "狗";
        //    }
        // }
        // GetAnimalType()  //Dog 這個 class 本身提供的功能
        // 直接：Dog.GetAnimalType();

        // 輸入正確帳號密碼
        // → 後端找到會員
        // → 密碼正確
        // → Status 不是 1
        // → 將 Status 轉成原因文字
        // → 回傳 403 與 message
        // → Axios 進入 catch
        // → LoginView 顯示後端 message

        #region 用 UserStatusDefinition.cs 代替
        // 建立狀態文字轉換方法
        // private static string GetUserStatusText( sbyte status)
        // {
        // switch 在做什麼？
        // 假設：user.Status == 3
        // GetUserStatusText(user.Status)
        // => GetUserStatusText(3);
        // => 違反使用規範
        //
        // switch作用看status現在是哪一個數字，選擇對應的文字回傳
        // 檢查 status，選出一個文字，然後將文字 return 回去
        //
        // 3 => "違反使用規範"
        // 如果 status 等於 3，結果就是 "違反使用規範"
        //
        // _ 代表：前面的條件全部不符合時，使用這個結果。
        // 它相當於 if 的最後一個 else
        //     return status switch
        //     {
        //         1 => "正常啟用",
        //         2 => "會員自行申請停用",
        //         3 => "違反使用規範",
        //         4 => "停用原因 4",
        //         5 => "停用原因 5",
        //         6 => "停用原因 6",
        //         7 => "停用原因 7",
        //         _ => "未知的帳號狀態"
        //     };
        // }
        #endregion

        #region 程式流程:
        // 有人呼叫 /api/Auth
        //         ↓
        // ASP.NET Core 準備建立 AuthController
        //         ↓
        // 先執行 AuthController 的建構函式
        //         ↓
        // 把 DbContext 傳進來
        //         ↓
        // 存入 _dbcontext
        //         ↓
        // 登入、註冊方法可以操作資料庫
        #endregion


        //新增、註冊會員資料
        // Post/api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            // 1. 整理前端送進來的文字
            //request 是「前端送過來的註冊資料物件」
            string account = request.Account.Trim();
            string nickname = request.Nickname.Trim();

            //ToLowerInvariant() : 把英文字母轉成小寫
            string email = request.Email.Trim().ToLowerInvariant();

            //string.IsNullOrWhiteSpace(...)
            //檢查字串是不是null 或 空字串 "" 或 全部都是空白 "     " => true
            // 如果沒有輸入有效內容 = null,如果有輸入內容=request.Phone.Trim()
            string? phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

            // 2. 檢查帳號是否已存在
            // AnyAsync(...) : 是否至少存在一筆符合條件的資料？
            //
            // user => user.Account == account
            //檢查資料表中的每一筆會員，該會員的 Account 是否等於使用者這次輸入的 account
            // user.Account → 資料庫目前這筆會員的 Account
            //     account  → request.Account 整理後的帳號
            //
            //等 MySQL 檢查完成，再把 true 或 false 存進 accountExists
            bool accountExists = await _dbcontext.Users.AnyAsync(user => user.Account == account);
            if (accountExists)
            {
                // 409 Conflict
                // 這次請求的資料，與伺服器目前已有的資料衝突
                return Conflict(new { message = "此帳號已被使用" });
            }

            #region
            //為什麼使用 await？
            // 因為查詢 MySQL 需要時間
            //
            //await _dbContext.Users.AnyAsync(...)
            //AnyAsync() 是非同步方法，程式需要等待資料庫回覆查詢結果，再把结果存入
            #endregion

            // 3. 檢查 Email 是否已存在
            bool emailExists = await _dbcontext.Users.AnyAsync(user => user.Email == email);
            if (emailExists)
            {
                return Conflict(new { message = "此Email已被使用" });
            }

            // 4. 有輸入電話時，才檢查電話是否重複
            if (phone is not null)
            {
                bool phoneExists = await _dbcontext.Users.AnyAsync(user => user.Phone == phone);
                if (phoneExists)
                {
                    return Conflict(new { message = "此電話已被使用" });
                }
            }

            // 5. 將密碼轉成雜湊值
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 6. 建立要存入資料庫的 User 物件
            //
            // 為什麼後端仍要明確指定 DEFAULT 'Member' ?
            // 在程式中明確指定，可以清楚保證透過註冊 API 建立的帳號一定是普通會員
            User user = new User
            {
                Account = account,
                PasswordHash = passwordHash,
                Nickname = nickname,
                Email = email,
                Phone = phone,
                PointBalance = 0,
                Status = 1,
                Role = "Member",
                Biography = null,
                CreatedAt = DateTime.Now,
                LastLoginAt = null,
            };

            // 7. 加入並儲存
            //真正把新會員新增進 MySQL
            //
            //Add(user) → 將 user 標記為「待新增」
            //EF Core 會開始追蹤 user，並把它的狀態標記成：Added（等待新增）
            //把資料放進「待處理清單」
            _dbcontext.Users.Add(user);

            // 檢查 DbContext 目前追蹤的變更
            // 產生並執行類似下面的 SQL：
            //  INSERT INTO users
            // (
            //     account,
            //     password_hash,
            //     nickname,
            //     email,
            //     phone
            // )
            // VALUES
            // (
            //     'hosan123',
            //     '雜湊後的密碼',
            //     'Hosan',
            //     'hsosan@example.com',
            //     '0912345678'
            // );
            //MySQL 執行成功後，資料才真正出現在 users 資料表
            await _dbcontext.SaveChangesAsync();

            // 8. 回傳結果，但不能回傳 PasswordHash
            //把結果回傳給前端，並結束這次 Register() 方法
            //StatusCode(狀態碼, 回傳內容)
            //StatusCodes.Status201Created 其實就是數字：201
            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    message = "註冊成功",
                    user = new
                    {
                        user.Id,
                        user.Account,
                        user.Nickname,
                        user.Email,
                        user.Phone,
                        user.PointBalance,
                        user.Status,
                        user.Role,
                        user.CreatedAt,
                    },
                }
            );

            //或者
            //return Created($"/api/users/{user.Id}",new
            // {
            //     message = "註冊成功",
            //     user = new
            //     {
            //         user.Id,
            //         user.Account,
            //         ...
            //      }
            //   }
            // );
        }

        //登入
        // Post/api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            // 1. 整理帳號文字
            string account = request.Account.Trim();

            // 2. 依照帳號尋找會員
            //尋找 Account 等於使用者輸入帳號的唯一會員；找不到就回傳 null
            User? user = await _dbcontext.Users.SingleOrDefaultAsync(user =>
                user.Account == account
            );
            //為什麼需要 ? => 因為使用者輸入的帳號可能不存在
            // 找到會員   → user 是 User 物件
            // user.Id / user.Account / user.PasswordHash
            //
            // 找不到會員 → user 是 null
            // user == null
            //
            // SingleOrDefaultAsync(...)
            // Single    → 應該只有一筆
            // OrDefault → 如果沒有，就回傳預設值
            // Async     → 非同步查詢資料庫
            // => 同一帳號理論上最多只有一筆

            // 3. 找不到會員時，回傳 401
            if (user is null)
            {
                return Unauthorized(new { message = "帳號或密碼錯誤" });
            }

            #region 4. 使用 BCrypt 驗證密碼
            // 登入時驗證:使用者現在輸入的密碼，是否與資料庫中的雜湊密碼相符？
            // BCrypt.Net.BCrypt.Verify(原始密碼,雜湊密碼); ?
            #endregion request.Password == user.PasswordHash ?
            bool passwordCorrect = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!passwordCorrect)
            {
                return Unauthorized(new { message = "帳號或密碼錯誤" });
            }

            #region 5. 判斷帳號能不能登入
            // 到這步時,帳號與密碼已是正確的
            // 403 Forbidden 是什麼？
            // 系統已經辨識出帳號和密碼，但此帳號沒有權限完成這個動作。
            //
            // 例如：Status = 3
            // 後端回傳：
            // {
            //     "message":"此帳號目前無法登入:違反使用規範",
            //     "status":3
            // }
            #endregion
            // UserStatusDefinition有定義UserStatusDefinitions方法：public const sbyte Active = 1;
            // 如果不能登入 => if迴圈
            if (user.Status != UserStatusDefinitions.Active)
            {
                string statusText = UserStatusDefinitions.GetText(user.Status);

                return StatusCode(
                    403,
                    new { message = $"此帳號目前無法登入:{statusText}", status = user.Status }
                );
                // 為什麼需要 $？
                // 普通字串："停用原因：{statusText}"
                // 結果就是：停用原因：{statusText}
                //
                // 插值字串：$"停用原因：{statusText}"
                // statusText = "違反使用規範";
                // 結果才會是：停用原因：違反使用規範
            }

            // 6. 更新最後登入時間
            user.LastLoginAt = DateTime.Now;

            await _dbcontext.SaveChangesAsync();

            // 7. 準備放進登入 Cookie 的會員身分資料
            // Claim 是什麼？
            // 理解成一條「身分聲明」: 資料名稱 + 資料內容
            // new Claim(類型, 值)
            //
            // 為什麼要Claim ?
            // 因為登入 Cookie 需要知道：這張 Cookie 是哪一位會員的？
            // Claim 就是放在登入身分裡的基本資料
            //
            //如果完全沒有 Claim，後端即使知道「有人登入了」，也不知道：他是哪個會員 ID ?、他的帳號是什麼？、他有什麼角色或權限？
            //
            // List<Claim> claims
            // 建立一個可以保存多筆 Claim 的清單
            List<Claim> claims = new List<Claim>
            {
                //會員編號
                //ClaimTypes.NameIdentifier
                // 是 .NET 預先定義的標準類型，表示：使用者的唯一識別值
                // 例如 user.Id = 8;
                // => NameIdentifier = "8"
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                //會員帳號
                // ClaimTypes.Name
                // 是標準類型，表示目前使用者的名稱。
                new Claim(ClaimTypes.Name, user.Account),
                //會員暱稱
                //  nickname 不是 .NET 預先定義的標準 Claim，所以使用自訂名稱： "nickname"
                // 在 C# 裡，只要用雙引號包起來，就是 string 字串常值
                new Claim("nickname", user.Nickname),
                //會員狀態
                // 使用自訂名稱： "status"
                // 在 C# 裡，只要用雙引號包起來，就是 string 字串常值
                new Claim("status", user.Status.ToString()),
                //會員權限
                // ClaimTypes.Role
                // 是標準類型，表示目前使用者的權限、角色
                //
                // 之後：[Authorize(Roles = "Admin")]
                // 會自動尋找 Cookie 裡的：ClaimTypes.Role
                new Claim(ClaimTypes.Role, user.Role),
            };
            // 為什麼要放進 Cookie？
            // 後端不希望每次都要求使用者重新傳送帳號和密碼
            // 所以登入成功時，把必要身分資料建立為 Claims，再建立登入 Cookie

            // 8. 使用這些 Claims 建立會員身分
            //把剛才準備好的 claims，組合成 ASP.NET Core 認得的「登入者身分物件」
            // ClaimsIdentity → 一組身分資料，代表一種登入身分
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(
                //之前建立的 Claim 清單
                claims,
                //這個身分是使用 Cookie Authentication 驗證的
                // CookieAuthenticationDefaults.AuthenticationScheme 或者寫 "Cookies"
                "Cookies"
            );

            // ClaimsPrincipal → 代表目前正在操作系統的這位完整使用者
            // 為什麼還要包一層 ClaimsPrincipal？
            // 因為一位使用者理論上可以同時具有多個 Identity
            // 例如：Cookie 登入身分、Google 登入身分、公司帳號登入身分
            // 而每種登入方式分別是一個ClaimsIdentity
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            //     Claim
            // → 證件上的一個欄位
            // → 姓名、會員編號

            // ClaimsIdentity
            // → 一張完整證件
            // → 會員證

            // ClaimsPrincipal
            // → 拿著這張證件的人
            // → 目前登入的使用者

            // 9. 建立加密的 HttpOnly Cookie
            // 「真正完成登入、產生 Cookie」
            // 直到執行 SignInAsync()，ASP.NET Core 才會把身分交給 Cookie Authentication，並在 HTTP 回應中建立 Cookie
            //
            // HttpContext 是什麼？
            // 代表目前這一次 HTTP 請求與回應的完整環境
            // Request  → 前端送來的資料
            // Response → 後端準備回傳的資料
            // User     → 目前使用者身分
            //
            // HttpContext.SignInAsync(...)
            // 對目前這次 HTTP 回應執行登入，讓回應帶回 Cookie
            //
            // SignInAsync() 是什麼？
            // 表示非同步完成登入
            // 它主要會：
            // 1. 接收登入者的 ClaimsPrincipal
            // 2. 使用 Cookie Authentication 處理身分
            // 3. 保護 Cookie 內容，避免任意修改
            // 4. 在 HTTP Response 加入 Set-Cookie
            // 5. 瀏覽器收到回應後保存 Cookie
            await HttpContext.SignInAsync(
                // 請使用名稱為 Cookies 的驗證系統登入
                "Cookies",
                // 代表要登入的會員身分
                claimsPrincipal,
                // 登入 Cookie 的個別設定
                new AuthenticationProperties
                {
                    // 建立「工作階段 Cookie」
                    // 關閉瀏覽器後 Cookie 消失
                    // 瀏覽器開著 → Cookie 存在
                    //
                    // 如果設定：IsPersistent = true
                    // 表示建立持久 Cookie，關閉瀏覽器後仍可能保存，直到過期時間到達
                    IsPersistent = false,

                    //允許這一次登入的 Cookie 之後被系統重新發放，以更新有效期限
                    // 允許 SlidingExpiration 自動更新有效期限
                    //
                    // SlidingExpiration = true
                    // → 整個 Cookie 驗證系統啟用「滑動過期」
                    //
                    // AllowRefresh = true
                    // → 這一次登入允許使用滑動過期
                    //
                    //options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    // 使用者在上午 9 點登入
                    // 原本期限：09:00～17:00
                    //使用者持續使用網站，到了約下午 1 點多又呼叫 API。ASP.NET Core 判斷登入期限已使用超過一半，可能重新發放 Cookie
                    //更新後期限：約 13:30～21:30
                    //這個重新發放需要AllowRefresh = true
                    AllowRefresh = true,
                    //
                    //AllowRefresh = false
                    //即使全域設定：
                    // SlidingExpiration = true
                    // 這一次登入也不允許更新期限
                    // 期限：09:00～17:00
                    // 使用者即使一直操作，到期後還是要重新登入。
                    //
                    // SlidingExpiration = true → 開啟自動延期功能
                    // AllowRefresh = true      → 這次登入允許自動延期
                }
            );

            // 10. 回傳安全的會員資料
            return Ok(
                new
                {
                    message = "登入成功",
                    user = new
                    {
                        user.Id,
                        user.Account,
                        user.Nickname,
                        user.Email,
                        user.Phone,
                        user.PointBalance,
                        user.Status,
                        user.Role,
                        user.Biography,
                        user.CreatedAt,
                        user.LastLoginAt,
                    },
                }
            );
        }

        //讓已登入的使用者取得「自己的最新會員資料」
        // me 表示「目前登入的我」
        // Get/api/Auth/me
        [Authorize]
        [HttpGet("me")]
        // Task<IActionResult> → 最後會非同步回傳 HTTP 結果
        // GetCurrentUser → 方法名稱：取得目前會員
        public async Task<IActionResult> GetCurrentUser()
        {
            // 1. 從登入 Cookie 的 Claims 取得會員 Id
            // 例如：NameIdentifier = "8"
            // 結果可能是：userIdText = "8";
            // 也可能是：userIdText = null;
            //
            // User 是哪一個？
            // User.FindFirstValue(...)
            // ControllerBase 提供的ClaimsPrincipal User 代表目前登入者的身分
            // Controller 裡的 User → 目前登入者的 ClaimsPrincipal
            // 資料庫的 User  → users 資料表對應的 Entity
            //
            //FindFirstValue()：從目前登入者的 Claim找值
            //找第一個類型為 NameIdentifier 的 Claim，並取得它的值
            //
            //SingleOrDefaultAsync()：從資料庫找一筆 Entity
            //
            string? userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. 檢查會員 Id 是否存在且能轉成 int
            // 嘗試把 userIdText 轉成 int，成功就把結果放進 userId
            // 同時產生兩個結果：
            // 1. 回傳 true 或 false，表示轉換是否成功。
            // 2. 透過 out 把轉換後的整數交給 userId。
            // out 表示：請這個方法把額外的結果放到 userId 變數中
            //
            // userIdText = "8" ; => true =>  return userId = 8
            // => !true = false
            //
            // userIdText = "abc"; => false => return userId = 0
            // => !false = true
            // => 如果不能成功轉成整數，就進入 if
            //
            // userIdText = null; => false => return userId = 0
            // => !false = true
            // => 如果不能成功轉成整數，就進入 if
            if (!int.TryParse(userIdText, out int userId))
            {
                return Unauthorized(new { message = "登入資訊無效" });
            }

            // 3. 使用 Cookie 中的會員 Id 查詢最新會員資料
            // 只從Users table 查詢不追蹤且從資料庫尋找唯一一筆 Id 等於登入會員 ID 的資料
            // user.Id → users 資料表目前這筆會員的 Id
            // userId  → 從 Cookie Claim 取得並轉成 int 的會員 Id
            User? currentUser = await _dbcontext
                .Users.AsNoTracking()
                .SingleOrDefaultAsync(user => user.Id == userId);

            // 4. 資料庫已經找不到這位會員
            if (currentUser is null)
            {
                //401
                return Unauthorized(new { message = "找不到會員資料" });
            }

            // 5. 判斷帳號目前能否使用
            if (currentUser.Status != 1)
            {
                // Forbidden(new { ... }) // 沒有這個快捷方法
                //因此 StatusCode(狀態碼, 回傳資料)
                return StatusCode(403, new { message = "此帳號目前無法使用" });
            }

            // 6. 回傳最新會員資料
            return Ok(
                new
                {
                    user = new
                    {
                        currentUser.Id,
                        currentUser.Account,
                        currentUser.Nickname,
                        currentUser.Email,
                        currentUser.Phone,
                        currentUser.PointBalance,
                        currentUser.Status,
                        currentUser.Role,
                        currentUser.Biography,
                        currentUser.CreatedAt,
                        currentUser.LastLoginAt,
                    },
                }
            );
        }

        [Authorize]
        [HttpPost("logout")]
        // 為什麼使用 POST？ => 登出會改變登入狀態
        // Post/api/Auth/logout
        public async Task<IActionResult> Logout()
        {
            // 刪除 Cookie Authentication 建立的登入 Cookie
            await HttpContext.SignOutAsync("Cookies");

            return Ok(new { message = "登出成功" });
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequestDto request)
        {
            // 1. 從登入資料取得會員編號
            //在目前登入者 User 的 Claims 裡，找 NameIdentifier 這一種類型的 Claim，然後把它的值取出來
            //
            // string? : 等一下右邊拿回來的資料，我要用「可以是 null 的字串」來存
            // 例如：userIdText = "15"; 也可以userIdText = null;
            //
            // User : 目前這一次 HTTP Request 的登入使用者身分
            // User裡面可能帶著NameIdentifier,Name,Email
            //
            // User.FindFirstValue(...)
            //去 User 裡面「找某一種 Claim」，找到之後把它的 Value 拿出來
            //它到底要找哪一個 Claim？
            //就是括號裡指定：ClaimTypes.NameIdentifier
            string? userIdTdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 嘗試把 userIdText 從 string 轉成 int
            //
            // "15"（string）
            //    ↓
            // TryParse
            //    ↓
            // 成功
            //    ├── 回傳 true
            //    └── userId = 15（int）
            //
            // out int userId 是什麼？
            // 順便建立一個 int 變數：int userId;
            // out 是什麼？
            // 請這個方法把它處理出來的結果，放到這個變數裡
            //
            // int.TryParse("15", out int userId);
            // 「請 TryParse 嘗試把 "15" 轉成整數，然後把轉出來的整數放進 userId。」
            // => return true ,userId = 15
            //
            // ！int.TryParse("15", out int userId)
            // TryParse 先執行：
            // => return true , userId:15
            // ！再執行：
            // => !true => false , userId:15
            //
            // ！int.TryParse("abc", out int userId)
            // TryParse 先執行：
            // => return false , userId:null
            // =>! false = true , userId:null

            // 如果會員 ID 轉換失敗
            // → 不要繼續執行
            // → 回傳 Unauthorized
            if (!int.TryParse(userIdTdText, out int userid))
            {
                return Unauthorized(new { message = "登入資料無效,請重新登入" });
            }

            // 到 Users 資料表尋找 Id 等於目前登入會員編號的第一筆資料，並把結果存入變數 user
            // FirstOrDefaultAsync(...) : 找出第一筆符合條件的資料；如果完全找不到，就回傳 null
            // user => user.Id == userId 拿一筆會員資料進來，檢查這筆會員的 Id 是否等於目前登入者的 userId
            var user = await _dbcontext.Users.FirstOrDefaultAsync(user => user.Id == userid);

            if (user == null)
            {
                return NotFound(new { message = "找不到會員資料" });
            }

            string email = request.Email.Trim();
            // Email 是否已經存在
            // AnyAsync() : 資料庫中有沒有任何一筆會員符合括號內的條件？
            // true  // 已被其他會員使用
            // false // 沒有被其他會員使用
            //
            //_dbContext.Users.AnyAsync(otherUser => ...)
            //otherUser 只是參數名稱,名字可以隨便取
            bool emailExists = await _dbcontext.Users.AnyAsync(otherUser =>
                // 資料庫裡某位會員的 Email == 前端傳來並經過 Trim() 的新 Email
                otherUser.Email == email
                &&
                // 資料庫會員的 Id，不可以等於目前登入者的 userId
                otherUser.Id != userid
            );

            // emailExists = true => 已被其他會員使用
            if (emailExists)
            {
                return Conflict(new { message = "此 Email 已被其他會員使用" });
            }

            user.Nickname = request.Nickname.Trim();
            user.Email = email;

            // 判斷使用者到底有沒有輸入有效內容
            // request.Phone = null;   // 沒有值
            // request.Phone = "";     // 空字串
            // request.Phone = "   ";  // 只有空白
            // 以上都是true
            // else 使用者輸入的電話且去除頭尾空白
            user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

            user.Biography = string.IsNullOrWhiteSpace(request.Biography)
                ? null
                : request.Biography.Trim();

            await _dbcontext.SaveChangesAsync();

            return Ok(
                new
                {
                    message = "會員資料修改成功",

                    user = new
                    {
                        user.Id,
                        user.Account,
                        user.Nickname,
                        user.Email,
                        user.Phone,
                        user.Biography,
                        user.PointBalance,
                        user.Status,
                        user.Role,
                    },
                }
            );
        }

        [Authorize]
        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto request)
        {
            // 從登入 Cookie 的 Claim 取得會員編號
            //  ex: id=5 從 Claim 取出時通常是字串: userIdText = "5";
            string? userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //  userIdtext = "5" -int-> userId=5 , true
            //  ！true = false 且 userId=5
            //
            //  userIdtext ="abc" -int-> userId=null,false
            //  ！false = true 且userId=null
            if (!int.TryParse(userIdText, out int userId))
            {
                return Unauthorized(new { message = "登入資料無效,請重新登入" });
            }

            // 從資料庫取得目前會員
            //  找到第一筆會員資料 => Users表裡的Id == userId = 5
            var user = await _dbcontext.Users.FirstOrDefaultAsync(user => user.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "找不到會員資料" });
            }

            // 驗證使用者輸入的目前密碼
            // 使用者輸入的密碼 == User表裡的密碼雜湊 ？
            bool currentPasswordCorrect = BCrypt.Net.BCrypt.Verify(
                request.CurrentPassword,
                user.PasswordHash
            );

            // currentPasswordCorrect = true => 密碼正確
            // => !currentPasswordCorrect = false
            //
            // currentPasswordCorrect = false => 密碼錯誤
            // => !currentPasswordCorrect = true
            if (!currentPasswordCorrect)
            {
                return BadRequest(new { message = "目前密碼錯誤" });
            }

            // 不允許新密碼與目前密碼相同
            bool sameAsCurrentPassword = BCrypt.Net.BCrypt.Verify(
                request.NewPassword,
                user.PasswordHash
            );

            // sameAsCurrentPassword = true
            if (sameAsCurrentPassword)
            {
                return BadRequest(new { message = "新密碼不能與目前密碼相同" });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            await _dbcontext.SaveChangesAsync();

            return Ok(new { message = "密碼修改成功" });
        }
    }
}
