// 讓類別庫能驗證 LINE webhook 的簽章
//
// 為什麼需要驗證？
// 假設你的後端收到一個請求，內容說「某個使用者加了官方帳號好友」。
// 光看這句話，你不知道是不是 LINE 真的送來的。如果你的程式要根據這個事件記下收件人的 LINE user ID，就應該先驗證，再使用裡面的資料。
//
// 驗證的是「這封通知是不是 LINE 送來的、內容有沒有被改過」，不是驗證使用者的身分，也不是查他有沒有加好友。
//
// 假設你的後端收到一句：「LINE 使用者 U123 加了好友。」任何人都能寫出這句話。如果後端只看內容就把 U123 存起來，就可能把假的事件當成真的。
// 所以 LINE 除了送內容，還會附上一個簽章。
//

using System.Security.Cryptography;
using System.Text;

namespace LineBot_Class_Library;

public class LineSignatureVerifier
{
    // requestBody：LINE 送來的原始內容，用 byte[] 保存。
    // signature：LINE 送來的簽章,從 x-line-signature 標頭取出的簽章。對方可能沒帶，所以是 string?。
    // channelSecret：後端自己的設定，不是從對方的請求拿的。
    public static bool IsValid(byte[] requestBody, string? signature, string channelSecret)
    {
        // 少了簽章或 Secret，就不能驗證
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(channelSecret))
        {
            return false;
        }

        // LINE 規定的驗證方式
        // 把 Secret 轉成運算需要的位元組
        // 用 Secret 和原始內容算出結果
        // 把結果轉成 Base64 文字
        byte[] key = Encoding.UTF8.GetBytes(channelSecret);
        byte[] hash = HMACSHA256.HashData(key, requestBody);
        string expectedSignature = Convert.ToBase64String(hash);

        // 「我要比對兩份簽章，.NET 有沒有專門做安全比對的方法？」
        // 才知道現成的方法叫 FixedTimeEquals()，它被放在 CryptographicOperations 這個類別裡。
        //
        // 例如： Math.Max(a, b)：你是先想「我要取較大的數」，不是先想到 Math；查到 Max() 放在 Math 裡，就寫 Math.Max()。
        //
        // 比對兩份簽章：「我自己算的」和「LINE 隨請求送來的」。相同就回 true，不同就回 false
        // FixedTimeEquals() 比較的是位元組
        // GetBytes(...)：兩份簽章目前都是文字，但 FixedTimeEquals() 比較的是位元組 byte[]，所以兩份都用同樣的 UTF-8 規則轉換。
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature), // 自己算出的簽章
            Encoding.UTF8.GetBytes(signature) // 從請求標頭收到的簽章
        );
    }
}
