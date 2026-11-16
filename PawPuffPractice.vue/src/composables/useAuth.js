// composables 是 Vue 常見的命名，用來放可以在多個元件共用的組合式功能。
// 例如 useAuth.js 之後會統一管理：
// 目前是否登入
// 目前會員資料
// 登入
// 登出
// 重新整理後還原登入狀態

// | 檔案             | 負責 |
// |---              |---|
// | `LoginView.vue` | 顯示登入表單、錯誤訊息、按鈕 |
// | `useAuth.js`    | 管理整個網站共用的登入狀態與登入功能 |

// 可是AuthControlller.cs 都判斷驗證過了 ?
// 對，真正的帳號密碼判斷與安全驗證，已經由 AuthController.cs 負責。
// useAuth.js 不是再驗證一次密碼，它只是管理 Vue 前端的登入顯示狀態。

// LoginView.vue  → 收集使用者輸入、顯示訊息
// useAuth.js     → 呼叫 API、保存前端會員狀態
// AuthController → 真正驗證帳密、Cookie 與權限

//useAuth.login()只是把資料送過去
//
// 把帳號密碼送到 AuthController
//         ↓
// 等待後端判斷
//         ↓
// 後端回 200，表示登入成功
//         ↓
// 把會員資料放進 currentUser

//為什麼還需要 currentUser？
//後端知道誰登入，不代表 Vue 畫面自動知道。

// Composable 常見結構：
// 1. import：載入需要的工具
// 2. state：建立狀態
// 3. computed：建立衍生狀態
// 4. function：撰寫功能
// 5. export：提供給其他檔案使用

// 1. 匯入 Vue 工具
import{computed,readonly,ref} from 'vue'
// computed 根據其他資料自動計算結果
// readonly 把資料提供給其他元件讀取，但不允許其他元件直接修改

// 2. 匯入 Axios 工具
import api from '@/services/api'

// 3. 建立共用狀態
const currentUser = ref(null)
// Vue 前端目前沒有會員資料

const authInitialized = ref(false)
// 「這次網頁啟動後，向後端確認登入狀態的工作，完成了沒有？」
//
// | `authInitialized` | `currentUser` | 真正意思 |
// |---                |---            |---      |
// | `false`           | `null`        | ❓ 還沒檢查 |
// | `true`            | `null`        | ❌ 檢查完，沒登入 |
// | `true`            | 有 User       | ✅ 檢查完，有登入 |


// interceptors.response.use() 是什麼？
// 所有透過這個 api 發出的請求，收到後端 Response 後，都先經過這裡檢查
// 例如: 
// const response = await api.get('/api/Auth/me')
// 
// 後端回傳 Response = 200 Ok
// 
// → 攔截器 return response
// → 頁面的 response 收到結果
// -------------------------------------
// 後端回傳 Response = 401 Unauthorized
// => 執行錯誤攔截器:
// error => {
//   if (error.response?.status === 401) {
//     currentUser.value = null
//   }

//   return Promise.reject(error)
// }
// 
// → currentUser = null
// → isLoggedIn = false
// → 導覽列改成「註冊、登入」
// 
// 為什麼最後還要 Promise.reject(error)？
// => 攔截器處理完共同工作後，仍然把錯誤交給原本呼叫 API 的地方。
//
//
// 統一檢查所有 Axios API 回應
// 任何 Axios API 回傳 401 時，自動把 Vue 前端的登入狀態清除
//
// 為什麼需要它？
// 假設會員已登入：
// currentUser.value = {
//   id: 52,
//   account: 'ho1999'
// }
// 所以 Vue 畫面認為：isLoggedIn.value === true
// 畫面會顯示：歡迎 ho1999 | 會員資料 |登出
// 接著管理員把這位會員停權。
// 後端檢查到：MySQL Status != 1 
// => 拒絕 Cookie，API 回傳：401 Unauthorized
// 如果前端不處理，可能發生：
// 後端：你已經沒有有效登入身分
// 前端：currentUser 還有會員資料
// 畫面：仍然顯示已登入
//  ===> 回應攔截器就是用來解決這個不同步問題
api.interceptors.response.use(

    // API 成功時，原封不動地把 response 傳回去
    response => {
        return response
    },

    error => {
        // 401 代表目前沒有有效的登入身分
        // 清除 Vue 記憶體裡的會員資料 → 畫面變成未登入
        // 會員下一次呼叫 API 時才會發現並登出 
        // 如果會員停留在頁面上、完全沒有發送 API，前端不會憑空知道資料庫已被改變
        // 
        // 停權後的實際情況
        // 假設會員目前停留在首頁，什麼都沒操作：
        // 管理員停權會員
        // → 會員沒有呼叫 API
        // → 後端還不知道這個瀏覽器正在活動
        // → Vue currentUser 暫時仍有資料
        // 接著會員進入會員資料頁，而該頁執行：
        // await api.get('/api/Auth/me')
        // 這時：
        // Axios 呼叫 API並帶 Cookie
        // → OnValidatePrincipal 查詢 MySQL
        // → 發現 Status != 1
        // → Cookie 被拒絕和刪除
        // → 受保護 API 回傳 401
        // → Axios 攔截器清除 currentUser
        // → 畫面變成未登入
        if(error.response?.status === 401){
            currentUser.value = null
    // 導覽列會變成未登入，但使用者可能仍停留在 /member 等受保護頁面
        }

        // 繼續把錯誤交給原本呼叫 API 的頁面
        // 如果攔截器沒有再次拋出錯誤，原本頁面的 catch 可能收不到錯誤
        // 
        // 攔截器已經處理共同工作
        // → 但這仍然是一個失敗結果
        // → 繼續交給原本頁面的 catch
        return Promise.reject(error)
    }
)



// 4. 計算是否登入
// 根據 currentUser 現在有沒有會員資料，自動算出「目前是否登入」
const isLoggedIn = computed(() => {
    return currentUser.value !== null
})
// currentUser.value = 有會員資料 → true
// => isLoggedIn = true

// currentUser.value = 沒有會員資料 → false
// => isLoggedIn = false

// 為什麼需要 computed()？
// computed() 可以理解成「自動計算欄位」

// () => {} ? 箭頭函式（Arrow Function）
// 可以先把它看成：
// function () {
//   return currentUser.value !== null
// }
// 相當於：
// () => {
//   return currentUser.value !== null
// }
//
// 所以：
// computed(() => {
//   return currentUser.value !== null
// })
// 「Vue，我給你一個計算方法。你幫我根據這個方法維護結果。」
// 根據其他響應式資料（ref、reactive）「計算出」一個新的值。

                    //  |  沒有 `computed()` | 有 `computed()` |
                    //  |---                |---
// | `currentUser` 改變  | 下次呼叫函式時重新算 | Vue 追蹤依賴，結果會隨依賴更新 |



// 6. 初始化登入狀態
// 因為裡面需要等待 API，所以加上：async
async function initializeAuth(){
    // 避免重複檢查
    // 如果已經檢查過，就直接結束
    if(authInitialized.value){
        return
    }
    try{
        // 呼叫後端
        // GET http://localhost:5140/api/Auth/me
        // 並自動攜帶 Cookie
        const response = await api.get(
            '/api/Auth/me'
        )
        // 把後端會員資料保存到 Vue
        currentUser.value = response.data.user
    }
// | 後端結果 | Axios 行為 |
// |---                |---|
// | `400 Bad Request` | 跳到 `catch` |
// | `401 Unauthorized` | 跳到 `catch` |
// | `403 Forbidden` | 跳到 `catch` |
// | `404 Not Found` | 跳到 `catch` |
// | `500 Internal Server Error` | 跳到 `catch` |


    // Cookie 無效
    // /me 最可能是哪個錯誤？ 401 Unauthorized
    // 也就是：後端說目前沒有登入，所以前端把目前會員設成 null。
    catch{
        currentUser.value = null
    }

    // 因爲try{} => 呼叫後端成功後,把後端回傳的會員資料放進 currentUser
    // catch{} => 呼叫後端失敗後,發現沒有"有效 Cookie" => 401 Unauthorized
    //有檢查了 => ture
    finally{
        authInitialized.value = true
    }
}

// 登入功能
//
// 接收帳號密碼
// → 使用 Axios 呼叫後端
// → 後端驗證成功並建立 Cookie
// → 保存會員資料
// → 把後端結果傳回 LoginView
//
//例如 
// 呼叫：login('pawpuff01', 'Password123!')
// 函式裡會得到：
// account === 'pawpuff01'
// password === 'Password123!'
//
// 為什麼加 async？
// 因為呼叫後端需要時間，函式裡會使用：await
// api.post(API路徑,要送給後端的資料)
async function login(account,password){
// api.post 成功
// → response 取得後端結果
// → currentUser 保存會員
// → return 把結果傳回 LoginView
    const response = await api.post('/api/Auth/login',{
        account,
        password
    })

// currentUser.value = {
//   id: 1,
//   account: 'pawpuff01',
//   nickname: '泡芙',
// }
    currentUser.value = response.data.user

// 完整結果傳回:
// {
//   message: '登入成功',
//   user: {
//     id: 1,
//     account: 'pawpuff01',
//     nickname: '泡芙',
//   },
// }
    return response.data

// 失敗時會怎樣？
// 後端回傳：401 Unauthorized
// Axios 會在這一行拋出錯誤：const response = await api.post(...)
// 因此後面兩行不會執行：
// currentUser.value = response.data.user
// return response.data
//
// 沒看到 throw，所以一定往下執行後面兩行 ?
//目前函式沒寫 throw，但它呼叫的函式、Promise 或套件仍可能拋錯或 reject；await 遇到 rejected Promise 時就不會繼續執行下一行。
}


// 登出功能
async function logout(){
    try{
        await api.post('/api/Auth/logout')
    }
    // catch{ } => 非必要
    finally{
        currentUser.value = null
    }
}

// 修改目前登入會員的資料
async function updateProfile(profile){
    // 呼叫後端 PUT /api/Auth/profile
    const response = await api.put(
        '/api/Auth/profile',
        profile
    )

    // 後端修改成功後，更新 Vue 共用的會員資料
    currentUser.value = response.data.user

    // 把後端完整結果交還給呼叫的頁面
    return response.data
}


//async function changePassword(passwordData) 
//函式呼叫：
// await changePassword({
//   currentPassword: form.currentPassword,
//   newPassword: form.newPassword,
// })
//第一個傳入值是整個物件：
// {
//   currentPassword: form.currentPassword,
//   newPassword: form.newPassword,
// }


// api.put(API網址, 要送給後端的資料)
// JavaScript 的函式參數可以接收任何值，包含物件 => 所以password可以接收任何值
async function changePassword(password){
    const response = await api.put(
        '/api/Auth/password',
        password
    )
    return response.data
}
// passwordData 會包含什麼？
// 之後修改密碼頁會這樣呼叫：
// await changePassword({
//     currentPassword: '原本密碼',
//     newPassword: '新的密碼',
//     confirmNewPassword: '再次輸入新密碼'
// })
// 所以 passwordData 是：
// {
//     currentPassword: '原本密碼',
//     newPassword: '新的密碼',
//     confirmNewPassword: '再次輸入新密碼'
// }


// useAuth() 對外提供一個唯讀初始化狀態，以及初始化、登入、登出三個功能。
// 把會員狀態和會員功能整理成一個物件，提供給其他 Vue 元件使用
export function useAuth(){
    return{
        // 回傳物件的屬性名稱 : 這個屬性的值
        currentUser:readonly(currentUser),
        // 元件可以知道目前是否登入
        isLoggedIn,
        // 登入狀態是否已經檢查完成？，但不允許外部直接修改
        authInitialized:readonly(authInitialized),
        // 現在開始檢查登入狀態
        initializeAuth,

        // login 和 logout 是函式，不是 ref 狀態，所以不需要使用 readonly()
        // 外部取得它是為了呼叫：await login(account, password)
        login,
        // 外部取得它是為了呼叫：await logout()
        logout,

        updateProfile,
        changePassword
    }
}
