namespace LineBot_Class_Library;

//「保存 LINE 設定的類別」
//
// 分兩件事：「你主動傳訊息給 LINE」和 「LINE 主動送事件到你的後端」
//
// 你的後端 → LINE : Channel access token
// LINE → 你的後端 : Channel secret
//
// 例如：假單核准後...
// 你主動發通知：
// 你的後端跟 LINE 說「請幫我把『假單已核准』傳給這個人」。LINE 要確認你的後端有權代表這個官方帳號發訊息，所以你帶上 Token。
//
// LINE 主動通知你的後端：
// 例如有人傳訊息給官方帳號，LINE 把這個事件送到你的後端（這叫 Webhook）。這次換成你的後端要確認請求真的是 LINE 送來的，所以用 Secret 檢查簽章。
//
// Token 是 LINE 查你有沒有權限；Secret 是你查收到的事件是不是真的。
public class LineBotOptions
{
    public string ChannelSecret { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}
