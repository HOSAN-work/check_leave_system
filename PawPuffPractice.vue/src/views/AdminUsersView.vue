<!-- 
1. 顯示一般會員清單
2. 顯示管理員清單
3. 頁面開啟時，自動向後端取得資料
4. 載入期間顯示「載入中」
5. 發生錯誤時顯示訊息
6. 401 表示未登入
7. 403 表示沒有管理員權限 -->

<!-- 第二步：這頁需要哪些 Vue 功能？
需要保存：
- 一般會員清單
- 管理員清單
- 是否載入中
- 錯誤訊息 -->

<script setup>
// 第三步：需要呼叫後端 API
// onMounted → 頁面元件顯示後執行載入函式
// computed 是「計算屬性」，會根據其他響應式資料自動重新計算
import { ref, onMounted, computed, watch, nextTick } from 'vue'
import { RouterLink } from 'vue-router'

// watch 當指定的響應式資料改變時，自動執行一個函式
// => 搜尋文字、狀態篩選或每頁筆數改變時，自動將頁碼恢復到第 1 頁

import api from '@/services/api'
import { useAuth } from '@/composables/useAuth.js'
import '../assets/drip-button.css'
import '../assets/admin-clock.css'
import '../assets/admin-users.css'
import dashboardPaper from '../assets/dashboard-paper.png'
import dashboardProfile from '../assets/profile.png'
import navClockIcon from '../assets/dashboard-nav-clock.svg'
import navUserIcon from '../assets/dashboard-nav-user.svg'
import navLeaveIcon from '../assets/dashboard-nav-leave.svg'
import navAdminIcon from '../assets/dashboard-nav-admin.svg'
import pagePreviousIcon from '../assets/dashboard-page-prev.svg'
import pageNextIcon from '../assets/dashboard-page-next.svg'
import pageCurrentIcon from '../assets/dashboard-page-current.svg'

const { currentUser, logout } = useAuth()
const accountPage = ref(null)
const activeAccountTab = ref('members')
const lastFocusedElement = ref(null)
const lastFocusedContext = ref(null)
const resultAnnouncement = ref('')

// 建立時間＆最後登入時間正常的ISO時間格式顯示為2026-10-11T00:19:06 -> 不宜使用者看
// 所以新增格式化函式
// value 是呼叫函式時傳進來的時間
function formatDateTime(value) {
  // value 是不是空的？
  if (!value) {
    return ''
  }
  // 原本的 value 只是字串：2026-10-11T00:19:06
  // 轉成 Date 後，JavaScript 才能對它進行日期格式化
  // 不會修改原本的 value，而是建立新的 Date 物件放進 date
  const date = new Date(value)

  // #region 檢查 value 能不能被解析成有效日期
  // 有效日期：const date = new Date('2026-10-11T00:19:06')
  // date.getTime()
  // 會得到一個數字，例如：1791677946000
  //
  // 如果傳入無效內容：const date = new Date('這不是時間')
  // date.getTime()
  // 結果:NaN //Not a Number 「不是有效數字」
  //
  // Number.isNaN(...) = 判斷結果是不是 NaN
  // Number.isNaN(1791677946000) -> false
  // Number.isNaN(NaN) -> true
  //  ===> 如果無法把傳入內容轉成有效日期，就不要繼續格式化，直接顯示「時間格式錯誤」
  //  #endregion
  if (Number.isNaN(date.getTime())) {
    return '時間格式錯誤'
  }
  // NaN => false => 可以格式化
  // 把 date 日期物件，格式化成「台灣常見的日期時間格式」
  // date.toLocaleString() = JavaScript 的日期格式化方法
  // 'zh-TW' 指定使用「繁體中文／台灣」的日期格式
  return date.toLocaleString('zh-TW', {
    year: 'numeric', //顯示完整年份
    month: '2-digit', //月份固定兩位數
    day: '2-digit', //日期固定兩位數
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit', //時、分、秒都顯示兩位數
    hour12: true, //代表使用 12 小時制
  })
}

// 取得目前登入者(管理員)
// 第四步：建立頁面需要的資料
//
// 為什麼初始值是 [] ?
// 因為它們要保存多筆帳號資料，資料型態是陣列
// 會員清單
const members = ref([])
// #region API 回傳後:
// members.value = [
//   {
//     id: 1,
//     account: 'member01',
//     status: 1
//   },
//   {
//     id: 2,
//     account: 'member02',
//     status: 3
//   },
//   {
//     id: 3,
//     account: 'member03',
//     status: 1
//   }
// ]
// #endregion

// 顯示篩選後會員清單
// 流程：
// members：保存所有會員
//        ↓ filter
// filteredMembers：畫面目前要顯示的會員

// 管理員清單
const admins = ref([])

// 管理員標題旁「新增管理員」button
const showCreateAdminForm = ref(false)
// 新增管理員表單資料
// 對應後端 CreateAdminRequestDto的欄位
const createAdminForm = ref({
  account: '',
  password: '',
  confirmPassword: '',
  nickname: '',
  email: '',
  phone: '',
})

// 關閉新增管理員彈窗
function cancelCreateAdminForm() {
  if (isCreatingAdmin.value) {
    return
  }

  // 關閉新增管理員彈窗
  showCreateAdminForm.value = false

  // 重設、清空清單內資料
  resetCreateAdminForm()

  // 清空失敗訊息
  createAdminErrorMessage.value = ''
}

// 是否正在送出新增管理員請求
const isCreatingAdmin = ref(false)
// 新增失敗訊息
const createAdminErrorMessage = ref('')
const createAdminError = ref(null)

watch(createAdminErrorMessage, async (message) => {
  if (message) {
    await nextTick()
    createAdminError.value?.focus()
  }
})

//共用的表單清空函式,清空新增管理員表單
function resetCreateAdminForm() {
  createAdminForm.value = {
    account: '',
    password: '',
    confirmPassword: '',
    nickname: '',
    email: '',
    phone: '',
  }
}

// 新增管理員
async function createAdmin() {
  createAdminErrorMessage.value = ''

  // 清除上一次的全域訊息
  actionMessage.value = ''
  // 為什麼不再把成功訊息放進 createAdminMessage？
  // <p v-if="createAdminMessage"> 位於新增彈窗裡
  // => 彈窗一關閉，這段內容也會被 v-if 移除，使用者看不到成功訊息

  // 檢查兩次密碼是否相同
  if (createAdminForm.value.password !== createAdminForm.value.confirmPassword) {
    createAdminErrorMessage.value = '兩次輸入的密碼不一致'
    return
  }
  //顯示「新增中...」
  //變成不可點擊
  isCreatingAdmin.value = true

  // #region ... 把 createAdminForm.value 物件裡的每一個欄位展開，放進新的物件。
  // {
  //   account: 'admin03',
  //   password: 'Test1234!',
  //   confirmPassword: 'Test1234!',
  //   nickname: '管理員三號',
  //   email: 'admin03@test.com',
  //   phone: '',
  // }
  // #endregion

  try {
    // 建立準備傳給後端的資料
    const requestData = {
      ...createAdminForm.value,
      // 前端 phone: ''
      // 後端 DTO:
      // [StringLength(20, MinimumLength = 10)]
      // public string? Phone { get; set; }
      // ===> 空字串 '' 的長度是 0，可能先被 DTO 判定為不符合「最少 10 個字元」
      // 所以前端轉成：phone: null
      // 後端才會把它當成「沒有提供電話」，跳過長度驗證。
      phone: createAdminForm.value.phone || null,
    }
    const response = await api.post('/api/AdminUsers/admins', requestData)
    // #region 把 requestData 傳給後端的[HttpPost("admins")]CreateAdmin
    // 後端回傳結果:
    // response = {
    //   data: {
    //     message: '管理員新增成功',
    //     admin: {
    //       id: 310,
    //       account: 'admin03',
    //       nickname: '管理員三號',
    //       email: 'admin03@test.com',
    //       status: 1,
    //       role: 'Admin',
    //     },
    //   },
    // }
    // #endregion

    // 把新管理員加入畫面清單
    // 後端剛建立完成的新管理員，加到目前畫面的管理員清單。
    // .push(...) 把一筆資料加到陣列最後面
    // #region  admin: {
    //       id: 310,
    //       account: 'admin03',
    //       nickname: '管理員三號',
    //       email: 'admin03@test.com',
    //       status: 1,
    //       role: 'Admin',
    //     }
    // #endregion
    admins.value.push(response.data.admin)

    // 成功訊息
    //   data: {
    //     message: '管理員新增成功',
    //     admin: {.....}
    // 會顯示在彈窗內
    // createAdminMessage.value = response.data.message ?? '管理員新增成功'

    // 優化：成功訊息顯示在頁面上
    actionMessage.value = response.data.message ?? '管理員新增成功'

    // 新增成功後清空表單
    // createAdminForm.value = {
    //   account: '',
    //   password: '',
    //   confirmPassword: '',
    //   nickname: '',
    //   email: '',
    //   phone: '',
    // }
    resetCreateAdminForm()

    // 優化：新增成功後->自動關閉彈窗 且 成功訊息顯示在頁面上而不是彈窗內
    showCreateAdminForm.value = false
  } catch (error) {
    console.error('新增管理員失敗：', error)

    const validationErrors = error.response?.data?.errors

    // 有 errors → 顯示 DTO 驗證錯誤
    if (validationErrors) {
      createAdminErrorMessage.value = Object.values(validationErrors).flat().join('、')
    }
    // 沒有 errors → 再檢查 message
    // 連 message 都沒有 → 顯示「新增管理員失敗」
    else {
      createAdminErrorMessage.value = error.response?.data?.message ?? '新增管理員失敗'
    }
    // 後端回傳：
    // error = {
    // response: {
    //   data: {
    //     message: '新增管理員失敗',
    //   },
    //  },
    // }
    // ===> '新增管理員失敗' ?? '此帳號已被使用'
  } finally {
    // 顯示「確認新增」
    // 恢復可點擊
    isCreatingAdmin.value = false
  }
}

// 改善「取消新增」：目前直接關閉表單後，再次打開仍會保留先前輸入與錯誤訊息。
function toggleCreateAdminForm() {
  // 假設目前是：showCreateAdminForm.value = true
  // => !true = false => 表單就會關閉
  // 假設目前是：showCreateAdminForm.value = false
  // => !false = true => 表單就會打開
  showCreateAdminForm.value = !showCreateAdminForm.value

  // showCreateAdminForm.value = true 原本表單打開的
  // 執行切換：
  // showCreateAdminForm.value = !showCreateAdminForm.value
  // ==> showCreateAdminForm.value = false 表單關閉了
  // |
  // v
  // ==> if(!false) = if(true)
  // !showCreateAdminForm.value 只是「算出相反結果」，沒有把結果存回 showCreateAdminForm.value
  // 注意：表單仍然是關閉的，沒有重新打開
  //
  // 進入if後 ,==> 表單還是關閉狀態,表單欄位會清空、成功,失敗訊息也清空
  if (!showCreateAdminForm.value) {
    // 表單欄位清空
    resetCreateAdminForm()

    createAdminErrorMessage.value = ''
  }
}

// 建立搜尋文字
// 管理員輸入的會員搜尋文字
const searchKeyword = ref('') //初始值是空字串，表示尚未搜尋
// 目標：
// 沒有輸入關鍵字、選全部 → 顯示全部會員
// 輸入 ho → 顯示帳號、暱稱、Email 或電話包含 ho 的會員
// 輸入 ho，再選狀態 1 → 只顯示符合 ho 而且狀態為 1 的會員

// 目前選擇的會員狀態篩選
// null = 全部會員 \ 1=正常啟用 \ 2~7=不同停用原因
const selectedStatusFilter = ref(null)

// 自動計算 會員狀態＆搜尋文字條件組合
const filteredMembers = computed(() => {
  // 搜尋文字去除前後空白，並統一轉成小寫
  const keyword = searchKeyword.value.trim().toLowerCase()
  return members.value.filter((member) => {
    // #region  為什麼一定要有前面的 === null？
    // const matchesStatus =
    // member.status === selectedStatusFilter.value
    //
    // 選「正常啟用」沒問題： 1 === 1 // true
    // 但選「全部會員」時： member.status === null
    // 假設會員：member.status = 1
    // => 代表這個會員本身的狀態是「正常啟用」
    //
    // 情況一 會員 status = 1，管理員也選「正常啟用」
    // selectedStatusFilter.value = 1
    // 1 === 1 -> true
    //
    // 情況二 會員 status = 1，管理員選「全部會員」
    // selectedStatusFilter.value = null
    // 1 === null -> false
    // ====>想要顯示全部會員卻無法？
    //
    // 所以才需要前面的：selectedStatusFilter.value === null
    // 當選「全部會員」：
    //  null === null || 1 === null
    // true || false = true
    //
    // member.status
    // ＝ 這個會員本身是什麼狀態
    // ＝ 後端會員資料
    //
    // selectedStatusFilter.value
    // ＝ 管理員現在想篩選什麼狀態
    // ＝ 下拉選單目前選擇的值
    // #endregion

    // 第一個條件：會員狀態是否符合
    // 選擇特定狀態 || 沒有選擇特定狀態（顯示全部狀態）
    const matchesStatus =
      // 管理員選「全部會員」
      selectedStatusFilter.value === null ||
      // 會員狀態 === 管理員選擇的狀態
      member.status === selectedStatusFilter.value

    // 第二個條件：搜尋文字是否符合
    // 沒有輸入搜尋文字 || 有輸入搜尋文字
    const matchesKeyword =
      // 搜尋文字為空字串？
      //「或」會員帳號統一轉成小寫後,有包含搜尋文字？
      //「或」會員暱稱統一轉成小寫後,有包含搜尋文字？
      //「或」會員email統一轉成小寫後,有包含搜尋文字？
      //「或」會員電話(可有可無）統一轉成小寫後,有包含搜尋文字？
      // 只要其中一項是true => matchesKeyword = true
      keyword === '' ||
      member.account.toLowerCase().includes(keyword) ||
      member.nickname.toLowerCase().includes(keyword) ||
      member.email.toLowerCase().includes(keyword) ||
      String(member.phone ?? '')
        .toLowerCase()
        .includes(keyword)

    // 狀態和搜尋文字都符合，才保留這位會員
    // 這位會員必須同時符合「狀態篩選」和「搜尋文字」兩個條件，才會保留在 filteredMembers，並顯示在表格上
    return matchesStatus && matchesKeyword

    // #region 四種篩選、搜尋組合狀態
    // 情況一：完全沒有條件
    // 狀態：全部會員、搜尋：空白
    // selectedStatusFilter.value = null
    // keyword = ''
    //
    // matchesStatus = true
    // matchesKeyword = true
    // return true && true  => true => 所有會員都顯示
    // 取代下方的：
    // 顯示全部會員
    // if(selectedStatusFilter.value === null){
    //     return members.value
    // }
    //
    //
    // 情況二：只有搜尋條件
    // 狀態：全部會員、搜尋：ho
    // selectedStatusFilter.value = null
    // keyword = 'ho'
    //
    //
    // matchesStatus = true
    // 搜尋部分根據會員資料決定：member.account.includes('ho')
    // matchesKeyword = true 或 matchesKeyword = false
    //
    // return true && matchesKeyword
    // ==> return true && true = true  顯示
    // ==> return true && false = false  不顯示
    // 所以結果只看搜尋條件
    // 例如：
    // ho1999 → 顯示
    // amy001 → 不顯示
    // ===> 「個別搜尋」
    //
    // 情況三：只有狀態條件
    // 狀態：正常啟用、搜尋：空白
    // selectedStatusFilter.value = 1
    // keyword = ''
    //
    //
    // member.status === 1 => 決定：matchesStatus
    // matchesKeyword = true
    // matchesStatus && true
    // ===> 結果只看狀態條件
    //
    //
    // 情況四：搜尋和狀態同時使用
    // 狀態：正常啟用、搜尋：ho
    //
    //
    // selectedStatusFilter.value = 1
    // keyword = 'ho'
    // 需要同時符合：member.status === 1
    // 而且：會員資料包含 'ho'
    //
    // matchesStatus && matchesKeyword
    // 兩個都符合才顯示
    // #endregion
    // #region 具體會員搜尋例子
    // const member = {
    //   account: 'amy001',
    //   nickname: '小美',
    //   email: 'amy@gmail.com',
    //   phone: '0911222333'
    // }
    // 管理員搜尋：keyword = 'ho'
    //
    // const matchesKeyword =
    //   'ho' === '' ||
    //   'amy001'.includes('ho') ||
    //   '小美'.includes('ho') ||
    //   'amy@gmail.com'.includes('ho') ||
    //   '0911222333'.includes('ho')
    //
    //  false || false || false || false || false
    // => matchesKeyword = false
    //
    // ---------------------------
    // const member = {
    //   account: 'ho1999',
    //   nickname: '小明',
    //   email: 'ho@gmail.com',
    //   phone: ''
    // }
    //
    // 管理員搜尋：keyword = 'ho'
    //
    //  false || true || false || true || false
    // => matchesKeyword = true
    // ----------------------------
    // 例子一：選擇全部會員
    // selectedStatusFilter.value = null
    // member.status = 3
    //
    // const matchesStatus =
    // null === null || 3 === null
    // => true || false = true
    //
    // 例子二：選擇正常啟用
    // selectedStatusFilter.value = 1
    // member.status = 1
    //
    // const matchesStatus =
    // 1 === null || 1 === 1
    // => false || true = true
    //
    // 例子三：選擇正常啟用，但會員是停用狀態
    //  selectedStatusFilter.value = 1
    //  member.status = 3
    //
    //  const matchesStatus =
    // 1 === null || 3 === 1
    // => false || false = false
    //
    //
    // 為什麼能確定是兩個條件同時符合？
    // return matchesStatus && matchesKeyword
    //
    // 完整具體例子一：兩個都符合
    // const member = {
    //   account: 'ho1999',
    //   status: 1
    // }
    // 篩選設定：
    // selectedStatusFilter.value = 1
    // keyword = 'ho'
    //
    // const matchesStatus =
    //  1 === null || 1 === 1
    //  => false || true = true
    //
    // const matchesKeyword =
    // false || true  = true
    //
    // true && true => 狀態與搜尋同時符合,所以會顯示出這位成員
    //  #endregion
  })
})

// 目前顯示第 1 頁，每頁最多顯示 5 位會員
// 目前所在頁數
const currentPage = ref(1)
// 每一頁顯示幾位會員
const pageSize = ref(10)

// 計算總頁數
const totalPages = computed(() => {
  return Math.max(1, Math.ceil(filteredMembers.value.length / pageSize.value))
  // 例如：篩選後有12人,每頁5位：
  // 12 / 5 = 2.4 => Math.ceil(2.4) -無條件進位-> 3 頁
  // Math.max(1,3) = 3
  // page 1 => 5位
  // page 2 => 5位
  // page 3 => 2位
  //
  // Math.ceil() : 天花板
  // Math.ceil(數字) = 只要有小數，向上取整
  // Math.ceil(3.01) = 4
  //
  // Math.floor():只要有小數，向下取整
  // Math.floor(2.2) = 2
  //
  // Math.round():四捨五入
  // Math.round(2.2) = 2
})
const memberPageNumbers = computed(() => {
  const pageCount = Math.min(7, totalPages.value)
  const firstPage = Math.max(
    1,
    Math.min(currentPage.value - 3, totalPages.value - pageCount + 1),
  )

  return Array.from({ length: pageCount }, (_, index) => firstPage + index)
})

function setMemberPage(page) {
  if (page < 1 || page > totalPages.value) {
    return
  }

  currentPage.value = page
}

// 取得目前頁面的會員
const paginatedMembers = computed(() => {
  // 算出目前頁面從陣列哪個位置開始
  const startIndex = (currentPage.value - 1) * pageSize.value
  // #region  篩選後有12人的startIndex
  // 第 1 頁
  // index 0～4
  // A B C D E
  //
  // 第 2 頁
  // index 5～9
  // F G H I J
  //
  // 第 3 頁
  // index 10～11
  // K L
  //
  // 頁數  startIndex
  // -----------------
  // 第 1 頁  → 0
  // 第 2 頁  → 5
  // 第 3 頁  → 10
  // 第 4 頁  → 15
  // 第 5 頁  → 20
  // pageSize.value = 5
  //
  // 為什麼是 (currentPage - 1)？
  // 假設現在：currentPage.value = 1
  // startIndex 是：0
  // #endregion

  // 例如：目前所在頁數 = 1
  // startIndex = (1-1) * 5 = 0 -> 第一頁第一筆索引
  // endIndex = 0 + 5 = 5
  //
  // slice(開始位置, 停止位置) : 停止位置本身不會被拿
  // return filteredMembers.value.slice(0,5)
  // 取得：[0,1 ,2 ,3 ,4 ] -> 取得第一頁內五筆的索引
  //
  // 例如：目前所在頁數 = 2
  // startIndex = (2-1) * 5 = 5 -> 第二頁第一筆索引
  // endIndex = 5 + 5 = 10
  // slice(5,10) => [5,6,7,8,9] -> 取得第二頁內五筆的索引

  // 算出要切到哪個位置為止，但不包含這個位置
  const endIndex = startIndex + pageSize.value
  // endIndex = 0 + 5 = 5

  // 原本從filteredMembers取出目前這頁的資料,排序後從sortedMembers
  // slice(開始位置, 停止位置) : 停止位置本身不會被拿
  return sortedMembers.value.slice(startIndex, endIndex)
})

// #region 避免目前頁碼超出總頁數
// 假設目前有 6 位正常會員
// 篩選條件：只顯示正常啟用會員
// 每頁顯示：5 位
//
// 第 1 頁：會員 A、B、C、D、E
// 第 2 頁：會員 F
// currentPage.value = 2, totalPages.value = 2
// 管理員正在第 2 頁操作會員 F
//
// 管理員停用會員 F
// 會員 F 原本：member.status = 1
//
// 停用後：member.status = 3
// 目前篩選條件是：只顯示 status = 1
// 所以會員 F 不再符合條件，會從 filteredMembers 消失
// 現在只剩：會員 A、B、C、D、E
// => 5 位剛好全部放在第 1 頁
// => totalPages.value = 1
// => 第 2 頁已經不存在
//
// *** 問題是 currentPage 不會自己改 ***
// totalPages.value 從 2 變成 1
// 但是currentPage.value仍然是2
// Vue 不會自己猜測：總頁數變成 1，所以目前頁數也應該改成 1。
//
// 因此分頁計算仍然使用：currentPage.value = 2
// 計算：
// startIndex = (2 - 1) * 5 = 5
// endIndex = 5 + 5 = 10
// filteredMembers.value.slice(5,10) = [5,6,7,8,9]
// -> 只有會員F [5] -> 停用 -> [] -> 空表格
// => currentPage 還殘留舊值 2
// => 增加第二個watch 是把錯誤的舊頁碼修正
//
// 現在應該要是 totalPages.value = 2 -> 1
// startIndex = (1-1) * 5 = 0
// endIndex = 0 + 5 = 5
// filteredMembers.value.slice(0,5)=[0,1,2,3,4]
// #endregion

// 總頁數縮小時修正頁碼
// 會員資料本身改變後，如果目前頁面已經不存在，就回到目前最後一頁
// watch(
//   要監控的資料,
//   新值 => {
//     // 資料改變後，Vue 把最新值放進「新值」
//   }
// )
watch(totalPages, (newTotalPages) => {
  // 假如：目前頁面第6頁 > 共5頁
  // currentPage.value = 6
  // totalPages.value = 6 -> 5
  //
  // watch 偵測到 totalPages 改變後，
  // 自動把它的新值 5 傳給回呼函式
  // ===> newTotalPages = 5
  // if(6 > 5){ currentPage.value = 5}
  if (currentPage.value > newTotalPages) {
    currentPage.value = newTotalPages
  }
})

//「會員清單排序」
// 目前依照哪個欄位排序,初始用id欄位排序
const sortField = ref('id')
// 排序方向： 初始是asc
// asc是從小到大、desc是從大到小
const sortDirection = ref('asc')

// 建立排序後的清單
const sortedMembers = computed(() => {
  // 複製篩選後的會員清單,避免直接改動原本陣列
  // ... 是展開運算子 意思是把陣列裡面的元素逐一展開
  const copiedMembers = [...filteredMembers.value]

  // #region numbers.sort((a, b) => a - b) //從小到大排序（升冪 asc）
  // sort() 的規則：
  // | 回傳結果 | 意思             |
  // |---      |---              |
  // | 負數     | a 排在 b 前面    |
  // | 正數     | a 排在 b 後面    |
  // | 0       | 維持原本的相對順序 |
  // const numbers = [8, 2, 6, 1]
  // numbers.sort((a, b) => a - b)
  //
  // 情況一 ： a=8,b=2
  // => numbers.sort((8,2) => 6)
  // => 正數 => [2,8]
  //
  // 情況二 ： a=2,b=8
  // => numbers.sort((2,8) => -6)
  // => 負數 => [2,8]
  //
  // 不管哪種情況，比較小的數字都會排在前面
  // => [1, 2, 6, 8]
  // ---------------------------------------
  // numbers.sort((a, b) => b - a) //從大到小 desc
  //  情況一 ： a=8,b=2
  // => numbers.sort((2,8) => -6)
  // => 負數 => [8,2]
  //
  //  情況二 ： a=2,b=8
  // => numbers.sort((2,8) => 6)
  // => 正數 => [8,2]
  // #endregion
  // 三種情況仍然需要理解排序演算法
  // 演算法面試：公司可能要求你手寫 Quick Sort、Merge Sort，考察你的程式邏輯。
  // 效能優化：當資料量非常大時，你需要理解排序的時間與記憶體成本。
  // 底層系統開發：如果你正在開發程式語言、資料處理函式庫或特殊系統，可能需要自行設計或選擇排序演算法。

  copiedMembers.sort((a, b) => {
    // 回傳 -1 => a在b前面
    // 回傳  1 => a在b後面
    // 回傳  0 => 順序不交換

    const valueA = a[sortField.value]
    const valueB = b[sortField.value]

    if (valueA < valueB) {
      // a-b 負數 => a在b前面 => 從小到大 => asc
      // 'asc' === 'asc' = true ? -1 : 1 => -1
      //
      // a-b 正數 => a在b後面 => desc
      // 'desc' === 'asc' =false ? -1 : 1 => 1
      return sortDirection.value === 'asc' ? -1 : 1
    }

    if (valueA > valueB) {
      // a-b 負數 => a在b前面 => 從小到大 => asc
      // 'asc' === 'asc' = true ? 1 : -1 => 1
      //
      // a-b 正數 => a在b後面 => desc
      // 'desc' === 'asc' =false ? 1 : -1 => -1
      return sortDirection.value === 'asc' ? 1 : -1
    }

    return 0
  })

  return copiedMembers
  // sort()排序完後,「排序完成的整份會員清單」
  // sortedMembers.value 得到排序後的會員清單
})

// 搜尋、篩選、每頁頁數、排序欄位或排序方向改變時回到第一頁
// 例如原本在第 3 頁，接著輸入搜尋文字，結果可能只剩一頁
// 只要搜尋文字改變，就把目前頁碼改回第 1 頁
// 例如原本在第 3 頁，選擇「點數由大到小」,就把目前頁碼改回第 1 頁
watch([searchKeyword, selectedStatusFilter, pageSize, sortField, sortDirection], () => {
  currentPage.value = 1
})

// 清除篩選條件
function clearMemberFilters() {
  // 清除搜尋框
  searchKeyword.value = ''
  // 恢復顯示全部會員
  selectedStatusFilter.value = null
}

const isLoading = ref(false) //清單尚未載入
const errorMessage = ref('')

// 建立操作狀態
// actionMessage：操作成功訊息
// actionErrorMessage：操作失敗訊息
// 共用功能：修改會員狀態、修改管理員狀態
const actionMessage = ref('')
const actionErrorMessage = ref('')

// 管理「停用／啟用」操作：目前正在修改哪位會員
const updatingMemberId = ref(null)

// 管理「停用／啟用」操作：目前正在修改哪一位管理員
const updatingAdminId = ref(null)

// #region 建立會員狀態選項 => 讓 Vue 使用後端選項
// const statusOptions = [
//     {value:1,label:'正常啟用'},
//     {value:2,label:'會員自行申請停用'},
//     {value:3,label:'違反平台使用規範'},
//     {value:4,label:'疑似詐騙行為'},
//     {value:5,label:'騷擾其他會員'},
//     {value:6,label:'發布不當內容'},
//     {value:7,label:'帳號安全異常'},
// ]
// #endregion
const statusOptions = ref([])
// 寫在函式裡 → 只有該函式可以使用
// 寫在 <script setup> 的最外層 → 整個 Vue 元件都能使用

// 會員停用狀態：排除正常啟用 1
const memberDisabledOptions = computed(() =>
  statusOptions.value.filter((option) => option.value !== 1),
)

// 建立管理員狀態選項
const adminStatusOptions = ref([])

// 管理員停用狀態：排除正常啟用 1
const adminDisabledOptions = computed(() =>
  adminStatusOptions.value.filter((option) => option.value !== 1),
)

// 目前選到哪一個會員 → 保存準備停用的會員
const selectedMember = ref(null)
// 目前正在查詳細資料的會員 → 保存正在查看詳細資料的會員
const selectedDetailMember = ref(null)

// 目前選到哪一個管理員 → 保存準備變更狀態的管理員
const selectedAdmin = ref(null)

// 管理員選擇的新狀態
const selectedAdminStatus = ref(null)

// 目前正在查詳細資料的管理員 → 保存正在查看詳細資料的管理員
const selectedDetailAdmin = ref(null)

// 關閉管理員詳細資料彈窗
function cancelDetailAdmin() {
  selectedDetailAdmin.value = null
}

// 修改管理員資料
//
// 目前正在編輯的管理員: 記住目前點了哪一位管理員的「編輯」
const selectedEditAdmin = ref(null)

// 編輯管理員表單
const editAdminForm = ref({
  nickname: '',
  email: '',
  phone: '',
})

// 是否正在送出修改管理員資料的請求: 請求期間防止重複送出
// 初始沒有送出：false
const isUpdatingAdminProfile = ref(false)

// 修改失敗訊息
const editAdminErrorMessage = ref('')
const editAdminError = ref(null)

watch(editAdminErrorMessage, async (message) => {
  if (message) {
    await nextTick()
    editAdminError.value?.focus()
  }
})

//「按下編輯時，開啟並填入原資料」
function openEditAdmin(admin) {
  // 記住目前要編輯哪一位管理員
  selectedEditAdmin.value = admin
  // 將管理員原本的資料放入表單
  editAdminForm.value = {
    nickname: admin.nickname ?? '',
    email: admin.email ?? '',
    phone: admin.phone ?? '',
  }
  // 清除上一次操作留下的訊息
  editAdminErrorMessage.value = ''
}
// 取消編輯
function cancelEditAdmin() {
  // 清除目前正在編輯的管理員，讓編輯區域隱藏
  selectedEditAdmin.value = null

  // 清空表單內容
  editAdminForm.value = {
    nickname: '',
    email: '',
    phone: '',
  }

  // 清除失敗訊息
  editAdminErrorMessage.value = ''
}

// 更新->編輯管理員資料
async function updateAdminProfile() {
  const admin = selectedEditAdmin.value
  if (!admin) {
    editAdminErrorMessage.value = '找不到要修改的管理員'
    return
  }
  // requestData 是準備送給後端 PATCH API 的資料物件
  // 決定修改哪些欄位及其新值
  const requestData = {}

  // 表單裡的暱稱(管理員修改過了) != 原本的暱稱 => 表單裡的暱稱存入requestData
  if (editAdminForm.value.nickname !== admin.nickname) {
    requestData.nickname = editAdminForm.value.nickname
  }
  // 表單裡的Email(管理員修改過了) != 原本的Email => 表單裡的Email存入requestData
  if (editAdminForm.value.email !== admin.email) {
    requestData.email = editAdminForm.value.email
  }
  // 原本的phone有沒有?
  //  有 => true => originalPhone = admin.phone
  // 沒有 => false => originalPhone = ''
  const originalPhone = admin.phone ?? ''

  // 表單裡的phone(管理員修改過了) != 原本的phone
  // => 表單裡的phone存入requestData
  if (editAdminForm.value.phone !== originalPhone) {
    requestData.phone = editAdminForm.value.phone
  }
  // 檢查物件有沒有任何欄位
  // requestData = {} => length === 0 -> 沒有修改任何資料
  if (Object.keys(requestData).length === 0) {
    editAdminErrorMessage.value = '沒有資料需要修改'
    return
  }

  // length > 0
  // editAdminMessage.value = ''
  actionMessage.value = ''
  editAdminErrorMessage.value = ''
  isUpdatingAdminProfile.value = true //正在送出

  try {
    const response = await api.patch(`/api/AdminUsers/admins/${admin.id}`, requestData)

    // 使用後端回傳的最新資料，更新清單中的管理員
    // Object.assign(目標物件, 來源物件)
    // Object.assign(
    //   admin,                // 要被更新的表格物件
    //   response.data.admin,  // 後端最新資料
    // )
    Object.assign(admin, response.data.admin)
    // 同步更新表單內容
    editAdminForm.value = {
      nickname: response.data.admin.nickname ?? '',
      email: response.data.admin.email ?? '',
      phone: response.data.admin.phone ?? '',
    }

    // editAdminMessage.value = response.data.message ?? '管理員資料修改成功'
    // 將成功訊息顯示在頁面上方
    actionMessage.value = response.data.message ?? '管理員資料修改成功'

    // 關閉編輯彈窗並清空表單
    cancelEditAdmin()

    // error 是自己取的變數名稱，JavaScript 不會因為單數或複數而改變行為
    //
    // console.error() 是 JavaScript 用來把「錯誤訊息」輸出到瀏覽器開發者工具 Console 的方法
    // 用途主要是給開發者除錯，它不會自動把訊息顯示在網頁畫面上
  } catch (error) {
    // #region console.error(第一個值, 第二個值)
    // 第一個值 → '修改管理員資料失敗：'
    // 第二個值 → error
    // 也就是：
    // console.error(
    //   '修改管理員資料失敗：', // 第一個值
    //   error,               // 第二個值
    // )
    //
    // 括號裡可以放任何 JavaScript 的「值」，不只物件屬性。
    // 例如:
    // console.error('文字')          // 字串
    // console.error(404)             // 數字
    // console.error(true)            // 布林值
    // console.error(error)           // 變數
    // console.error(admin.id)        // 物件屬性
    // console.error(requestData)     // 物件
    // 也可以一次放多個值：
    // console.error(
    //   '修改管理員資料失敗：',
    //   error,
    //   requestData,
    //   admin.id,
    // )
    // #endregion
    console.error('修改管理員資料失敗：', error)

    const responseData = error.response?.data

    // #region Object.values(responseData.errors)
    // [
    //   ['Email格式不正確'],
    //   ['電話必須是10個字元'],
    // ]
    // ==> 再執行.flat()
    // [
    //   'Email格式不正確',
    //   '電話必須是10個字元',
    // ]
    // ==> [0] = 'Email格式不正確' = validationMessage
    // ----------------
    // 如果後端沒有 errors = null = validationMessage
    // #endregion
    const validationMessage = responseData?.errors
      ? Object.values(responseData.errors).flat()[0]
      : null

    // 準備給「網頁使用者」看的
    // 後端一般錯誤訊息：responseData?.message
    // DTO 欄位驗證訊息：validationMessage = responseData?.errors整理出來的第一條訊息
    // 都沒有時使用預設訊息：'修改管理員資料失敗'
    //
    // 優先順序：
    // 1.後端一般錯誤訊息 2.DTO 欄位驗證訊息 3.都沒有時使用預設訊息
    editAdminErrorMessage.value = responseData?.message ?? validationMessage ?? '修改管理員資料失敗'
  } finally {
    isUpdatingAdminProfile.value = false //改為未送出,恢復按鈕
  }
}

// 會員選擇的新狀態
// const selectedStatus = ref(2)
const selectedStatus = ref(null)
// 因為現在還沒呼叫 API，不應先假設第一個選項一定是 2

function openSuspendMember(member) {
  selectedStatus.value = ''
  actionErrorMessage.value = ''
  selectedMember.value = member
}

function cancelSuspendMember() {
  selectedMember.value = null
  selectedStatus.value = ''
  actionErrorMessage.value = ''
}

// 確認停用會員
// 會員停用要選原因
// #region 流程  selectedMember 取得會員
// → selectedStatus 取得下拉選單的停用原因
// → 檢查有沒有會員
// → 檢查有沒有選原因
// → 顯示會員帳號及停用原因
// → 呼叫 updateMemberStatus()
// → 成功後關閉選擇區域
// #endregion → 重設停用原因
async function confirmSuspendMember() {
  // 取得目前正在操作的會員
  const member = selectedMember.value

  // 取得下拉選單選擇的停用狀態
  // 假設 ：newStatus = 2
  const newStatus = selectedStatus.value

  // 如果沒有會員資料，就停止執行
  if (!member) {
    actionErrorMessage.value = '找不到要停用的會員'
    return
  }
  // 如果沒有選擇停用狀態，就停止執行
  if (!newStatus) {
    actionErrorMessage.value = '請先選擇停用原因'
    return
  }

  // 將數字狀態轉換成中文原因
  // 假設：statusText = getStatusTest(2)
  // statusText = '會員自行申請停用'
  const statusText = getStatusText(newStatus)

  // 這個確認已經由目前開啟的 Figma 樣式 dialog 完成，
  // 不再另外呼叫 window.confirm()，避免跳出作業系統原生灰色視窗。
  actionMessage.value = `正在停用會員「${member.account}」（${statusText}）`

  // 使用者按「確認停用」後，才呼叫後端 API。
  // succeeded = true
  const succeeded = await updateMemberStatus(member, newStatus)

  // 呼叫 API 失敗
  // succeeded = false
  // 停止執行
  if (!succeeded) {
    return
  }
  selectedMember.value = null //恢復初始值

  // statusOptions[0]= 1：正常啟用
  // memberDisabledOptions[0] = 2：會員自行申請停用 -> 預設選項
  selectedStatus.value = ''
  // 這樣後端即使將第一個停用狀態改成其他數字，前端也不用跟著修改。
}

// 恢復會員
async function enableMember(member) {
  const confirmed = window.confirm(`確定要恢復會員「${member.account}」的帳號嗎？`)

  if (!confirmed) {
    return
  }

  await updateMemberStatus(member, 1)
}

// 第五步：建立載入函式
async function loadUsers() {
  // 第六步：開始載入前的狀態
  errorMessage.value = ''
  // 告訴畫面目前正在載入
  isLoading.value = true

  // 用Promise.All() 共同呼叫三支API
  // 第七步：呼叫一般會員 API
  try {
    const [statusOptionsResponse, adminStatusOptionsResponse, membersResponse, adminsResponse] =
      await Promise.all([
        api.get('/api/AdminUsers/member-status-options'),
        api.get('/api/AdminUsers/admin-status-options'),
        api.get('/api/AdminUsers/members'),
        api.get('/api/AdminUsers/admins'),
      ])

    // 使用Promise.all()取代
    // 先從後端取得停用原因
    // const statusOptionsResponse = await api.get(
    //     '/api/AdminUsers/status-options'
    // )

    // [HttpGet("member-status-options")]：取得所有會員狀態
    statusOptions.value = statusOptionsResponse.data.statusOptions ?? []

    // [HttpGet("admin-status-options")]：取得所有管理員狀態
    adminStatusOptions.value = adminStatusOptionsResponse.data.adminStatusOptions ?? []

    // statusOptions[0]= 1：正常啟用
    // memberDisabledOptions[0] = 2：會員自行申請停用 -> 預設選項
    selectedStatus.value = memberDisabledOptions.value[0]?.value ?? null

    // 使用Promise.all()取代
    // 取得一般會員
    // const membersResponse = await api.get(
    //     '/api/AdminUsers/members'
    // )
    // 後端可能回傳：
    // {
    //   "members": [
    //     {
    //       "id": 1,
    //       "account": "ho01",
    //       "role": "Member"
    //     }
    //   ]
    // }
    //  也就是membersResponse.data
    // 再往下一層 : membersResponse.data.members
    //
    // [
    //   {
    //     id: 1,
    //     account: 'ho01',
    //     role: 'Member',
    //   },
    // ]

    members.value = membersResponse.data.members ?? []

    // 使用Promise.all()取代
    // 取得管理員
    // const adminsResponse = await api.get(
    //     '/api/AdminUsers/admins'
    // )

    admins.value = adminsResponse.data.admins ?? []
  } catch (error) {
    console.error('取得帳號清單失敗：', error)

    if (error.response?.status === 403) {
      errorMessage.value = '你沒有管理員權限'
    } else if (error.response?.status === 401) {
      errorMessage.value = '請先登入'
    } else {
      errorMessage.value = error.response?.data?.message ?? '取得帳號清單失敗'
    }
  } finally {
    isLoading.value = false
  }
}

// 建立會員更新狀態
// (member, newStatus) => (要修改哪一位會員資料, 要改成哪個狀態)
async function updateMemberStatus(member, newStatus) {
  // 清除上一次的訊息
  actionMessage.value = ''
  actionErrorMessage.value = ''
  // 記住目前正在修改哪位會員
  updatingMemberId.value = member.id

  // 假設會員目前 Status = 1
  // → 操作按鈕顯示「停用」
  // → 點擊「停用」
  // → 顯示 2～7 的停用原因下拉選單
  // → 選擇一個原因
  // → 按「確認停用」
  // → 跳出確認視窗
  // → 顯示會員、停用原因
  // → 再按確認
  // → 呼叫後端 API
  // → 修改成功
  // → Status 變成 2～7
  // → 操作按鈕改為「啟用」
  try {
    const response = await api.put(`/api/AdminUsers/members/${member.id}/status`, {
      status: newStatus,
    })
    // 不必重新下載整份清單，直接更新這一列
    member.status = response.data.member.status
    // 顯示成功訊息
    actionMessage.value = response.data.message ?? '會員狀態修改成功'

    return true // updateMemberStatus = true

    // 假設後端回傳：
    // {
    //   "message": "會員已停權",
    //   "member": {
    //     "id": 52,
    //     "status": 2
    //   }
    // }
    // response.data.member.status = 2
    // 等同：member.status = 2
    //
    //actionMessage.value = "會員已停權" ?? "會員狀態修改成功"
  } catch (error) {
    console.error('修改會員狀態失敗：', error)

    // 取得 DTO 驗證錯誤
    const validationErrors = error.response?.data?.errors

    // 假設你送出：
    // {
    //   "status": 9
    // }
    // 但是 DTO 限定：[Range(1, 7)]
    // 後端會回傳：
    // {
    //   "errors": {
    //     "Status": ["帳號狀態必須介於1到7"]
    //   }
    // }
    // error.response.data.errors
    // 會取得：
    // {
    //   Status: ['帳號狀態必須介於1到7']
    // }

    // 處理 DTO 欄位驗證錯誤
    if (validationErrors) {
      actionErrorMessage.value = Object.values(validationErrors).flat().join('、')

      // Object.values(validationErrors)
      // = 把一個物件裡面的「值」全部拿出來，組成一個陣列。
      // 原本：
      // {
      //   Status: ['帳號狀態必須介於1到7']
      // }
      // 會變成：
      // [
      //   ['帳號狀態必須介於1到7']
      // ]
      //
      //.flat() :把雙層陣列攤平成單層陣列
      //結果：
      // [
      //   '帳號狀態必須介於 1 到 7'
      // ]
      //
      //.join('、') :把陣列裡的多段文字連成一個字串，並在中間放入頓號
      //結果： '帳號狀態必須介於 1 到 7'
      //
      //----------------------
      // 如果有多個欄位：
      // {
      //   Status: ['狀態錯誤'],
      //   Id: ['會員編號錯誤'],
      // }
      // 則變成：
      // [
      //   ['狀態錯誤'],
      //   ['會員編號錯誤'],
      // ]
      //
      //.flat() =>
      // [
      //   '狀態錯誤',
      //   '會員編號錯誤',
      // ]
      //
      // .join('、') =>
      // '狀態錯誤、會員編號錯誤'
    }
    // 處理一般錯誤訊息
    else {
      actionErrorMessage.value = error.response?.data?.message ?? '修改會員狀態失敗'

      // 假設會員不存在，後端回傳：
      // return NotFound(new
      // {
      //     message = "找不到一般會員"
      // });
      // 它沒有：data.errors
      // 所以：validationErrors === undefined
      // if 不成立，進入 else
      //
      // actionErrorMessage.value =
      //  '找不到一般會員' ?? '修改會員狀態失敗'
    }

    return false // updateMemberStatus = false
  } finally {
    updatingMemberId.value = null
  }
}

// 開啟管理員狀態 -> 變更表單
function disableAdmin(admin) {
  // 記住要修改哪位管理員
  // 例如：admin = admin001
  // selectedAdmin.value = admin001
  selectedAdmin.value = admin

  // 預設選擇第一個停用狀態
  // adminDisabledOptions.value[0] ={value: 8,label:'離職',}
  // selectedAdminStatus.value = 8 ?? null
  selectedAdminStatus.value = adminDisabledOptions.value[0]?.value ?? null

  // 清除上次操作訊息
  actionMessage.value = ''
  actionErrorMessage.value = ''
}
// 管理員狀態 -> 確認變更
async function confirmDisableAdmin() {
  // 取出目前準備修改的管理員
  const admin = selectedAdmin.value
  // 目前準備修改管理員為空
  if (!admin) {
    actionErrorMessage.value = '找不到要修改的管理員'
    return
  }
  // 代表下拉選單沒有選擇新的狀態
  if (selectedAdminStatus.value == null) {
    actionErrorMessage.value = '請選擇管理員狀態'
    return
  }
  // selectedAdminStatus.value !== null
  // 真正呼叫後端，修改管理員狀態
  // updateAdminStatus(哪位管理員, 新狀態)
  const success = await updateAdminStatus(admin, selectedAdminStatus.value)
  // 例如：updateAdminStatus(admin, 8)
  // 為什麼需要 await？ => 裡面會呼叫 API，需要等待後端回應
  // 呼叫後端->等待後端完成->取得 true 或 false->存進 success

  // API 修改成功才關閉表單
  if (success) {
    // 目前沒有選擇管理員、沒有選擇新的狀態
    selectedAdmin.value = null
    selectedAdminStatus.value = null
  }
}

// 管理員狀態 -> 取消變更
function cancelDisableAdmin() {
  // 清除目前選擇的管理員
  selectedAdmin.value = null
  // 清除選擇的新狀態
  selectedAdminStatus.value = null
  // 清除錯誤訊息
  actionErrorMessage.value = ''
}

// 啟用管理員：先開啟專案自己的 Figma 確認視窗，不使用瀏覽器 window.confirm。
function enableAdmin(admin) {
  selectedAdmin.value = admin
  selectedAdminStatus.value = 1
  actionMessage.value = ''
  actionErrorMessage.value = ''
}

// 建立管理員更新狀態
// updateAdminStatus(哪位管理員, 新狀態)
async function updateAdminStatus(admin, newStatus) {
  // 清除上一次的訊息
  actionMessage.value = ''
  actionErrorMessage.value = ''
  // 記住目前正在修改哪位管理員
  updatingAdminId.value = admin.id

  try {
    const response = await api.put(`/api/AdminUsers/admins/${admin.id}/status`, {
      status: newStatus,
    })
    // 不必重新下載整份清單，直接更新這一列
    admin.status = response.data.admin.status
    // 顯示成功訊息
    actionMessage.value = response.data.message ?? '管理員狀態修改成功'

    return true // updateAdminStatus = true
  } catch (error) {
    console.error('修改管理員狀態失敗：', error)

    // 取得 DTO 驗證錯誤
    const validationErrors = error.response?.data?.errors

    // 處理 DTO 欄位驗證錯誤
    if (validationErrors) {
      actionErrorMessage.value = Object.values(validationErrors).flat().join('、')
    } else {
      actionErrorMessage.value = error.response?.data?.message ?? '修改管理員狀態失敗'
    }

    return false // updateAdminStatus = false
  } finally {
    updatingAdminId.value = null
  }
}

// 管理員搜尋文字
const adminSearchKeyword = ref('')

// 可以搭配「啟用／停用」狀態篩選
// null=全部管理員 、1=正常啟用、2=已停用
const selectedAdminStatusFilter = ref(null)

// 根據搜尋文字 + 狀態條件 -> 自動計算要顯示的管理員
const filteredAdmins = computed(() => {
  const keyword = adminSearchKeyword.value.trim().toLowerCase()
  // .filter() JavaScript 陣列的方法，會逐一檢查陣列中的每個項目，用來「篩選資料」
  return admins.value.filter((admin) => {
    // #region  是否符合目前選擇的狀態
    // 例如選擇「全部管理員」= null :
    // selectedAdminStatusFilter.value = null === null
    // || admin.status === 1\2\3\...
    // null === null || 1\2\3\... === null
    //  true || false = true
    //
    // 假設管理員狀態是 2：
    //  null === null || 2 === null
    //  true || false = true
    //
    // const matchesStatus =
    // 選擇「全部管理員」 || 管理員狀態等於選擇的狀態
    // #endregion
    const matchesStatus =
      selectedAdminStatusFilter.value === null || admin.status === selectedAdminStatusFilter.value

    // #region 是否符合搜尋文字
    // 假設搜尋： keyword = 'admin'
    // 正在檢查的管理員是：
    // {
    //   account: 'admin001',
    //   nickname: '系統管理員',
    //   email: 'admin001@test.com',
    //   phone: null
    // }
    //
    // 'admin' === ''
    // || 'admin001'.includes(admin)
    // || '系統管理員'.includes(admin)
    // || 'admin001@test.com'.includes(admin)
    //
    // || String(null ?? '').includes(admin)
    // = || String('').includes(admin)
    //
    // false || true || true || false = true
    // #endregion
    const matchesKeyword =
      keyword === '' ||
      admin.account.toLowerCase().includes(keyword) ||
      admin.nickname.toLowerCase().includes(keyword) ||
      admin.email.toLowerCase().includes(keyword) ||
      String(admin.phone ?? '')
        .toLowerCase()
        .includes(keyword)

    // 狀態與搜尋文字必須同時符合
    return matchesStatus && matchesKeyword
    // #region 三種組合情況
    // 1. 選擇全部管理員
    // null === null = true
    // 2. 選擇正常啟用
    // admin.status === 1
    // 3. 選擇已停用
    // admin.status === 2
    // #endregion
  })
})

const adminCurrentPage = ref(1)
const adminPageSize = 10
const adminTotalPages = computed(() =>
  Math.max(1, Math.ceil(filteredAdmins.value.length / adminPageSize)),
)
const adminPageNumbers = computed(() => {
  const pageCount = Math.min(7, adminTotalPages.value)
  const firstPage = Math.max(
    1,
    Math.min(adminCurrentPage.value - 3, adminTotalPages.value - pageCount + 1),
  )

  return Array.from({ length: pageCount }, (_, index) => firstPage + index)
})
const paginatedAdmins = computed(() => {
  const startIndex = (adminCurrentPage.value - 1) * adminPageSize
  return filteredAdmins.value.slice(startIndex, startIndex + adminPageSize)
})

watch([adminSearchKeyword, selectedAdminStatusFilter], () => {
  adminCurrentPage.value = 1
})
watch(adminTotalPages, (newTotalPages) => {
  if (adminCurrentPage.value > newTotalPages) {
    adminCurrentPage.value = newTotalPages
  }
})

watch(
  [
    activeAccountTab,
    filteredMembers,
    filteredAdmins,
    currentPage,
    adminCurrentPage,
    totalPages,
    adminTotalPages,
    sortField,
    sortDirection,
    isLoading,
    errorMessage,
  ],
  (_, __, onCleanup) => {
    if (isLoading.value) {
      resultAnnouncement.value = '帳號資料載入中。'
      return
    }

    if (errorMessage.value) {
      resultAnnouncement.value = ''
      return
    }

    const timer = window.setTimeout(() => {
      if (activeAccountTab.value === 'admins') {
        const count = filteredAdmins.value.length
        resultAnnouncement.value =
          count === 0
            ? '沒有符合篩選條件的管理員。'
            : `顯示 ${count} 位管理員，第 ${adminCurrentPage.value} 頁，共 ${adminTotalPages.value} 頁。`
        return
      }

      const count = filteredMembers.value.length
      if (count === 0) {
        resultAnnouncement.value = '沒有符合篩選條件的會員。'
        return
      }

      const sortLabels = {
        id: '會員編號',
        account: '帳號',
        pointBalance: '點數',
        createdAt: '註冊時間',
      }
      const directionLabel = sortDirection.value === 'asc' ? '由小到大' : '由大到小'
      resultAnnouncement.value = `依${sortLabels[sortField.value]}${directionLabel}排序；顯示 ${count} 位會員，第 ${currentPage.value} 頁，共 ${totalPages.value} 頁。`
    }, 500)

    onCleanup(() => window.clearTimeout(timer))
  },
  { immediate: true, flush: 'post' },
)

function setAdminPage(page) {
  if (page < 1 || page > adminTotalPages.value) {
    return
  }

  adminCurrentPage.value = page
}

// 清除管理員搜尋與狀態篩選
function clearAdminFilters() {
  adminSearchKeyword.value = ''
  selectedAdminStatusFilter.value = null
}

function switchAccountTab() {
  activeAccountTab.value = activeAccountTab.value === 'members' ? 'admins' : 'members'
  currentPage.value = 1
  adminCurrentPage.value = 1
  selectedMember.value = null
  selectedDetailMember.value = null
  selectedAdmin.value = null
  selectedEditAdmin.value = null
  selectedDetailAdmin.value = null
  showCreateAdminForm.value = false
  actionErrorMessage.value = ''
}

const isAnyDialogOpen = computed(
  () =>
    showCreateAdminForm.value ||
    Boolean(selectedMember.value) ||
    Boolean(selectedDetailMember.value) ||
    Boolean(selectedAdmin.value) ||
    Boolean(selectedEditAdmin.value) ||
    Boolean(selectedDetailAdmin.value),
)

watch(
  isAnyDialogOpen,
  async (isOpen, wasOpen) => {
  if (isOpen) {
    await nextTick()
    const dialog = accountPage.value?.querySelector('.account-modal-backdrop')
    if (dialog instanceof HTMLDialogElement && !dialog.open) {
      dialog.showModal()
    }
  } else if (wasOpen) {
    const rowAction = lastFocusedContext.value?.isConnected
      ? lastFocusedContext.value.querySelector('button:not(:disabled)')
      : null
    const fallbackAction = accountPage.value?.querySelector(
      '.account-table tbody tr button:not(:disabled)',
    )
    const trigger = lastFocusedElement.value
    const focusTarget = trigger instanceof HTMLElement && trigger.isConnected
      ? trigger
      : (rowAction ?? fallbackAction ?? accountPage.value?.querySelector('#account-main'))
    focusTarget?.focus()
    lastFocusedElement.value = null
    lastFocusedContext.value = null
  }
  },
  { flush: 'post' },
)

function captureDialogTrigger(event) {
  if (isAnyDialogOpen.value || !(event.target instanceof Element)) {
    return
  }

  const trigger = event.target.closest('button')
  if (!trigger) {
    return
  }

  lastFocusedElement.value = trigger
  lastFocusedContext.value = trigger.closest('tr')
}

function isDialogBusy() {
  return (
    (showCreateAdminForm.value && isCreatingAdmin.value) ||
    (selectedMember.value && updatingMemberId.value === selectedMember.value.id) ||
    (selectedAdmin.value && updatingAdminId.value === selectedAdmin.value.id) ||
    (selectedEditAdmin.value && isUpdatingAdminProfile.value)
  )
}

function handleDialogCancel() {
  if (!isDialogBusy()) {
    closeActiveDialog()
  }
}

function handleDialogBackdropClick() {
  if (!isDialogBusy()) {
    closeActiveDialog()
  }
}

function closeActiveDialog() {
  if (showCreateAdminForm.value) {
    cancelCreateAdminForm()
  } else if (selectedMember.value) {
    cancelSuspendMember()
  } else if (selectedDetailMember.value) {
    selectedDetailMember.value = null
  } else if (selectedAdmin.value) {
    cancelDisableAdmin()
  } else if (selectedEditAdmin.value) {
    cancelEditAdmin()
  } else if (selectedDetailAdmin.value) {
    cancelDetailAdmin()
  }
}

// 將會員狀態數字轉成文字
// #region 例如：
// getStatusText(1) → 正常啟用
// getStatusText(3) → 違反使用規範
//
//  假設： getStatusText(2)
//  statusOptions.find(option => option.value === 2)
//  find => {value:2,label:'會員自行申請停用'}
// return label = '會員自行申請停用'
// #endregion
function getStatusText(status) {
  if (status === 1) {
    return '正常啟用'
  }
  // 因為 statusOptions 現在是 ref，在 JavaScript 裡必須透過 .value 取得陣列
  const statusOption = statusOptions.value.find((option) => option.value === status)

  // statusOptions.find() 是什麼？
  // .find() 會從陣列中，由前往後尋找符合條件的第一筆資料
  // 假設：status === 3
  // 它會依序檢查：{ value: 2, label: '會員自行申請停用' }
  // 檢查：2 === 3 → 不符合，繼續找下一筆
  // 檢查：{ value: 3, label: '違反使用規範' }
  // → 3 === 3 → 符合，回傳這一筆資料
  //
  // option => option.value === status
  // 完整寫法是：
  // function (option) {
  //   return option.value === status
  // }
  return statusOption ? statusOption.label : `未知狀態（${status})`
  // statusOption 有找到資料嗎？
  // 有找到  → 傳回 statusOption.label
  // 沒有找到 → 傳回「未知狀態（狀態數字）」
  // 如果 status 是 9，沒有找到選項：
  // statusOption === undefined
  // 就會傳回：`未知狀態（${status}）` 結果：未知狀態（9）
}
// 將管理員狀態數字轉成文字
// #region 例如後端傳來：admin.status = 8
// getAdminStatusTest(8) -> 離職
// adminStatusOptions.value.find((option) => option.value === 8)
// find => {value:8,label:'離職'}
// ===> adminstatusOption = true
// return adminstatusOption.label = '離職'
// #endregion
function getAdminStatusText(status) {
  const adminstatusOption = adminStatusOptions.value.find((option) => option.value === status)

  return adminstatusOption ? adminstatusOption.label : `未知管理員狀態(${status})`
}

// 為什麼放在 onMounted？
// 這個頁面的需求是：使用者進入管理員帳號頁時，自動看到會員和管理員清單。
// 這個管理員頁面的 Vue 元件顯示完成後，自動執行一次 loadUsers()，向後端取得會員清單
// 可以直接寫：onMounted(loadUsers)
onMounted(() => {
  loadUsers()
})
// #region onMounted vs loadUsers() 兩種寫法區別
// 一、使用 onMounted
// onMounted(() => {
//   loadUsers()
// })
// 建立響應式資料
// → 建立初始畫面
// → 元件掛載
// → 開始取得會員資料
//
// 二、直接呼叫
// loadUsers()
// 建立響應式資料
// → 立即開始取得會員資料
// → Vue 同時繼續建立畫面
//
// 區分兩件事:
// 第一件事
// 什麼時候開始 loadUsers？
// onMounted(loadUsers) // 掛載後開始
// 或
// loadUsers() // setup 執行時開始
//
// 第二件事
// members 和 admins 是否同時請求？
// 依序：
// await members
// await admins
// 同時：
// await Promise.all([
//   members請求,
//   admins請求,
// ])
//
//為什麼一開始選 onMounted？
// 1. 明確表示這是頁面的初始化動作
// onMounted(loadUsers)
// 這不是使用者點按鈕後才執行；而是進入此頁時自動執行一次。
// 2. 離開再進入時會重新載入
// 進入管理員頁：元件掛載 → loadUsers()
// 離開後再回來：元件重新掛載 → 再次 loadUsers()
// => 取得比較新的帳號清單
// 3. 這是常見的 Vue 教學寫法
// 把「頁面進入後取得資料」寫在：onMounted(...)
// 這樣容易將程式分成：
// 建立狀態
// 建立函式
// 頁面掛載後執行初始化
// #endregion
</script>

<!-- 頁面有三種主要情況：
1. 正在載入
2. 載入失敗
3. 載入成功，顯示清單 -->
<template>
  <div
    ref="accountPage"
    class="admin-clock-page admin-users-page"
    @click.capture="captureDialogTrigger"
  >
    <a class="dashboard-skip-link" href="#account-main">跳到主要內容</a>
    <aside class="dashboard-sidebar">
      <div class="dashboard-avatar" aria-hidden="true">
        <img :src="dashboardProfile" alt="" />
      </div>
      <nav class="dashboard-nav" aria-label="主要導覽">
        <RouterLink class="dashboard-nav-link" to="/admin/clock">
          <img :src="navClockIcon" alt="" aria-hidden="true" />
          <span>打卡功能</span>
        </RouterLink>
        <RouterLink class="dashboard-nav-link" to="/member">
          <img :src="navUserIcon" alt="" aria-hidden="true" />
          <span>使用者中心</span>
        </RouterLink>
        <RouterLink class="dashboard-nav-link" to="/admin/leave">
          <img :src="navLeaveIcon" alt="" aria-hidden="true" />
          <span>請假功能</span>
        </RouterLink>
        <RouterLink
          class="dashboard-nav-link is-section-active"
          aria-current="page"
          to="/admin/users"
        >
          <img :src="navAdminIcon" alt="" aria-hidden="true" />
          <span>帳號管理</span>
        </RouterLink>
      </nav>
      <div class="dashboard-logout-wrap drip-button-wrap">
        <span class="drip-button-drips" aria-hidden="true"></span>
        <button class="dashboard-logout drip-button" type="button" @click="logout">登出</button>
      </div>
    </aside>

    <main id="account-main" class="dashboard-paper" tabindex="-1">
      <img class="dashboard-paper-texture" :src="dashboardPaper" alt="" aria-hidden="true" />
      <div class="dashboard-content account-content">
        <header class="account-page-heading">
          <h1>帳號管理</h1>
          <button
            class="comic-button account-mode-toggle"
            type="button"
            aria-label="切換會員與管理員清單"
            :aria-pressed="activeAccountTab === 'admins'"
            @click="switchAccountTab"
          >
            {{ activeAccountTab === 'members' ? '管理員' : '會員' }}
          </button>
        </header>

        <p class="visually-hidden" role="status" aria-live="polite" aria-atomic="true">
          {{ actionMessage || (isLoading ? '帳號資料載入中...' : '') }}
        </p>
        <p class="visually-hidden" role="alert" aria-live="assertive" aria-atomic="true">
          {{ actionErrorMessage || createAdminErrorMessage || editAdminErrorMessage || errorMessage }}
        </p>
        <p class="visually-hidden" role="status" aria-live="polite" aria-atomic="true">
          {{ resultAnnouncement }}
        </p>

        <p v-if="actionMessage" class="success account-feedback">
          {{ actionMessage }}
        </p>

        <p v-if="actionErrorMessage" class="error account-feedback">
          {{ actionErrorMessage }}
        </p>

        <!-- isLoading = true -->
        <p v-if="isLoading" class="account-state">帳號資料載入中...</p>

        <!-- isLoading = false => 載入程序已經結束，
    而且 errorMessage 有內容→ 載入失敗 -->
        <!-- errorMessage = 'xxxxx' => true => {{ xxxxx }} -->
        <p v-else-if="errorMessage" class="error account-state">
          {{ errorMessage }}
        </p>

        <!-- isLoading = false => 載入程序已經結束，
    而且 errorMessage = '' => false -->
        <!-- 載入成功後的會員清單放這 -->
        <!-- ----------會員---------- -->
        <template v-else>
          <section v-if="activeAccountTab === 'members'" class="account-panel">
            <h2>一般會員</h2>

            <!-- 會員搜尋條件 -->
            <div class="account-filter-row">
              <div class="account-control">
                <label for="member-search">搜尋會員</label>

        <!-- v-model.trim 是什麼？
            v-model.trim="searchKeyword"
            v-model 把輸入框文字保存進：searchKeyword.value
            .trim 會移除文字前後的空白 -->
                <input
                  id="member-search"
                  v-model.trim="searchKeyword"
                  class="account-input"
                  type="search"
                  placeholder="請輸入帳號、暱稱、Email或電話"
                />
              </div>

              <div class="account-control">
                <label for="member-status-filter">篩選會員狀態</label>

                <select
                  id="member-status-filter"
                  v-model="selectedStatusFilter"
                  class="account-select"
                >
                  <option :value="null">全部會員</option>
                  <option v-for="option in statusOptions" :key="option.value" :value="option.value">
                    {{ option.label }}
                  </option>
                </select>
              </div>

              <button
                v-if="searchKeyword !== '' || selectedStatusFilter !== null"
                class="comic-button account-clear-button"
                type="button"
                @click="clearMemberFilters"
              >
                清除篩選
              </button>
            </div>

            <div class="account-table-meta">
              <p v-if="members.length > 0">
                顯示 {{ filteredMembers.length }} 位，共 {{ members.length }} 位會員
              </p>
            </div>
        <!-- 例如總共有 10 位會員，篩選後剩 3 位：
            顯示 3 位，共 10 位會員-->

        <!-- 完整會員清單是空的 → 顯示「目前沒有一般會員」 -->
        <p v-if="members.length === 0">目前沒有一般會員</p>
        <!-- 完整清單有會員，但篩選結果是空的 → 顯示「目前沒有符合篩選條件的會員」 -->
        <p v-else-if="filteredMembers.length === 0">目前沒有符合篩選條件的會員</p>

        <div class="account-sort-controls">
          <label for="sort-field"> 排序欄位 </label>
          <select id="sort-field" v-model="sortField">
            <option value="id">會員編號</option>
            <option value="account">帳號</option>
            <option value="pointBalance">點數</option>
            <option value="createdAt">註冊時間</option>
          </select>

          <label for="sort-direction"> 排序方向 </label>
          <select id="sort-direction" v-model="sortDirection">
            <option value="asc">由小到大</option>
            <option value="desc">由大到小</option>
          </select>
        </div>

        <!-- members.length > 0 -->
        <!-- filteredMembers.length > 0 -->
        <!-- 加入排序選單後, 
        <div>排序選單</div>
        Vue 會疑惑：
        <table v-else>
        否則什麼？
        前一個元素是普通 div，沒有 if！ => 因此報錯 -->

        <!-- thead 表頭
                tr 表格的一橫列
                th 標題格

                tbody 資料
                td 資料格 -->
        <div
          v-if="filteredMembers.length > 0"
          class="account-table-scroll"
          tabindex="0"
          role="region"
          aria-label="一般會員清單，可左右捲動"
        >
          <table class="account-table">
            <caption class="visually-hidden">一般會員帳號清單</caption>
            <thead>
            <tr>
              <th
                scope="col"
                :aria-sort="sortField === 'id' ? (sortDirection === 'asc' ? 'ascending' : 'descending') : undefined"
              >編號</th>
              <th
                scope="col"
                :aria-sort="sortField === 'account' ? (sortDirection === 'asc' ? 'ascending' : 'descending') : undefined"
              >帳號</th>
              <th scope="col">暱稱</th>
              <th scope="col">Email</th>
              <th
                scope="col"
                :aria-sort="sortField === 'pointBalance' ? (sortDirection === 'asc' ? 'ascending' : 'descending') : undefined"
              >點數</th>
              <th scope="col">狀態</th>
              <th scope="col">角色</th>
              <th scope="col">操作</th>
            </tr>
            </thead>

            <tbody>
            <!-- 把members裡面的東西一個、一個拿出來 
                    每拿一個出來就建立一次 <tr> -->
            <!-- :key 告訴Vue 這一個 <tr> 是哪一個會員的 -->
            <!-- v-for 在適當情況下提供唯一 key，讓更新、重排、刪除時可以正確追蹤每一項 -->
            <tr v-for="member in paginatedMembers" :key="member.id">
              <th scope="row">{{ member.id }}</th>
              <td>{{ member.account }}</td>
              <td>{{ member.nickname }}</td>
              <td>{{ member.email }}</td>
              <td>{{ member.pointBalance }}</td>
              <td>{{ getStatusText(member.status) }}</td>
              <td>{{ member.role }}</td>

              <!-- 
                        假設：
                        {
                        id: 52,
                        account: 'ho1999',
                        status: 1
                        } -->
              <!-- 假設正在更新會員 52：updatingMemberId.value = 52 -->
              <!-- 目前這一列也是會員 52：
                        member.id = 52 -->
              <!-- @click 是 v-on:click的縮寫 
                        意思是使用者點擊「停用」按鈕時，把這一列的會員資料放進 selectedMember。-->
              <!-- 例如點擊會員 52 的停用按鈕之前：
                        selectedMember.value = null
                        點擊之後：
                        selectedMember.value = 
                        {
                        id: 52,
                        account: 'ho1999',
                        status: 1
                        } -->
              <td>
                <!-- 會員目前的狀態等於1，才顯示這顆「停用」按鈕 -->
                <!-- 按下「停用」
                        → selectedMember 保存這位會員
                        → 顯示停用確認區域
                        → 選擇停用原因
                        → 按下「確認停用」
                        → confirmSuspendMember()
                        → updateMemberStatus()
                        → 呼叫後端 API
                        → 修改 MySQL -->

                <button
                  type="button"
                  :aria-label="`查看會員 ${member.account}`"
                  @click="selectedDetailMember = member"
                >
                  查看
                </button>

                <!-- 正常會員：先選取這位會員 -->
                <button
                  v-if="member.status === 1"
                  type="button"
                  :disabled="updatingMemberId === member.id"
                  :aria-label="`停用會員 ${member.account}`"
                  @click="openSuspendMember(member)"
                >
                  停用
                </button>
                <!-- else 會員目前的狀態!=1，才顯示這顆「啟用」按鈕 -->
                <!-- 點擊「啟用」時，把目前這一列的會員傳給 enableMember()  -->
                <!-- 假設正在更新會員 52： 
                        updatingMemberId = 52
                        member.id = 52
                        true  => 顯示：處理中...
                        false => 顯示：啟用 -->

                <!-- 已停用會員：恢復帳號 -->
                <button
                  v-else
                  type="button"
                  :disabled="updatingMemberId === member.id"
                  :aria-label="`${updatingMemberId === member.id ? '處理中' : '啟用'}會員 ${member.account}`"
                  @click="enableMember(member)"
                >
                  {{ updatingMemberId === member.id ? '處理中...' : '啟用' }}
                </button>
              </td>
            </tr>
            </tbody>
          </table>
        </div>

        <!-- 會員詳細資料 -->
        <dialog
          v-if="selectedDetailMember"
          class="account-modal-backdrop"
          aria-labelledby="member-detail-title"
          @cancel.prevent="handleDialogCancel"
          @click.self="handleDialogBackdropClick"
        >
          <section class="account-modal-window account-modal-window--member-detail">
            <h2 id="member-detail-title">會員詳細資料</h2>
            <div
              class="account-detail-scroll"
              tabindex="0"
              role="region"
              aria-label="會員詳細資料表，可左右捲動"
            >
              <table class="account-detail-table">
                <caption class="visually-hidden">會員詳細資料</caption>
                <thead>
                  <tr>
                    <th scope="col">編號</th>
                    <th scope="col">帳號</th>
                    <th scope="col">暱稱</th>
                    <th scope="col">Email</th>
                    <th scope="col">電話</th>
                    <th scope="col">點數</th>
                    <th scope="col">狀態</th>
                    <th scope="col">角色</th>
                    <th scope="col">建立時間</th>
                    <th scope="col">最後登入</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td>{{ selectedDetailMember.id }}</td>
                    <td>{{ selectedDetailMember.account }}</td>
                    <td>{{ selectedDetailMember.nickname }}</td>
                    <td>{{ selectedDetailMember.email }}</td>
                    <td>{{ selectedDetailMember.phone || '未填寫' }}</td>
                    <td>{{ selectedDetailMember.pointBalance }}</td>
                    <td>{{ getStatusText(selectedDetailMember.status) }}</td>
                    <td>{{ selectedDetailMember.role }}</td>
                    <td>
                      {{
                        selectedDetailMember.createdAt
                          ? formatDateTime(selectedDetailMember.createdAt)
                          : '無建立時間'
                      }}
                    </td>
                    <td>
                      {{
                        selectedDetailMember.lastLoginAt
                          ? formatDateTime(selectedDetailMember.lastLoginAt)
                          : '尚未登入'
                      }}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <button
              class="comic-button account-button-secondary"
              type="button"
              @click="selectedDetailMember = null"
            >
              關閉
            </button>
          </section>
        </dialog>

        <!-- 分頁控制區 -->
        <nav v-if="filteredMembers.length > 0" class="account-pagination" aria-label="會員清單分頁">
          <button
            class="account-page-arrow"
            type="button"
            aria-label="上一頁"
            :disabled="currentPage === 1"
            @click="setMemberPage(currentPage - 1)"
          >
            <img :src="pagePreviousIcon" alt="" aria-hidden="true" />
          </button>
          <ol>
            <li v-for="page in memberPageNumbers" :key="page">
              <button
                type="button"
                :aria-label="`第 ${page} 頁`"
                :aria-current="page === currentPage ? 'page' : undefined"
                :class="{ 'is-current': page === currentPage }"
                @click="setMemberPage(page)"
              >
                <img v-if="page === currentPage" :src="pageCurrentIcon" alt="" aria-hidden="true" />
                <span>{{ page }}</span>
              </button>
            </li>
          </ol>
          <button
            class="account-page-arrow"
            type="button"
            aria-label="下一頁"
            :disabled="currentPage >= totalPages"
            @click="setMemberPage(currentPage + 1)"
          >
            <img :src="pageNextIcon" alt="" aria-hidden="true" />
          </button>
          <span class="visually-hidden">第 {{ currentPage }} 頁，共 {{ totalPages }} 頁</span>
        </nav>

        <!-- 停用確認區域 -->
        <dialog
          v-if="selectedMember"
          class="account-modal-backdrop"
          aria-labelledby="suspend-member-title"
          @cancel.prevent="handleDialogCancel"
          @click.self="handleDialogBackdropClick"
        >
          <section class="account-modal-window account-modal-window--suspend-member">
            <h2 id="suspend-member-title">停用會員</h2>

            <p class="account-modal-subtitle">會員：{{ selectedMember.account }}</p>

            <form @submit.prevent="confirmSuspendMember">
              <div>
                <label for="suspend-status">停用原因</label>
          <!-- label的for對應select的id,所以自行取名稱-->
          <!-- 為什麼使用 .number？
                表單取得的值有時會是字串：'3'
                但後端 DTO 的 Status 需要數字：
                {
                "status": 3
                }
                加入 .number：
                Vue 會嘗試把值轉成數字：'3' → 3 -->

          <!-- v-model 是跟 JavaScript 變數綁定
                假設： const selectedStatus = ref(2) 
                那：<select v-model="selectedStatus"> 
                代表：
                <select> 目前的值
                        ⇅
                selectedStatus -->
            <select
              id="suspend-status"
              class="account-select"
              v-model.number="selectedStatus"
              required
              :aria-describedby="`suspend-status-help${actionErrorMessage ? ' suspend-status-error' : ''}`"
              :aria-invalid="actionErrorMessage && !selectedStatus ? 'true' : undefined"
            >
            <option value="" disabled>請選擇停用原因</option>
            <!-- key 是給 Vue 使用的，用來辨認每個由 v-for 產生的選項 -->
            <!-- value 是表單選項真正代表的資料  -->
            <!-- 如果只有 key、沒有 value
                => 瀏覽器通常會把顯示文字當作選項值
                => selectedStatus.value = '違反使用規範'
                可是後端需要：
                {
                "status": 3
                }
                而不是：
                {
                "status": "違反使用規範"
                }
                -->

            <!-- statusOptions
      ↓
{ value: 3, label: '違反使用規範' }
      ↓
option.value
      ↓
3
      ↓
:value="option.value"
      ↓
<option> 代表的值 = 3
      ↓
管理員選這個 option
      ↓
v-model.number="selectedStatus"
這句就是告訴 Vue：
「這個 <select> 選到什麼 value，就把那個 value 存進 selectedStatus。」
所以現在選到：<option value="3">
      ↓
selectedStatus.value = 3 -->

            <option
              v-for="option in memberDisabledOptions"
              :key="option.value"
              :value="option.value"
            >
              {{ option.label }}
            </option>
            </select>
              </div>

            <p id="suspend-status-help" class="account-field-help">
              請選擇一項停用原因。
            </p>
            <p v-if="actionErrorMessage" id="suspend-status-error" class="error" role="alert">
              {{ actionErrorMessage }}
            </p>

          <!-- v-for 建立每位會員的表格列
        ↓
點擊 Bob 那一列的「停用」
        ↓
當下的 member 就是 Bob
        ↓
selectedMember = member
        ↓
selectedMember.value 保存 Bob
        ↓
執行 confirmSuspendMember()
        ↓
const member = selectedMember.value
        ↓
取回 Bob -->
            <div class="account-modal-actions">
              <button
                class="comic-button account-button-secondary"
                type="button"
                :disabled="updatingMemberId === selectedMember.id"
                @click="cancelSuspendMember"
              >
                取消
              </button>
              <button
                class="comic-button account-button-primary"
                type="submit"
                :disabled="updatingMemberId === selectedMember.id"
              >
                {{ updatingMemberId === selectedMember.id ? '處理中...' : '確認停用' }}
              </button>
            </div>
            </form>
          </section>
        </dialog>
          </section>

      <!-- ------------管理員------------ -->
      <!-- 管理員篩選條件 -->
      <section v-if="activeAccountTab === 'admins'" class="account-panel">
        <div class="account-section-heading">
          <h2>管理員</h2>

          <p v-if="admins.length > 0" class="account-count">
            顯示 {{ filteredAdmins.length }} 位，共 {{ admins.length }} 位管理員
          </p>
        </div>
        <div class="account-filter-row account-filter-row--admins">
          <div class="account-control">
            <label for="admin-search">搜尋管理員</label>
            <input
              id="admin-search"
              v-model.trim="adminSearchKeyword"
              class="account-input"
              type="search"
              placeholder="請輸入帳號、暱稱、Email或電話"
            />
          </div>

          <div class="account-control">
            <label for="admin-status-filter">篩選管理員狀態</label>
            <!-- :value="null" 
            => selectedAdminStatusFilter.value = null -->
            <select
              id="admin-status-filter"
              v-model="selectedAdminStatusFilter"
              class="account-select"
            >
              <option :value="null">全部管理員</option>
              <!-- 假設：
              option = { value: 8, label: '離職',}
              :key="option.value" → key 是 8 
              key：告訴 Vue「這個選項是誰」
              ----------------
              :value="option.value" → 選取值是 8 
              value：告訴程式「使用者選了什麼」
              {{ option.label }}  → 畫面顯示「離職」-->
              <option
                v-for="option in adminStatusOptions"
                :key="option.value"
                :value="option.value"
              >
                {{ option.label }}
              </option>
            </select>
          </div>

          <button
            class="comic-button account-clear-button"
            type="button"
            @click="clearAdminFilters"
            :disabled="adminSearchKeyword === '' && selectedAdminStatusFilter === null"
          >
            清除篩選
          </button>

          <!-- showCreateAdminForm = !showCreateAdminForm 
        初始false = 不顯示表單 
        => 按下新增後 
        => showCreateAdminForm.value = true 
        => 顯示表單 -->
          <!-- 原本false -> @click= !false = true -> 顯示表單
        => button 顯示"取消新增"-->
          <!-- 原本true -> @click= !true = false -> 隱藏表單
        => button 顯示"新增管理員" 
        ===> 由"toggleCreateAdminForm" 優化-->
          <!-- 防止 API 正在送出時，使用者按「取消新增」把表單關閉  => 加上:disabled="isCreatingAdmin"
          isCreatingAdmin=true => button停用 -->
          <button
            class="comic-button account-create-button"
            type="button"
            :disabled="isCreatingAdmin"
            @click="toggleCreateAdminForm"
          >
            {{ showCreateAdminForm ? '取消新增' : '新增管理員' }}
          </button>
        </div>
      </section>
      <!-- ------新增管理員表單 => 彈窗------ -->
      <dialog
        v-if="showCreateAdminForm"
        class="account-modal-backdrop"
        aria-labelledby="create-admin-title"
        @cancel.prevent="handleDialogCancel"
        @click.self="handleDialogBackdropClick"
      >
        <section class="account-modal-window account-modal-window--create-admin">
          <h2 id="create-admin-title">新增管理員</h2>

          <form @submit.prevent="createAdmin">
            <div>
              <label for="admin-account">帳號</label>
              <input
                id="admin-account"
                v-model.trim="createAdminForm.account"
                type="text"
                minlength="6"
                maxlength="20"
                autocomplete="username"
                required
              />
            </div>
            <div>
              <label for="admin-password">密碼</label>

              <input
                id="admin-password"
                v-model="createAdminForm.password"
                type="password"
                minlength="8"
                maxlength="100"
                autocomplete="new-password"
                required
              />
            </div>

            <div>
              <label for="admin-confirm-password">確認密碼</label>

              <input
                id="admin-confirm-password"
                v-model="createAdminForm.confirmPassword"
                type="password"
                minlength="8"
                maxlength="100"
                autocomplete="new-password"
                :aria-invalid="createAdminErrorMessage === '兩次輸入的密碼不一致' ? 'true' : undefined"
                :aria-describedby="createAdminErrorMessage === '兩次輸入的密碼不一致' ? 'create-admin-error' : undefined"
                required
              />
            </div>
            <div>
              <label for="admin-nickname">暱稱</label>

              <input
                id="admin-nickname"
                v-model.trim="createAdminForm.nickname"
                type="text"
                maxlength="100"
                autocomplete="nickname"
                required
              />
            </div>

            <div>
              <label for="admin-email">Email</label>

              <input
                id="admin-email"
                v-model.trim="createAdminForm.email"
                type="email"
                autocomplete="email"
                required
              />
            </div>

            <div>
              <label for="admin-phone">電話（選填）</label>

              <input
                id="admin-phone"
                v-model.trim="createAdminForm.phone"
                type="tel"
                inputmode="numeric"
                pattern="[0-9]{10}"
                minlength="10"
                maxlength="10"
                autocomplete="tel"
                aria-describedby="admin-phone-help"
              />
            </div>

            <p id="admin-phone-help" class="account-field-help">請輸入 10 位數字。</p>

            <p
              v-if="createAdminErrorMessage"
              id="create-admin-error"
              ref="createAdminError"
              class="error"
              tabindex="-1"
            >
              {{ createAdminErrorMessage }}
            </p>

            <!-- :disabled="isCreatingAdmin"
            當 API 正在新增管理員時，按鈕會被禁用，避免重複送出。 -->

            <div class="account-modal-actions">
              <button
                class="comic-button account-button-primary"
                type="submit"
                :disabled="isCreatingAdmin"
              >
                {{ isCreatingAdmin ? '新增中...' : '確認新增' }}
              </button>
              <button
                class="comic-button account-button-secondary"
                type="button"
                :disabled="isCreatingAdmin"
                @click="cancelCreateAdminForm"
              >
                取消
              </button>
            </div>
          </form>
        </section>
      </dialog>

      <p
        v-if="activeAccountTab === 'admins' && admins.length === 0"
        class="account-state"
      >
        目前沒有管理員
      </p>
      <p
        v-else-if="activeAccountTab === 'admins' && filteredAdmins.length === 0"
        class="account-state"
      >
        目前沒有符合篩選條件的管理員
      </p>

      <div
        v-if="activeAccountTab === 'admins' && filteredAdmins.length > 0"
        class="account-table-scroll"
        tabindex="0"
        role="region"
        aria-label="管理員清單，可左右捲動"
      >
        <table class="account-table account-table--admins">
          <caption class="visually-hidden">管理員帳號清單</caption>
          <thead>
          <tr>
            <th scope="col">編號</th>
            <th scope="col">帳號</th>
            <th scope="col">暱稱</th>
            <th scope="col">Email</th>
            <th scope="col">狀態</th>
            <th scope="col">角色</th>
            <th scope="col">查看</th>
            <th scope="col">操作</th>
          </tr>
          </thead>
          <tbody>
          <tr v-for="admin in paginatedAdmins" :key="admin.id">
            <th scope="row">{{ admin.id }}</th>
            <td>{{ admin.account }}</td>
            <td>{{ admin.nickname }}</td>
            <td>{{ admin.email }}</td>
            <td>{{ getAdminStatusText(admin.status) }}</td>
            <td>{{ admin.role }}</td>
            <td>
              <button
                type="button"
                :aria-label="`查看管理員 ${admin.account}`"
                @click="selectedDetailAdmin = admin"
              >
                查看
              </button>
            </td>

            <td>
              <!-- 目前登入者：不能在管理員清單操作自己 -->
              <span v-if="currentUser?.id === admin.id"> 目前登入帳號 </span>

              <!-- 其他管理員 -->
              <template v-else>
                <!-- 編輯button -> 視窗用CSS寫的 -->
                <!-- showModal() 是讓瀏覽器開啟原生模態視窗。 -->
                <button
                  type="button"
                  :disabled="isUpdatingAdminProfile"
                  :aria-label="`編輯管理員 ${admin.account}`"
                  @click="openEditAdmin(admin)"
                >
                  編輯
                </button>
                <!-- 情況一：其他管理員，而且目前是啓用狀態 -->
                <button
                  v-if="admin.status === 1"
                  type="button"
                  :disabled="updatingAdminId === admin.id"
                  :aria-label="`${updatingAdminId === admin.id ? '處理中' : '停用'}管理員 ${admin.account}`"
                  @click="disableAdmin(admin)"
                >
                  {{ updatingAdminId === admin.id ? '處理中...' : '停用' }}
                </button>
                <!-- 情況二：其他管理員，而且目前是停用狀態 -->
                <button
                  v-else
                  type="button"
                  :disabled="updatingAdminId === admin.id"
                  :aria-label="`${updatingAdminId === admin.id ? '處理中' : '啟用'}管理員 ${admin.account}`"
                  @click="enableAdmin(admin)"
                >
                  {{ updatingAdminId === admin.id ? '處理中...' : '啟用' }}
                </button>
              </template>
            </td>
          </tr>
          </tbody>
        </table>
      </div>
      <nav
        v-if="activeAccountTab === 'admins' && filteredAdmins.length > 0"
        class="account-pagination"
        aria-label="管理員清單分頁"
      >
        <button
          class="account-page-arrow"
          type="button"
          aria-label="上一頁"
          :disabled="adminCurrentPage === 1"
          @click="setAdminPage(adminCurrentPage - 1)"
        >
          <img :src="pagePreviousIcon" alt="" aria-hidden="true" />
        </button>
        <ol>
          <li v-for="page in adminPageNumbers" :key="page">
            <button
              type="button"
              :aria-label="`第 ${page} 頁`"
              :aria-current="page === adminCurrentPage ? 'page' : undefined"
              :class="{ 'is-current': page === adminCurrentPage }"
              @click="setAdminPage(page)"
            >
              <img
                v-if="page === adminCurrentPage"
                :src="pageCurrentIcon"
                alt=""
                aria-hidden="true"
              />
              <span>{{ page }}</span>
            </button>
          </li>
        </ol>
        <button
          class="account-page-arrow"
          type="button"
          aria-label="下一頁"
          :disabled="adminCurrentPage >= adminTotalPages"
          @click="setAdminPage(adminCurrentPage + 1)"
        >
          <img :src="pageNextIcon" alt="" aria-hidden="true" />
        </button>
        <span class="visually-hidden">
          第 {{ adminCurrentPage }} 頁，共 {{ adminTotalPages }} 頁
        </span>
      </nav>
      <!-- ------變更管理員狀態 : 彈出視窗 ------ -->
      <!-- 初始 selectedAdmin.value = null -> 不顯示 
                selectedAdmin.value !== null -> 顯示表單 -->
      <!-- class="modal-backdrop" => 覆蓋整個畫面的半透明背景 -->
      <!-- class="modal-window" => 中間真正顯示內容的視窗 -->
      <!-- v-if="selectedAdmin" => 只有選到管理員時才建立視窗 -->
      <!-- @click.self="cancelDisableAdmin" => 只有直接點擊視窗外面的背景才執行  -->
      <!-- 整體結構：
        modal-backdrop：全畫面黑色半透明背景
        └── modal-window：中央白色視窗
            ├── 標題
            └── form
                ├── 狀態下拉選單
                ├── 錯誤訊息
                ├── 確認按鈕
                └── 取消按鈕-->
      <dialog
        v-if="selectedAdmin"
        class="account-modal-backdrop"
        aria-labelledby="admin-status-title"
        @cancel.prevent="handleDialogCancel"
        @click.self="handleDialogBackdropClick"
      >
        <section
          class="account-modal-window account-modal-window--admin-status"
          :class="{ 'account-modal-window--confirm': selectedAdminStatus === 1 }"
        >
          <!-- 代入選擇管理員的帳號 -->
          <h2 id="admin-status-title">
            {{
              selectedAdminStatus === 1
                ? `確定要啟用管理員「${selectedAdmin.account}」嗎？`
                : `變更管理員狀態：${selectedAdmin.account}`
            }}
          </h2>
          <button
            v-if="selectedAdminStatus === 1"
            class="account-dialog-close"
            type="button"
            aria-label="關閉"
            :disabled="updatingAdminId === selectedAdmin.id"
            @click="cancelDisableAdmin"
          >
            ×
          </button>

          <!-- 表單送出 -->
          <form
            :class="{ 'account-confirm-form': selectedAdminStatus === 1 }"
            @submit.prevent="confirmDisableAdmin"
          >
            <div v-if="selectedAdminStatus !== 1">
              <label for="admin-disabled-status"> 新狀態 </label>

              <!-- .number 會盡量把選到的值轉成數字 -->
              <select
                id="admin-disabled-status"
                v-model.number="selectedAdminStatus"
                required
                :aria-describedby="actionErrorMessage ? 'admin-status-error' : undefined"
                :aria-invalid="actionErrorMessage ? 'true' : undefined"
              >
                <option
                  v-for="option in adminDisabledOptions"
                  :key="option.value"
                  :value="option.value"
                >
                  {{ option.label }}
                </option>
              </select>
            </div>

            <p
              v-if="actionErrorMessage"
              id="admin-status-error"
              class="error"
              role="alert"
            >
              {{ actionErrorMessage }}
            </p>

            <!-- button的submit 
            => 執行<form>的confirmDisableAdmin() -->
            <!-- 準備修改的管理員 === 目前選擇的管理員 => 判斷目前是不是正在更新這位管理員  -->
            <div v-if="selectedAdminStatus === 1" class="account-confirm-actions">
              <button
                class="comic-button account-button-primary"
                type="submit"
                :disabled="updatingAdminId === selectedAdmin.id"
              >
                {{ updatingAdminId === selectedAdmin.id ? '處理中...' : '確定' }}
              </button>
              <span>or</span>
              <button
                class="comic-button account-button-secondary"
                type="button"
                :disabled="updatingAdminId === selectedAdmin.id"
                @click="cancelDisableAdmin"
              >
                取消
              </button>
            </div>
            <div v-else class="account-modal-actions">
              <button
                class="comic-button account-button-secondary"
                type="button"
                :disabled="updatingAdminId === selectedAdmin.id"
                @click="cancelDisableAdmin"
              >
                取消
              </button>
              <button
                class="comic-button account-button-primary"
                type="submit"
                :disabled="updatingAdminId === selectedAdmin.id"
              >
                {{ updatingAdminId === selectedAdmin.id ? '處理中...' : '確認變更' }}
              </button>
            </div>
          </form>
        </section>
      </dialog>

      <!-- ------編輯管理員資料：彈出視窗------ -->
      <!-- 覆蓋整個畫面的半透明背景 => 點擊背景後 => 關閉彈窗 -->
      <dialog
        v-if="selectedEditAdmin"
        class="account-modal-backdrop"
        aria-labelledby="edit-admin-title"
        @cancel.prevent="handleDialogCancel"
        @click.self="handleDialogBackdropClick"
      >
        <!-- 中央彈窗 -->
        <section class="account-modal-window account-modal-window--edit-admin">
          <h2 id="edit-admin-title">編輯管理員：{{ selectedEditAdmin.account }}</h2>
          <form @submit.prevent="updateAdminProfile">
            <div>
              <!-- 編輯表單顯示暱稱時，不允許使用者把它清空 -->
              <label for="edit-admin-nickname">暱稱</label>
              <input
                id="edit-admin-nickname"
                v-model.trim="editAdminForm.nickname"
                type="text"
                maxlength="100"
                autocomplete="nickname"
                required
              />
            </div>
            <div>
              <!-- 編輯表單顯示Email時，不允許使用者把它清空 -->
              <label for="edit-admin-email">Email</label>
              <input
                id="edit-admin-email"
                v-model.trim="editAdminForm.email"
                type="email"
                autocomplete="email"
                required
              />
            </div>
            <!-- 因為表單一開始會顯示完整原資料，required 是防止使用者把既有的暱稱或 Email 清空 -->
            <div>
              <!-- 電話沒有required，所以畫面允許空白 -->
              <!-- type="tel" -> 點擊時通常會開啟電話／數字鍵盤，方便輸入電話 -->
              <!-- 電話欄位建議再補上： 
              inputmode="numeric" -> 手機顯示數字鍵盤
              pattern="[0-9]{10}" -> 必須是10個數字
              placeholder="例如：0912345678" -> 輸入框顯示：提示輸入格式 -->
              <label for="edit-admin-phone">電話(選填)</label>
              <input
                id="edit-admin-phone"
                v-model.trim="editAdminForm.phone"
                type="tel"
                inputmode="numeric"
                pattern="[0-9]{10}"
                maxlength="10"
                autocomplete="tel"
                placeholder="例如：0912345678"
                aria-describedby="edit-admin-phone-help"
              />
            </div>
            <p id="edit-admin-phone-help" class="account-field-help">請輸入 10 位數字。</p>
            <p
              v-if="editAdminErrorMessage"
              id="edit-admin-error"
              ref="editAdminError"
              class="error"
              tabindex="-1"
            >
              {{ editAdminErrorMessage }}
            </p>

            <div class="account-modal-actions">
              <button
                class="comic-button account-button-secondary"
                type="button"
                :disabled="isUpdatingAdminProfile"
                @click="cancelEditAdmin"
              >
                取消
              </button>
              <button
                class="comic-button account-button-primary"
                type="submit"
                :disabled="isUpdatingAdminProfile"
              >
                {{ isUpdatingAdminProfile ? '儲存中...' : '儲存修改' }}
              </button>
            </div>
          </form>
        </section>
      </dialog>

      <!-- ------管理員詳細資料 => 彈窗 ------ -->
      <dialog
        v-if="selectedDetailAdmin"
        class="account-modal-backdrop"
        aria-labelledby="admin-detail-title"
        @cancel.prevent="handleDialogCancel"
        @click.self="handleDialogBackdropClick"
      >
        <!-- 中央彈窗 -->
        <section class="account-modal-window account-modal-window--admin-detail">
          <h2 id="admin-detail-title">管理員詳細資料</h2>
          <div
            class="account-detail-scroll"
            tabindex="0"
            role="region"
            aria-label="管理員詳細資料表，可左右捲動"
          >
            <table class="account-detail-table">
              <caption class="visually-hidden">管理員詳細資料</caption>
              <thead>
                <tr>
                  <th scope="col">編號</th>
                  <th scope="col">帳號</th>
                  <th scope="col">暱稱</th>
                  <th scope="col">Email</th>
                  <th scope="col">電話</th>
                  <th scope="col">狀態</th>
                  <th scope="col">角色</th>
                  <th scope="col">建立時間</th>
                  <th scope="col">最後登入</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>{{ selectedDetailAdmin.id }}</td>
                  <td>{{ selectedDetailAdmin.account }}</td>
                  <td>{{ selectedDetailAdmin.nickname }}</td>
                  <td>{{ selectedDetailAdmin.email }}</td>
                  <td>{{ selectedDetailAdmin.phone || '未填寫' }}</td>
                  <td>{{ getAdminStatusText(selectedDetailAdmin.status) }}</td>
                  <td>{{ selectedDetailAdmin.role }}</td>
                  <td>
                    {{
                      selectedDetailAdmin.createdAt
                        ? formatDateTime(selectedDetailAdmin.createdAt)
                        : '無建立時間'
                    }}
                  </td>
                  <td>
                    {{
                      selectedDetailAdmin.lastLoginAt
                        ? formatDateTime(selectedDetailAdmin.lastLoginAt)
                        : '尚未登入'
                    }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          <button
            class="comic-button account-button-secondary"
            type="button"
            @click="cancelDetailAdmin"
          >
            關閉
          </button>
        </section>
      </dialog>
    </template>
      </div>
    </main>
  </div>
</template>

