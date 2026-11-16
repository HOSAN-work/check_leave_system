using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Namespace
{
    // 加一個本機測試 API，讓你在 Swagger 按 Execute，手機就能收到一則 LINE 訊息。
    //
    // [FromServices] 表示這三個工具由 ASP.NET Core 提供，不是由 Swagger 傳入。
    // 讀我存好的 Token 和收件人 ID -> 所以請 ASP.NET Core 給我IConfiguration config
    // 確認現在是開發環境 -> IWebHostEnvironment environment
    // 建立可以向 LINE 發請求的 HttpClient -> IHttpClientFactory clients
    // IHttpClientFactory 是專門提供 HttpClient 的工具

    // api/LineTest
    [Route("api/[controller]")]
    [ApiController]
    public class LineTestController : ControllerBase
    {
        [HttpPost("push")]
        public async Task<IActionResult> Push(
            // 「Swagger 要呼叫哪裡？方法需要哪些工具？」
            // 「讀設定、檢查開發環境、發 HTTP」
            [FromServices] IConfiguration config,
            [FromServices] IWebHostEnvironment environment,
            [FromServices] IHttpClientFactory clients
        )
        {
            //「這次可不可以發？」
            // 環境檢查
            // launchSettings.json："ASPNETCORE_ENVIRONMENT": "Development"
            // -> 「用這個啟動設定啟動後端時，把環境名稱設成 Development。」
            //
            // environment.IsDevelopment()
            // 是在問：「這次啟動的 ASP.NET Core 後端，環境名稱是不是 Development？
            if (!environment.IsDevelopment())
                return NotFound();

            // 讀你後端的設定
            //「LineBot 這組設定裡，`ChannelAccessToken`、`UserId` 的值是什麼？」
            string? token = config["LineBot:ChannelAccessToken"];
            string? userId = config["LineBot:UserId"];

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userId))
                return Problem("本機設定缺少 LINE Token 或測試收件人的 User ID。");

            // 「我要請 LINE 幫我推播一則訊息。」
            // 「要送給 LINE 的 POST 請求」
            // new HttpRequestMessage(...)：建立請求物件，取名叫 request。
            // `using var` 表示離開這個方法時，會自動釋放這個物件。
            // HttpMethod.Post：指定這份請求要用 POST 送。
            //
            // "https://api.line.me/v2/bot/message/push"：指定這份請求要送到 LINE 的推播 API，不是你自己的 /api/line-test/push。
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.line.me/v2/bot/message/push"
            //LINE 官方提供的「發送推播訊息」網址，不是你自己取的
            );

            // 準備送給 LINE 的請求上，加上「發送憑證」
            // request.Headers.Authorization = ... 把組好的值放進這份請求的 Authorization 標頭。
            // LINE 收到 POST 後，要用 Token 判斷你的後端有沒有權限代表官方帳號發訊息。這個標頭管有沒有權限發；
            // "Bearer" 是憑證的格式名稱。LINE 文件要求用 Bearer，不是你自己取的。
            // token 是你從後端設定讀到的 Channel access token
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            // 例如，假設 token 是 "abc123"，送出時的標頭就長這樣：
            // Authorization: Bearer abc123

            // 準備送給 LINE 的 Body：要傳給誰、要傳什麼
            // Content 放訊息內容
            // new { type = "text", text = "PawPuff 通知測試" } 建立一則訊息
            // `type` 告訴 LINE 這是文字訊息，`text` 是手機上會看到的文字
            //
            // new[] { ... } 把這一則訊息放進清單
            // LINE 的 messages 欄位要的是陣列，所以只傳一則也要包在陣列裡。
            //
            // 外層 new { to = userId, messages = ... } 把收件人和訊息清單包成一個 C# 物件。
            // JsonContent.Create(...) 把這個物件包成 JSON 內容，`request.Content =` 則把它放到請求的 Body。
            //
            // 假設 userId 是 "U123" 送出時的 JSON：
            // {
            //   "to": "U123",
            //   "messages": [
            //     { "type": "text", "text": "PawPuff 通知測試" }
            //   ]
            // }
            request.Content = JsonContent.Create(
                new
                {
                    to = userId,
                    messages = new[] { new { type = "text", text = "系統通知測試" } },
                }
            );

            // 最後才送出，再把結果回給 Swagger
            //
            // 拿到發送工具
            using var client = clients.CreateClient();
            // 送給 LINE，等它回應
            // request 就是前面裝好 POST 網址、Token、收件人和訊息的那一份請求。
            // `SendAsync()` 才真正送出
            using var response = await client.SendAsync(request);

            // 看 LINE 的回應，再決定你的 API 要回什麼
            // IsSuccessStatusCode -> 「LINE 回的是不是 2xx？」
            //
            // IsSuccessStatusCode = false -> !false = true => return xxx
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            return Ok(new { message = "LINE 已接受發送請求，請查看手機。" });
        }
    }
}
