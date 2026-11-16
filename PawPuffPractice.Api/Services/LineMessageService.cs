namespace PawPuffPractice.Api.Services;

public class LineMessageService
{
    // 1. 上班打卡、下班打卡、Swagger 測試，都共用同一個 LINE 發送方法。
    //    每次發送由呼叫端提供：userId（傳給誰）、message（傳什麼）。
    //
    // 2. Service 用 HttpClient 將請求送到 LINE，
    //    並在 Authorization 標頭附上 Token，讓 LINE 檢查發送權限。
    //
    // 3. AdminClocksController 完成打卡存檔後，
    //    用剛存好的打卡時間組成訊息，再呼叫 LineMessageService 發送。
    //
    // 打卡 Controller 決定「何時通知」；Service 只管「怎麼送到 LINE」。
    #region  為什麼其他 API 不需要 AddHttpClient()？
    // 原本的打卡 API： Vue 發 POST → C# Controller 接收 → 存資料庫 → 回應 Vue。這裡發 HTTP 的是 Vue，不是 C# 後端。
    //
    // 加上 LINE 通知後： Vue 發 POST → C# Controller 接收 → 存資料庫 → C# 後端再主動發一個新的 POST 給 LINE。這第二個 POST 才用到 HttpClient。
    //
    // 需要 HttpClient 不等於一定要寫 AddHttpClient()。我們這版選擇用 IHttpClientFactory 的 CreateClient() 取得它，所以才在 Program.cs 用 AddHttpClient() 把工廠註冊好。這個註冊只做一次，不用每支 API 都加。
    #endregion
    public async Task<bool> SendLineMessageAsync(
        HttpClient client,
        string token,
        string userId,
        string message
    )
    {
        // HttpRequestMessage：.NET內建的,指定要建立的是「HTTP 請求物件」
        // HttpRequestMessage(用什麼方法,送去哪裡)
        //
        // "https://api.line.me/v2/bot/message/push" 是 LINE 提供的「推播訊息入口」
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.line.me/v2/bot/message/push"
        );

        // 加上發送憑證
        // 把 LINE 的 Channel access token 放進 HTTP 請求的 Authorization 標頭，讓 LINE 檢查你的後端有沒有權限代表這個官方帳號發訊息
        //
        // token 是前面從後端設定讀出的 Channel access token。假設它的值是 abc123。
        // new AuthenticationHeaderValue("Bearer", token) 把驗證方式 Bearer 和憑證 abc123 組成標頭的值 Bearer abc123。`Bearer` 是 LINE 要求使用的格式，C# 用這個類別來建立它。
        // request.Headers.Authorization = ... 把這個值放到 request 的 Authorization 標頭
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            token
        );

        // LINE 要知道「傳給誰」和「傳什麼」，所以請求的 Body 要放 to 和 messages
        //
        // to = userId 把呼叫端傳進來的 LINE 使用者 ID 填進收件人欄位 to
        // messages = new[] { ... } 建立一個陣列。這次只放一則訊息，LINE 的 messages 欄位仍然要求陣列。
        // new { type = "text", text = message } 建立一則訊息。`type` 告訴 LINE 這是文字訊息，`text` 才是手機上會看到的內容，例如「上班打卡成功：09:03」。
        // JsonContent.Create(...) 把這些 C# 資料準備成 JSON 請求內容，再用 request.Content = ... 放進同一個 HTTP 請求的 Body。
        request.Content = JsonContent.Create(
            new { to = userId, messages = new[] { new { type = "text", text = message } } }
        );

        // lineMessageService.SendLineMessageAsync(...) 是你寫的方法。它拿到收件人、訊息和 Token，組好 LINE 要的請求，最後回傳 bool
        //
        // client.SendAsync(request) 才會把剛才準備好的 POST 送給 LINE。
        // client.SendAsync(request) 是 HttpClient 自帶的方法。它把組好的請求送出去，回傳 LINE 的 HTTP 回應。你的 Service 再從這份回應取出 IsSuccessStatusCode。
        //
        // `IsSuccessStatusCode` 把回應轉成你的方法要交回的 bool。
        // LINE 回 2xx 就是 true，回 4xx 或 5xx 就是 false。
        using var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }
}
