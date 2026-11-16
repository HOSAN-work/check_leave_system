// 可以寫在 .vue 的 <script setup> 裡；另外建檔，是為了把一項工作獨立出來。
// 獨立出來的理由：SignalR 有自己的連線設定：連哪個網址、怎麼重連。把設定放在這裡
// 
// 建立「接收請假即時通知的連線物件」
//  
// 後端已設好 /hubs/admin-leaves，
// 前端接著需要知道：連到哪裡、是否帶登入身分、斷線怎麼處理。

// HubConnectionBuilder 是 SignalR 提供的連線設定工具。
// 1. 匯入建立連線的工具
import {HubConnectionBuilder} from '@microsoft/signalr'

// 取得網址 → 設定連線 → 設定重連 → 建立物件 → 之後呼叫 start()
// 宣告一個函式，並允許其他檔案匯入使用它
export function createAdminLeaveConnection(){
// 2. 取得後端網址
const baseUrl = import.meta.env.VITE_API_BASE_URL.replace(/\/$/, '')
// VITE_API_BASE_URL 是你在環境設定中的後端網址。
// replace(要找的內容, 替換成什麼)
//  /   \/   $   /
//  │   │    │   └ 結束符號
//  │   │    └ 只找字串結尾
//  │   └ 要找的斜線 /
//  └ 開始符號
// http://localhost:5140/ -->  http://localhost:5140


// 3. 設定連線目的地與登入 Cookie
// new HubConnectionBuilder() 是建立「連線設定工具」；return 則把整串設定完成後的結果交回去。
return new HubConnectionBuilder()
.withUrl(`${baseUrl}/hubs/admin-leaves`,{
    withCredentials: true
    // withCredentials: true 允許符合瀏覽器規則的 Cookie 隨請求傳送，供後端辨識登入者。
})

// 4. 設定重連，再產生物件
.withAutomaticReconnect()
// 已連線後如果意外斷線，會依預設策略嘗試重連；不包含首次連線失敗的自動重試。

.build()
// 產生連線物件，不會直接連上後端。
}


// return new HubConnectionBuilder() //建立連線設定工具
//   .withUrl('後端網址') //設定連去哪裡
//   .withAutomaticReconnect() //設定斷線重連
//   .build() //產生連線物件