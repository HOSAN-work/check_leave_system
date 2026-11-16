// 	獨立出來的理由：把查日曆的固定做法放在一處：網址怎麼組、這次請求不帶跨來源憑證、回傳哪部分資料。請假和打卡以後若都需要，就能呼叫同一個 getCalendar(year)。
// 
// 
// 下次開始寫時，先問：「這段是只有這個畫面才需要的邏輯，還是一項可以單獨交代的工作？」
// 1. 只跟這個畫面有關，例如開關請假視窗、顯示載入中 → 留在 .vue
// 2. 多個地方要用，或本身有一套獨立設定，例如建立 SignalR 連線 → 可以拆成 .js。
// 3. 目前只有一個地方用，而且只有幾行 → 先留在 .vue 也沒問題，等真的需要共用再搬。
// 
// 在前端建立「依年份取得日曆」的共用函式
// 
// 要提供什麼？→ 年份
// 要呼叫哪裡？→ /api/calendar/年份
// 要拿到什麼？→ 年度日曆物件
import api from './api'

export async function getCalendar(year){
    const response = await api.get(`/api/calendar/${year}`,{
        withCredentials:false // false -> 跨來源請求不帶登入 Cookie
    })
    return response.data 
}