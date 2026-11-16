using LineBot_Class_Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

// 已確認簽章驗證方法和 LineBotOptions 註冊都寫好了
// 現在接上 webhook 入口
//
// LINE ──HTTP POST──▶ Controller ->
//                     讀原始內容與簽章 → 呼叫 IsValid() → 決定怎麼回應
//
// LINE ◀──HTTP 200─── Controller（成功接受事件時）
//
// LINE 官方規定的是「收到 Webhook 後，先用原始請求內容驗證 x-line-signature」
namespace PawPuffPractice.Api.Controllers
{
    // api/LineWebhook
    [Route("api/[controller]")]
    [ApiController]
    public class LineWebhookController : ControllerBase
    {
        // 接住 LINE 送來的 POST，驗證簽章，再回覆 LINE
        // 寫之前先想：「我已經有 IsValid()，它需要哪三個值？我去哪裡拿？」
        // [AllowAnonymous]：LINE 不會登入你的請假系統，所以這個入口不要求網站登入。
        // 非同步，是因為 Receive() 要讀取 HTTP 請求的 Body，而 Body 不一定已經全部收到。
        // [FromServices]：告訴 ASP.NET Core：「這個參數從後端註冊的服務取得，不要從 POST 的 Body 取。」
        // IOptions<LineBotOptions>：參數的型別。它是 ASP.NET Core 提供的設定容器，裡面裝的是 LineBotOptions。
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Receive([FromServices] IOptions<LineBotOptions> options)
        {
            // 讀取 LINE 送來的原始內容
            //
            // using var 會在方法離開時自動釋放它
            using var body = new MemoryStream(); //在記憶體開一個暫存空間
            // CopyToAsync(body) 把位元組複製到剛開好的暫存空間
            await Request.Body.CopyToAsync(body); //Request.Body → body

            // 從請求標頭取出 LINE 提供的簽章
            // Request.Headers["X-Line-Signature"] 是用標頭名稱找值
            // 如果標頭沒帶，這裡會得到空字串，前面寫好的 IsValid() 會回傳 false。
            string signature = Request.Headers["X-Line-Signature"].ToString();

            // 把三個值交給類別庫
            bool isValid = LineSignatureVerifier.IsValid(
                body.ToArray(), // 把暫存內容取成 byte[]
                signature, // 剛剛讀到的標頭文字
                // 從容器拿出設定物件
                // LineBotOptions lineSettings = options.Value;
                //
                // 從設定物件拿出 Secret
                // string channelSecret = lineSettings.ChannelSecret;
                //
                //  合成一行 ==> options.Value.ChannelSecret
                // options.Value.ChannelSecret 是 API 設定提供的密鑰
                options.Value.ChannelSecret
            );

            //  未驗證過 isValid = false -> !false = true -> 401
            if (!isValid)
            {
                return Unauthorized(); // 401
            }

            return Ok();
        }
    }
}
