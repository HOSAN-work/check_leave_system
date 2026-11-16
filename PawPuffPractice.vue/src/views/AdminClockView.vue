<!-- 管理員打卡頁面 -->
<!-- 列出頁面要做什麼 ？
1. 頁面開啟時，取得今天的打卡狀態。
2. 頁面開啟時，取得歷史打卡紀錄。
3. 尚未上班時，可以按「上班打卡」。
4. 已上班、未下班時，可以按「下班打卡」。
5. 上下班都完成後，兩個按鈕都不能按。
6. 打卡成功後，重新取得最新資料。
7. 時間顯示為 12 小時制。 -->

<!-- 輸寫順序是： 
1. 建立頁面基本結構 -> 代入vue3

2. 匯入需要的工具 
-> import {...} from 'vue' 
=> ref 影響畫面的響應資料 \ computed 根據打卡狀態自動計算顯示文字 \ onMounted 頁面顯示後自動載入資料
-> import api from '@/services/api'
=> 呼叫後端 API，並攜帶 Cookie

3. 建立頁面狀態
-> 考慮這個頁面需要記住什麼：
-> 今天的打卡狀態 => const todayStatus = ref({.....})
-> 歷史紀錄 => const records = ref({..})
-> 是否正在載入 => const isLoading = ref(....)
-> 是否正在送出打卡 => const isSubmitting = ref(...)
-> 成功訊息 => const actionMessage = ref(...)
-> 失敗訊息 => const actionErrorMessage = ref(...)

4. 建立日期時間格式化函式

後端 DateOnly 會傳回：2026-10-16

先寫日期格式化：
function formatDate(value) {
......
return....
}

接著寫日期時間格式化：
function formatDateTime(value) {
.....
return....
hour12: true //12小時制
}

===> 顯示： 2026/10/16 下午05:30:15

5. 建立狀態顯示文字

三種狀態：
沒有 record              → 尚未上班
有 record、沒有下班時間   → 已上班，尚未下班
有 record、有下班時間     → 上下班都完成

後端已經回傳：
hasClockedIn
hasClockedOut

建立今日狀態的計算屬性：
const todayStatusText = computed(() => {
.....
})

6. 建立取得今日狀態與歷史紀錄
1. 頁面開啟時 => 取得今天的打卡狀態
2. 頁面開啟時 => 取得歷史打卡紀錄

async function loadClockData() {
.....
try{  //可以用Promise.all() }
catch{ }
finally{ }
}

7. 建立上班打卡函式
8. 建立下班打卡函式
9. 頁面開啟時載入資料
10. 寫 template 畫面
11. 加入路由
12. 加入導覽連結-->

<!-- 資料與功能 -->
<script setup>
import { ref, computed, onMounted, watch, nextTick } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import DigitalClock from '@/components/DigitalClock.vue'

import api from '@/services/api'
import { useAuth } from '@/composables/useAuth.js'
import '../assets/drip-button.css'
import dashboardPaper from '../assets/dashboard-paper.png'
import dashboardProfile from '../assets/profile.png'
import navClockIcon from '../assets/dashboard-nav-clock.svg'
import navUserIcon from '../assets/dashboard-nav-user.svg'
import navLeaveIcon from '../assets/dashboard-nav-leave.svg'
import navAdminIcon from '../assets/dashboard-nav-admin.svg'
import pagePreviousIcon from '../assets/dashboard-page-prev.svg'
import pageNextIcon from '../assets/dashboard-page-next.svg'

const route = useRoute()
const { currentUser, logout } = useAuth()
const isAllAdminRecordsPage = computed(() => route.name === 'admin-clock-records')
const dashboardMain = ref(null)
const adminRecordsHeading = ref(null)
const personalRecordsHeading = ref(null)
const notClockedInTrigger = ref(null)
const notClockedInDialogHeading = ref(null)
const notClockedInPopover = ref(null)

watch(isAllAdminRecordsPage, async (isRecordsPage) => {
  await nextTick()
  if (isRecordsPage) {
    adminRecordsHeading.value?.focus()
  } else {
    dashboardMain.value?.focus()
  }
})

watch(
  () => route.hash,
  async (hash) => {
    if (hash === '#personal-records') {
      await nextTick()
      personalRecordsHeading.value?.focus()
    }
  },
)

function handleNotClockedInPopoverToggle(event) {
  if (event.newState === 'open') {
    notClockedInDialogHeading.value?.focus()
  }
}

function closeNotClockedInPopover() {
  if (notClockedInPopover.value?.matches(':popover-open')) {
    notClockedInPopover.value.hidePopover()
  }
  notClockedInTrigger.value?.focus()
}

// 建立「頁面狀態的初始資料」，讓頁面還沒呼叫 API 前就有東西可以使用。
// ===> 畫面需要長期保存、顯示或控制的資料，才建立 ref()。
//
// 今日狀態：後端 GET today API
// 回傳：
// workDate = today, -> 今日工作日
// hasClockedIn, -> 是否已經上班打卡
// hasClockedOut, -> 是否已經下班打卡
// canClockIn, -> 是否可以上班打卡
// canClockOut, -> 是否可以下班打卡
// record, -> 查詢"目前管理員"今天的打卡紀錄
// 前端依照後端建立相同的欄位
const todayStatus = ref({
  workDate: '',
  hasClockedIn: false,
  hasClockedOut: false,
  canClockIn: false,
  canClockOut: false,
  record: null, //record → 一筆紀錄
})
// 查詢目前登入管理員自己的打卡紀錄：後端 GET me API
// #region  回傳：
// List < AdminClockDto > records =
// {
// Id = clock.Id,
// AdminAccount = clock.Admin.Account,
// WorkDate = clock.WorkDate,
// ClockInTime = clock.ClockInTime,
// ClockOutTime = clock.ClockOutTime,
//  }
//
// return Ok(new { records });
// #endregion

// 多筆打卡紀錄要使用陣列保存
const records = ref([])
const personalCurrentPage = ref(1)
const personalPageSize = 5
const personalTotalPages = computed(() => Math.max(1, Math.ceil(records.value.length / personalPageSize)))
const personalPageNumbers = computed(() => {
  const pageCount = Math.min(7, personalTotalPages.value)
  const firstPage = Math.max(
    1,
    Math.min(personalCurrentPage.value - 3, personalTotalPages.value - pageCount + 1),
  )

  return Array.from({ length: pageCount }, (_, index) => firstPage + index)
})
const personalPageRecords = computed(() => {
  const startIndex = (personalCurrentPage.value - 1) * personalPageSize
  return records.value.slice(startIndex, startIndex + personalPageSize)
})

function setPersonalPage(page) {
  if (page < 1 || page > personalTotalPages.value) {
    return
  }

  personalCurrentPage.value = page
}

// 所有管理員的打卡紀錄
const allRecords = ref([])

// 所有管理員資料
const admins = ref([])

// 管理員帳號搜尋文字
const adminAccountSearch = ref('')
// 工作日篩選
const workDateFilter = ref('')
// allRecords          → 所有管理員的原始紀錄
// adminAccountSearch  → 使用者輸入的管理員帳號
// workDateFilter      → 使用者選擇的日期

// 上班打卡：後端 POST clock-in API
// #region 回傳：
// return Ok(new { message = "上班打卡成功", record });
// ===> 目的是執行一個動作
//
// 前端只需要：
// 1. 顯示成功訊息。
// 2. 重新取得最新今日狀態。
// 3. 重新取得歷史紀錄
// ===> 改變 todayStatus 和 records
// #endregion
// 下班打卡：後端 POST clock-out API => 與clock-in相同

// #region  什麼情況才需要每支 API 各自的狀態？
// => 如果畫面確實要分別顯示它們，就可以建立。
// 例如希望上班和下班按鈕各自顯示處理狀態：
// const isClockingIn = ref(false) //尚未上班打卡
// 上班時: isClockingIn.value = true
// 下班時: isClockingIn.value = false
//
// const isClockingOut = ref(false) //尚未下班打卡
// 下班時: isClockingOut.value = true
// 上班時: isClockingOut.value = false
// 這比共用：const isSubmitting = ref(false) 更精細。
//
// #endregion
// 判斷要不要建立 ref() 的三個問題
// 每次收到 API 回應時，問：
// 1. 這份資料之後還要使用嗎？
// 2. Template 需要顯示它嗎？
// 3. 它改變時，畫面需要自動更新嗎？
//
// 一個 isSubmitting 可以控制兩個按鈕嗎？
// => 可以, const isSubmitting = ref(false)
// 意思：目前是否有任何一個打卡 API 正在執行。
// 如果希望兩個按鈕的處理文字更精確，再拆成 isClockingIn 和 isClockingOut
const isSubmitting = ref(false)

// 是否正在讀取資料
// 記錄：前端目前是否正在等待「查詢資料 API」回應。
const isLoading = ref(false)

// 打卡成功訊息
const actionMessage = ref('')

// 錯誤訊息
const actionErrorMessage = ref('')

// 狀態產生畫面文字 => computed()
// 判斷三種狀態：
// 1. record  null -> 尚未上班
// 2. record not null 、 ClockOutTime = null -> 已上班、尚未下班
// 3. record not null 、 ClockOutTime = not null -> 上下班完成
//
// 後端已經回傳：
// hasClockedIn
// hasClockedOut
const todayStatusText = computed(() => {
  // 初始：const todayStatus = ref({
  // ....
  // hasClockedIn = false ; //已經上班打卡
  // })
  //
  // #region 狀態一、
  // hasClockedIn.value = false = 原來就沒上班打卡  => !false = true
  // => return 今日尚未打卡
  //
  // hasClockedIn.value = true = 原來就有上班打卡 => !true = false
  // => 不進if
  // #endregion
  if (!todayStatus.value.hasClockedIn) {
    return '今天尚未上班打卡'
  }

  // hasClockedIn.value = true = 原來就有上班打卡
  // |
  // v
  // 狀態二：已上班、尚未下班
  // hasClockOut.value = false = 沒有下單打卡 => !false = true => 進if
  if (!todayStatus.value.hasClockedOut) {
    return '已完成上班打卡,尚未下班打卡'
  }

  // hasClockedIn.value = true
  // hasClockOut.value = true
  // |
  // v
  // 狀態三：上下班打卡已完成
  return '今日上下班打卡已完成'
})

// 根據管理員帳號和工作日期篩選全部紀錄
// 從全部管理員打卡紀錄 allRecords 中，篩選出同時符合「管理員帳號搜尋文字」與「工作日期」的紀錄。
// .filter() 會逐筆檢查 . 每次取出一筆資料，暫時命名為：record ,回傳陣列
const filteredAllRecords = computed(() => {
  // 先決定要從「全部日期」還是「只有今天」開始篩選，再套用帳號及日期條件
  // showAllDates.value = true -> allRecords.value →從所有日期的紀錄中搜尋
  //
  // 預設 showAllDates = ref(false) -> 顯示今天的
  // showAllDates.value = false -> todayAllRecords.value →只從今天的紀錄中搜尋
  //
  // sourceRecords 經過三元運算後，已經是一般陣列，不是 ref，因此沒有 .value
  const sourceRecords = showAllDates.value ? allRecords.value : todayAllRecords.value
  // #region 假設：
  // const allRecords =
  // ref([
  //   { id: 1, adminAccount: 'admin01' },
  //   { id: 2, adminAccount: 'admin02' },
  // ])
  //
  // const todayAllRecords = computed(() => [{ id: 1, adminAccount: 'admin01' }])
  //
  // ref() 和 computed() 可以想成外面包了一個盒子：
  //   allRecords
  //        └── value
  //              └── 陣列
  //
  //  todayAllRecords
  //        └── value
  //              └── 陣列
  //
  // 所以要取出裡面的陣列，必須寫：
  // allRecords.value
  // todayAllRecords.value
  //
  // const sourceRecords = showAllDates.value
  // ? allRecords.value
  // : todayAllRecords.value
  //
  //  => 假設：showAllDates.value = true
  //  ==> const sourceRecords = allRecords.value
  //  ==>
  // const sourceRecords =
  // [
  //   { id: 1, adminAccount: 'admin01' },
  //   { id: 2, adminAccount: 'admin02' },
  // ]
  //  ===> 此時 sourceRecords 本身就是陣列，不是 ref 盒子
  //  ===> 所以直接使用：
  // sourceRecords.filter(...)
  // sourceRecords.length
  // sourceRecords[0]
  // #endregion

  // 使用者輸入文字
  // 例如：" ADMin " -去除空白、轉成小寫->  "admin"
  // accountKeyword = "admin"
  const accountKeyword = adminAccountSearch.value.trim().toLowerCase()

  // 外層 return：
  // 將 filter 建立的篩選後陣列回傳給 computed
  return sourceRecords.filter((record) => {
    // #region 管理員帳號篩選：判斷帳號是否符合
    // 例如： accountKeyword = "" => 在條件判斷中會被視為false
    // !accountKeyword = true
    //
    // matchesAccount = true || 右邊條件 = true
    // -> 右邊這段通常不會再執行
    // -> 「短路運算」
    //
    //
    // 假設目前紀錄裡的帳號：'Admin001' -轉成小寫-> 'admin001'
    //  -包含""-> true
    //  matchesAccount = true || true = true => 帳號符合所以顯示出來
    // #endregion
    const matchesAccount =
      !accountKeyword || record.adminAccount.toLowerCase().includes(accountKeyword)
    // #region 日期篩選
    // 沒選日期-> workDateFilter.value ='' == false
    // => !workDateFilter.value == true
    // matchesDate = true || 右邊條件 ＝= true => 所有日期都符合
    //
    // '2026-10-17' -> workDateFilter.value='2026-10-17' == true
    // => !workDateFilter.value == false
    // matchesDate = false || '2026-10-17' === '2026-10-17'
    // => false || true == true => '2026-10-17'符合
    //
    // record.workDate → 資料庫紀錄的日期
    // workDateFilter.value → 使用者目前選擇的篩選日期
    //
    // record.workDate = '2026-10-16'
    // matchesDate = false || '2026-10-16' === '2026-10-17'
    // => false || false == false => '2026-10-17'不符合
    // #endregion
    const matchesDate = !workDateFilter.value || record.workDate === workDateFilter.value

    // #region 帳號條件符合，而且日期條件也符合
    // => true && true
    // => filter 保留這一筆打卡紀錄
    //
    // 帳號符合，但日期不符合 => true && false = false => 這筆不顯示
    //
    // 為什麼不能只看帳號是否符合？
    // 如果只寫：return matchesAccount
    // => 當使用者選擇日期時，日期條件完全沒有作用。
    // 結果卻會顯示：
    // admin001　2026-10-17
    // admin001　2026-10-16
    // admin001　2026-10-15
    // admin001　2026-10-14
    // => 日期篩選就失去意義
    //
    // 如果日期沒有填呢？
    // workDateFilter.value = '' == false
    // !workDateFilter.value = true
    // => matchesDate = true || 右邊條件 == true
    // ===> return matchesAccount && true == true  => 最後只看帳號
    //
    // 規則是：
    // 日期有填 → 日期必須符合
    // 日期沒填 → 不限制日期
    //
    // && 代表縮小搜尋範圍
    // #endregion
    // 內層 return：
    // 把 true 或 false 回傳給 filter
    // true → 保留這筆 record
    // false → 排除這筆 record
    // 它會告訴 filter()：目前正在檢查的這一筆 record，到底要保留還是排除？
    return matchesAccount && matchesDate
  })
})

// 統計今天有多少位不同管理員已打卡
const todayAttendanceSummary = computed(() => {
  // 1. 篩選status=1管理員
  // 假設：
  // admins.value =
  // [
  //   { account: 'Admin01', status: 1 },
  //   { account: 'Admin02', status: 2 },
  //   { account: 'Manager01', status: 1 },
  // ]
  //
  // => .filter() - admin.status =1 -> 只保留status=1管理員
  // activeAdmins → 管理員物件陣列
  // activeAdmins =
  // [
  //   { account: 'Admin01', status: 1 },
  //   { account: 'Manager01', status: 1 },
  // ]
  const activeAdmins = admins.value.filter((admin) => admin.status === 1)

  // 2. 取得status=1的管理員帳號
  // activeAdminAccounts → 只有帳號文字的陣列
  //
  // activeAdmins =
  // [
  //   { account: 'Admin01', status: 1 },
  //   { account: 'Manager01', status: 1 },
  // ]
  //
  // => .map() -admin.account.toLowerCase()-> 只取出帳號並轉成小寫
  //
  // activeAdminAccounts =
  // [
  //   'admin01',
  //   'manager01',
  // ]
  const activeAdminAccounts = activeAdmins.map((admin) => admin.account.toLowerCase())

  // 3. 今天已打卡，而且目前仍為啟用狀態的管理員 ID
  // 假設：
  // activeAdminAccounts =
  // [
  //   'admin01',
  //   'manager01',
  // ]
  //
  // 今天的打卡紀錄：
  // todayAllRecords.value = [
  //   {
  //     adminAccount: 'Admin01',
  //     workDate: '2026-10-19',
  //   },
  //   {
  //     adminAccount: 'Admin02',
  //     workDate: '2026-10-19',
  //   },
  //   {
  //     adminAccount: 'Manager01',
  //     workDate: '2026-10-19',
  //   },
  // ]
  //
  // 開始篩選：
  // const clockedInRecords = todayAllRecords.value.filter(
  // 判斷條件：
  // activeAdminAccounts.includes(record.adminAccount.toLowerCase(),)
  //
  // 第一筆： todayAllRecords裡 record.adminAccount // 'Admin01'
  // -toLowerCase()-> 'admin01'
  // 檢查activeAdminAccounts includes('admin01') => true => 保留
  //
  // 第二筆： todayAllRecords裡 record.adminAccount // 'Admin02'
  // -toLowerCase()-> 'admin02'
  // 檢查activeAdminAccounts includes('admin02') => false => 移除
  //
  // 第三筆： todayAllRecords裡 record.adminAccount // 'Manager01'
  // -toLowerCase()-> 'manager01'
  // 檢查activeAdminAccounts includes('manager01') => true => 保留
  //
  // ==>
  //  clockedAdminIds =
  // [
  // {adminAccount:'Admin01',
  //   workDate:'2026-10-19'
  // },
  // {
  //   adminAccount:'Manager01',
  //   workDate:'2026-10-19'
  // }
  // ]
  //
  const clockedInRecords = todayAllRecords.value.filter(
    (record) => activeAdminAccounts.includes(record.adminAccount.toLowerCase()),
    // #region .has() 是 Set 用來檢查「某個值是否存在」的方法
    // 例如：
    // const activeAdminIds = new Set([10, 25, 30])
    // activeAdminIds.has(10) // true
    // activeAdminIds.has(25) // true
    // activeAdminIds.has(99) // false
    //
    // .filter((record) => activeAdminIds.has(record.adminId))
    // 例如：
    // activeAdminIds = Set { 10, 25 }
    //
    // record.adminId = 10
    // activeAdminIds.has(10) // true，所以保留
    //
    // record.adminId = 30
    // activeAdminIds.has(30) // false，所以刪除
    //
    // 區別：
    // 陣列使用 includes()
    // Set 使用 has()
    //
    // [10, 25].includes(10)    // true
    // new Set([10, 25]).has(10) // true
    // #endregion
  )
  const clockedInAccountCount = new Set(
    clockedInRecords.map((record) => record.adminAccount.toLowerCase()),
  ).size
  // 執行順序是由內往外：map() → filter() → new Set()
  // 假設：
  // todayAllRecords.value = [
  //   { adminId: 10 },
  //   { adminId: 10 },
  //   { adminId: 25 },
  //   { adminId: 30 }, // 已停用的管理員
  // ]
  //
  // 第一步：map()
  // 打卡紀錄 → 取出管理員 ID
  // todayAllRecords.value =
  // [
  //   { adminId: 10 },
  //   { adminId: 10 },
  //   { adminId: 25 },
  //   { adminId: 30 },
  // ]
  //     |
  //     v
  //  [10,10,25,30]
  //
  // 第二步：filter() : 只保留存在於 activeAdminIds 的 ID
  // [10, 10, 25, 30]
  //       ↓ filter()
  // [10, 10, 25]
  //
  // 第三步：new Set()
  // 去除重複 ID
  //  [10,10,25]
  //     |
  //   new Set()
  //     v
  // clockedAdminIds = Set(2){10,25}
  // 或者
  // const clockedAdminIds = new Set([10,25])
  //
  // clockedAdminIds.size =2

  //#region  Set 是什麼？ ：Set 是一種不允許重複值的集合。
  // const ids = [10, 10, 25, 25, 30]
  // const uniqueIds = new Set(ids)
  // => uniqueIds = [10,25,30]
  // => uniqueIds.size = 3
  // #endregion
  //#region  map() 是什麼？ :map() 會把陣列裡的每一筆資料「轉換成另一個值」，最後建立一個新陣列。
  // 原本：
  // const records = [
  //   { adminId: 10, adminAccount: 'admin01' },
  //   { adminId: 25, adminAccount: 'admin02' },
  // ]
  // 執行 const adminIds = records.map((record) => record.adminId,)
  // 第一筆： record.adminId -> 10
  // 第二筆： record.adminId -> 25
  // ==> adminsIds = [10,25]
  //
  // 例如：
  // const numbers = [1, 2, 3]
  // const results = numbers.map((number) => number * 2,)
  // ==> results = [2,4,6]
  // #endregion

  return {
    // totalAdmins：啟用中的管理員總數
    totalAdmins: activeAdmins.length,
    // clockedIn：啟用中且今天已打卡的不重複管理員數
    clockedIn: clockedInAccountCount,
    // notClockedIn：啟用中的管理員總數 - 啟用中且今天已打卡的人數
    notClockedIn: activeAdmins.length - clockedInAccountCount,
  }
})

// 今天尚未打卡的啟用管理員帳號
const notClockedInAdminAccounts = computed(() => {
  // 取得今天已打卡的帳號 = 今天打卡的管理員紀錄 -> .map() adminAccount放入新的一個陣列轉成小寫
  const clockedAccounts = todayAllRecords.value.map((record) => record.adminAccount.toLowerCase())

  // 篩選出帳號：admin.status === 1 && !clockedAccounts.includes(...)
  // 已經打卡：includes()=true -> !true=false -> 排除
  // 尚未打卡：includes() = false → !false = true → 保留
  // includes() 檢查帳號是否在已打卡名單
  return admins.value
    .filter((admin) => admin.status === 1 && !clockedAccounts.includes(admin.account.toLowerCase()))
    .map((admin) => admin.account)
})

// #region 假設所有打卡紀錄是：
// allRecords.value = [
//   {
//     adminAccount: 'admin01',
//     workDate: '2026-10-19',
//   },
//   {
//     adminAccount: 'admin02',
//     workDate: '2026-10-19',
//   },
//   {
//     adminAccount: 'admin01',
//     workDate: '2026-10-18',
//   },
// ]
//
// 今天是：todayStatus.value.workDate = '2026-10-19'
//
// 今天的紀錄是：
// todayAllRecords.value = [
//   {
//     adminAccount: 'admin01',
//     workDate: '2026-10-19',
//   },
//   {
//     adminAccount: 'admin02',
//     workDate: '2026-10-19',
//   },
// ]
//
// 例子一：查看全部日期 => showAllDates.value = true
// sourceRecords = allRecords.value
// 畫面：
// admin01　2026-10-19
// admin02　2026-10-19
// admin01　2026-10-18
//
// 例子二：只看今天 => showAllDates.value = false
// sourceRecords = todayAllRecords.value
// 畫面:
// admin01　2026-10-19
// admin02　2026-10-19
//
// 例子三：查看全部日期，再搜尋 admin01
// showAllDates.value = true
// adminAccountSearch.value = 'admin01'
// workDateFilter.value = ''
// 先取得全部三筆，再套用帳號篩選：
// admin01　2026-10-19
// admin01　2026-10-18
//
// 例子四：查看全部日期，搜尋 Admin01，再選日期
// 假設資料庫內：
// record = {
//   adminAccount: 'Admin01',
//   workDate: '2026-10-18',
// }
//
// showAllDates.value = true
// adminAccountSearch.value = 'admin01' -> 使用者輸入
// workDateFilter.value = '2026-10-18' -> 使用者選擇日期
// 必須同時符合：
// 帳號包含 admin01 && 日期是 2026-10-18
//
// 帳號判斷：
// const accountKeyword = adminAccountSearch.value.trim().toLowerCase()
// => const accountKeyword = "admin01" -bool-> true
//  !accountKeyword = false
//
// const matchesAccount = !accountKeyword || 'Admin01'.toLowerCase().includes('admin01')
// => matchesAccount = false || true = true -> 符合帳號篩選條件
//
// const matchesDate = !workDateFilter.value || record.workDate === workDateFilter.value
// workDateFilter.value = '2026-10-18' -bool-> true
// !workDateFilter.value = '' -bool-> false
//
// record.workDate = '2026-10-18'
// workDateFilter.value = '2026-10-18'
// record.workDate === workDateFilter.value
// => '2026-10-18' === '2026-10-18' === true
// ===> matchesDate = false || true = true -> 符合日期篩選條件
//
// return matchesAccount && matchesDate
// return true && true = true -> 帳號包含 admin01 && 日期是 2026-10-18
//
// 例子五：只看今天，卻選擇昨天
// 假設今天是：todayStatus.value.workDate = '2026-10-19'
// 所有資料：
// allRecords.value = [
//   { adminAccount: 'admin01', workDate: '2026-10-19' },
//   { adminAccount: 'admin02', workDate: '2026-10-19' },
//   { adminAccount: 'admin01', workDate: '2026-10-18' },
// ]
//
// 第一步「只看今天」→ 昨天的資料先被排除
// showAllDates.value = false -> 只看今天
// sourceRecords = todayAllRecords.value
// 此時只剩今天兩筆：
// [
//   { adminAccount: 'admin01', workDate: '2026-10-19' },
//   { adminAccount: 'admin02', workDate: '2026-10-19' },
// ]
//
// 第二步「找昨天」→ 剩下的今天資料都不符合
// 日期選擇昨天
// workDateFilter.value = '2026-10-18' -bool-> true
//  !workDateFilter.value = '' -> false
//
// 檢查第一筆：record.workDate = '2026-10-19'
// const matchesDate = !workDateFilter.value || record.workDate === workDateFilter.value
// => const matchesDate = false || '2026-10-19' === '2026-10-18'
// => const matchesDate = false || false = false -> 不符合日期篩選條件
//
// ===> 結果：查無資料
// #endregion

// 使用者按下「只看今天／查看全部」按鈕時，切換顯示範圍
function toggleAllDates() {
  //
  // 初始 const showAllDates = ref(false) -> 只顯示今天
  //  true -> 顯示全部日期
  // 第一次按：
  // showAllDates.value = !false = true -> 顯示全部日期
  //
  //
  //
  // 每按一次，就會來回切換：只看今天 → 查看全部日期 → 只看今天
  showAllDates.value = !showAllDates.value
  // 清除日期選擇器
  // 無論切換到哪一種狀態，都會清除日期
  workDateFilter.value = ''
  // 切換資料範圍後回到第一頁，避免原本停在第 4 頁，但切換後只剩一頁
  currentPage.value = 1 //預設回到第1頁
}

// 所有管理員打卡紀錄的「分頁」
const currentPage = ref(1) // 目前頁面為第一頁
const pageSize = ref(10) // 每頁10筆
// 總頁數
const totalPages = computed(() => {
  // Math.max(1, 計算結果) 表示總頁數最少是 1
  return Math.max(1, Math.ceil(filteredAllRecords.value.length / pageSize.value))
  // 例如：
  //  filteredAllRecords.value.length = 23 // 篩選後共 23 筆
  // pageSize.value = 10                 // 每頁顯示 10 筆
  // Math.ceil(23/10) = Math.ceil(2.3)=3
  // Math.max(1,3) = 3
  // return 3
  //
  // 例如：
  // Math.ceil(0,10) = 0
  // Max.max(1,0)=1
  // return 1
})

const attendanceSummary = computed(() => {
  const total = filteredAllRecords.value.length
  const completed = filteredAllRecords.value.filter((record) => record.clockOutTime).length
  return { total, completed, incomplete: total - completed }
})

const allAdminPageNumbers = computed(() => {
  const pageCount = Math.min(7, totalPages.value)
  const firstPage = Math.max(
    1,
    Math.min(currentPage.value - 3, totalPages.value - pageCount + 1),
  )

  return Array.from({ length: pageCount }, (_, index) => firstPage + index)
})

// 目前頁面的紀錄
// 從「篩選後的全部紀錄」中，取出目前頁面要顯示的那幾筆資料
const pageAllRecords = computed(() => {
  // 假設每頁顯示 10 筆：pageSize.value = 10
  // 第1頁： currentPage.value = 1
  // startIndex = (1-1)*10 = 0
  // slice(0,0+10) = slice(0,10)
  // -> 取得每筆index為 0,1,2,3,4,5,6,7,8,9 ,共10筆
  //
  // 第2頁： currentPage.value = 2
  // startIndex = (2-1)*10 = 10
  // slice(10,10+10) = slice(10,20)
  // -> 取得 10,11,12,13,14,15,16,17,18,19 ,共10筆
  //
  // 第3頁： currentPage.value = 3
  // startIndex = (3-1)*10 = 20
  // slice(20,20+10) = slice(20,30)
  // -> 取得 20,21,22,23,24,25,26,27,28,29 ,共10筆
  const startIndex = (currentPage.value - 1) * pageSize.value

  return filteredAllRecords.value.slice(startIndex, startIndex + pageSize.value)
})

// 加入監看後，只要帳號或日期條件改變，就會自動回到第 1 頁。
// 監看「管理員帳號」和「工作日期」和「每頁幾筆」三個篩選條件；其中任何一個改變，就回到第 1 頁。
watch([adminAccountSearch, workDateFilter, pageSize], () => {
  currentPage.value = 1
})

// #region computed 和 watch 的用途不同：
// computed：根據現有資料，算出另一個值。
// watch：監聽資料改變，然後執行副作用。
//
// 簡單記法：
// 要「得到一個值」 → computed
// 要「發生一個動作」 → watch
// #endregion

// 建立所有管理員的打卡紀錄狀態
const showAllDates = ref(false) // 控制狀態
const filteredRecordsAnnouncement = ref('')

// 建立今天的全部管理員打卡紀錄
// 從「所有管理員打卡紀錄」中，篩選出「今天的紀錄」
// .filter() 逐筆檢查所有管理員的打卡紀錄
const todayAllRecords = computed(() => {
  // record.workDate → 目前這筆紀錄的工作日期
  // todayStatus.value.workDate → 今天的工作日期
  // 日期相同就保留，不同就排除
  return allRecords.value.filter((record) => record.workDate === todayStatus.value.workDate)
})

let filteredRecordsAnnouncementTimeout

watch(
  [adminAccountSearch, workDateFilter, showAllDates, () => filteredAllRecords.value.length],
  (_, __, onCleanup) => {
    clearTimeout(filteredRecordsAnnouncementTimeout)
    filteredRecordsAnnouncementTimeout = setTimeout(() => {
      filteredRecordsAnnouncement.value =
        filteredAllRecords.value.length === 0
          ? '查無符合的打卡紀錄'
          : `共有 ${filteredAllRecords.value.length} 筆符合條件的打卡紀錄`
    }, 300)

    onCleanup(() => clearTimeout(filteredRecordsAnnouncementTimeout))
  },
  { immediate: true },
)

// 所有管理員打卡紀錄的「清除篩選」功能
function clearFilters() {
  adminAccountSearch.value = ''
  workDateFilter.value = ''
}

// #region 前面有定義 const allRecords = ref([]) 為陣列
// 例如：
// allRecords.value = [
//   {
//     id: 1,
//     adminAccount: 'admin',
//     workDate: '2026-10-17',
//   },
//   {
//     id: 2,
//     adminAccount: 'manager',
//     workDate: '2026-10-16',
//   },
// ]
//
//  .filter() 第一次把第一筆傳進去：
// record = {
//   id: 1,
//   adminAccount: 'admin',
//   workDate: '2026-10-17',
// }
//
// 內層return allRecords.value.filter((record) => {...} :
// matchesAccount = true
// matchesDate = true
// ==> return true && true = true
// 接收者是：filter()
// ===> true 回到 .filter()，所以第一筆被保留
//
// 第二次檢查第二筆：
// record = {
//   id: 2,
//   adminAccount: 'manager',
//   workDate: '2026-10-16',
// }
//
// 內層return :
// matchesAccount = false
// matchesDate = true
// ==> return false && true = false
// 接收者是：filter()
// ===> false 回到 .filter()，所以第二筆被排除
//
//  |
// 外層:
// return allRecords.value.filter(...)
// filter() 篩選後的新陣列
// 接收者是：computed()
//  |
//  v
// 建立一個新陣列：
// [
//   {
//     id: 1,
//     adminAccount: 'admin',
//     workDate: '2026-10-17',
//   },
// ]
//
//  接著外層的：
//  return allRecords.value.filter(...)
//  會把這個新陣列回傳給：computed()
//  最後成為：filteredAllRecords.value
//
// allRecords.value.length -> 「全部資料筆數」
// filteredAllRecords.value.length -> 經過帳號搜尋、日期篩選後，目前剩下幾筆
// #endregion
// 建立日期格式化：可能想顯示成：2026/10/16
// 後端回傳：
// DateOnly WorkDate = "2026-10-16"
// DateTime ClockInTime = "2026-10-16T08:35:42"
// DateTime ClockOutTime = "2026-10-16T18:05:10"
// .......
// <template>畫面呼叫時傳入 workDate => {{ formatDate(todayStatus.workDate) }}
// 執行時相當於：formatDate('2026-10-16')
//
// 日期格式化
// formatDate("2026-10-16")
function formatDate(value) {
  // workDate =''
  if (!value) {
    return ''
  }
  // date = new Date(`2026-10-16T00:00:00`)
  const date = new Date(`${value}T00:00:00`)

  // date.getTime() 會把日期轉成「從 1970 年 1 月 1 日到該時間經過的毫秒數」
  // isNaN = is Not a Number
  // -----------------------------------------
  // workDate = '2026-10-16'
  // const date = new Date(`2026-10-16T00:00:00`)
  // date.getTime() //可能得到 1792080000000
  // Number.isNaN(1792080000000) => false => 不進if
  // ------------------------------------------
  // workDate = '這不是日期'
  // new Date (`這不是日期T00:00:00`) -> Invalid Date
  // date.getTime() -> NaN
  // Number.isNaN(NaN) => true => 進if => return `日期格式錯誤`
  if (Number.isNaN(date.getTime())) {
    return '日期格式錯誤'
  }

  // toLocaleDateString() JavaScript Date 物件提供的方法
  // 用途是：將日期轉成符合指定地區格式的字串。
  // 'zh-TW' 代表使用台灣繁體中文的地區格式：zh → 中文 \ TW → 台灣
  // 顯示：2026/10/16
  //
  // date.toLocaleDateString('en-US')
  // 顯示：10/16/2026
  return date.toLocaleDateString('zh-TW', {
    year: 'numeric', // 年份使用完整數字 :2026
    month: '2-digit', // 月份固定顯示兩位數
    day: '2-digit',
  })
}
// 日期時間格式化
function formatDateTime(value) {
  if (!value) {
    return ''
  }

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return '時間格式錯誤'
  }

  return date.toLocaleTimeString('zh-TW', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: true, //12小時制
  })
}

// 顯示每日工作時數
function formatWorkDuration(clockInTime, clockOutTime) {
  if (!clockInTime || !clockOutTime) {
    return '尚未完成'
  }

  // JavaScript 的 Date 物件內部保存的是「時間點的毫秒數」
  // C# DateTime 相減 → 得到 TimeSpan → 可以讀取時、分、秒
  // 流程：
  //   後端 C# DateTime
  //         ↓ 轉成 JSON
  // 前端收到日期時間字串
  //         ↓ new Date(...)
  // JavaScript Date
  //         ↓ 相減
  //       毫秒差
  const clockIn = new Date(clockInTime)
  const clockOut = new Date(clockOutTime)

  // 無效錯誤檢查、clockOut<clockIn
  if (Number.isNaN(clockIn.getTime()) || Number.isNaN(clockOut.getTime()) || clockOut < clockIn) {
    return '時間錯誤'
  }

  // 分別取得時分秒：
  // date.getHours()   //小時
  // date.getMinutes() //分鐘
  // date.getSeconds() //秒
  // date.getTime() 大概意思是取得毫秒
  // -> 精確來說，是取得「從 1970/01/01 00:00:00 UTC 到這個日期時間的毫秒數」

  // 原本：直接相減
  // clockOut - clockIn 等同於 clockOut.getTime() - clockIn.getTime()
  // 毫秒 /1000 = 秒 -> /60 => 分鐘
  // const totalMinutes = Math.floor((clockOut - clockIn) / 1000 / 60)
  // 例如上班時間是 08:30:00，下班時間是 17:45:30
  // 17:45:30 - 08:30:00 = 09:15:30 = 60*9+15+30/60=555.5 mins
  // Math.floor(555.5)

  // 新版：相減後，再扣午休重疊部分
  let workMilliseconds = clockOut - clockIn

  // 複製上班日期
  const day = new Date(clockIn)
  day.setHours(0, 0, 0, 0) // (時,分,秒,毫秒)

  while (day < clockOut) {
    // 午休開始:12點
    const lunchStart = new Date(day)
    lunchStart.setHours(12, 0, 0, 0)
    //午休結束：13點
    const lunchEnd = new Date(day)
    lunchEnd.setHours(13, 0, 0, 0)

    // 只扣除實際出勤與午休重疊的時間
    const overlapClockIn = Math.max(clockIn.getTime(), lunchStart.getTime())
    const overlapClockOut = Math.min(clockOut.getTime(), lunchEnd.getTime())
    // 例子一：09:00 上班，12:30 下班 , 午休：12:00 ─────── 13:00
    //
    // Math.max(09:00, 12:00) → 12:00
    // 09:00 已上班，但 12:00 才開始午休，所以交集從 12:00 開始
    //
    // Math.min(12:30, 13:00) → 12:30
    // 午休雖到 13:00，但你 12:30 已下班，所以交集在 12:30 結束
    // ==> 午休 12:00-12:30
    // ------------------------------------
    // 例子二：12:30 上班，18:00 下班 , 午休：12:00 ─────── 13:00
    // Math.max(12:30,12:00) = 12:30
    // Math.min(18:00,13:00) = 13:00
    // ==> 午休 12:30-13:00

    // 實際工作時長 =工作時間-午休時間
    workMilliseconds -= Math.max(0, overlapClockOut - overlapClockIn)
    // 例如你 09:00～11:00 出勤，完全沒碰到午休：
    // overlapStart = max(09:00, 12:00) → 12:00
    // overlapEnd   = min(11:00, 13:00) → 11:00
    // 11:00 − 12:00 = −1 小時
    // 如果直接扣：workMilliseconds -= overlapEnd - overlapStart
    // 原本 2 小時 −（−1 小時）= 3 小時 ❌

    // 加一天
    day.setDate(day.getDate() + 1)
  }

  // 1 秒 = 1,000 毫秒
  // 1 分鐘 = 60 秒
  const totalMinutes = Math.floor(workMilliseconds / 60000)

  const hours = Math.floor(totalMinutes / 60)
  const minutes = totalMinutes % 60

  return `${hours}小時 ${minutes}分鐘`
}
// #region 括號裡「兩個都是傳入」 vs 括號裡「一個傳入、一個傳出」 區別
// 1. 括號裡「一個傳入、一個傳出」
// C# 的 out：
// 方法(傳入資料, out 傳出資料)
// int.TryParse(adminIdText, out adminId)
// adminIdText → 傳入
// out adminId → 傳出
//
// 2. 括號裡「兩個都是傳入」
// JavaScript：
// 函式(傳入資料1, 傳入資料2) ->  return 傳出結果
// formatWorkDuration(clockInTime, clockOutTime)
// clockInTime  → 傳入上班時間
// clockOutTime → 傳入下班時間
// #endregion

// 從後端載入今日狀態和管理員自己的打卡紀錄
async function loadClockData() {
  // #region 不要隨便清除成功訊息 !!
  // 流程:
  // 1. 上班打卡成功 ->  actionMessage = "上班打卡成功"
  //
  // 2. 呼叫 loadClockData() -> 如果裡面會清除成功訊息
  // |
  // v
  // 3. actionMessage = "" -> 清除成功訊息
  // |
  // v
  // 4. 畫面沒有成功訊息
  // #endregion

  // 清除上次的失敗訊息
  actionErrorMessage.value = ''
  // 如果 actionErrorMessage 不清除：
  // 第一次查詢失敗：取得打卡資料失敗
  // 第二次重新查詢成功時:會顯示就錯誤

  isLoading.value = true // 正在載入中

  try {
    const [todayResponse, recordsResponse, allRecordsResponse, adminsResponse] = await Promise.all([
      api.get('/api/AdminClocks/today'),
      api.get('/api/AdminClocks/me'),
      api.get('/api/AdminClocks/all'),
      api.get('/api/AdminUsers/admins'),
      //get /api/AdminUsers/admins => 取得Users裡的Role=Admin清單
    ])
    //#region todayResponse：
    // {
    //   data: {
    //     workDate: '2026-10-16',
    //     hasClockedIn: true,
    //     hasClockedOut: false,
    //     canClockIn: false,
    //     canClockOut: true,
    //     record: {
    //       // 今日打卡紀錄
    //     },
    //   },

    //   status: 200,
    //   statusText: 'OK',
    //   headers: {
    //     // 回應標頭
    //   },
    //   config: {
    //     // Axios 請求設定
    //   },
    // }
    // #endregion
    // todayStatus 預期是物件
    todayStatus.value = todayResponse.data
    // #region recordsResponse:
    // {
    //   "records": [
    //     {
    //       "id": 3,
    //       "adminAccount": "admin",
    //       "workDate": "2026-10-16",
    //       "clockInTime": "2026-10-16T08:35:42",
    //       "clockOutTime": null
    //     },
    //     {
    //       "id": 2,
    //       "adminAccount": "admin",
    //       "workDate": "2026-10-15",
    //       "clockInTime": "2026-10-15T08:40:00",
    //       "clockOutTime": "2026-10-15T18:10:00"
    //     }
    //   ]
    // }
    // #endregion
    // records 預期是陣列 , 預設是[]
    records.value = recordsResponse.data.records ?? [] //[] :沒有打卡紀錄
    personalCurrentPage.value = 1

    allRecords.value = allRecordsResponse.data.records ?? []

    admins.value = adminsResponse.data.admins ?? [] //保存管理員資料
  } catch (error) {
    console.error('取得打卡資料失敗：', error)

    // 不知道是誰
    if (error.response?.status === 401) {
      actionErrorMessage.value = '請先登入'
    }

    // 沒有權限
    else if (error.response?.status === 403) {
      actionErrorMessage.value = '你沒有管理員權限'
    }
    // 前面加 !，表示沒有收到回應。離線、後端未啟動等情況
    else if (!error.response) {
      actionErrorMessage.value = '無法連線至伺服器，請確認網路與後端服務'
    } else {
      // #region  例如後端回傳：
      // return Conflict(new
      // {
      //     message = "今天已經完成上班打卡"
      // });
      //
      // HTTP -> 409 Conflict
      // Axios 會進入：catch (error)
      //
      // error：
      // {
      //   response: {
      //     status: 409,
      //     data: {
      //       message: '今天已經完成上班打卡',
      //     },
      //   },
      // }
      //
      //  => error.response.data.message = 今天已經完成上班打卡
      // #endregion
      actionErrorMessage.value = error.response?.data?.message ?? '取得打卡資料失敗'
    }
    // 401 → 請先登入
    // 403 → 沒有管理員權限
    // 沒有收到回應 → 無法連線至伺服器
    // 其他錯誤 → 後端訊息或預設錯誤文字
  } finally {
    isLoading.value = false // 目前不載入
  }
}

// 上班打卡
async function clockIn() {
  actionMessage.value = ''
  actionErrorMessage.value = ''
  isSubmitting.value = true // 目前正在送出中 ->  上班打卡按鈕停用

  try {
    const response = await api.post('/api/AdminClocks/clock-in')

    // 更新成功訊息
    actionMessage.value = response.data.message ?? '上班打卡成功'

    // 重新載入今日狀態和管理員自己的打卡紀錄
    await loadClockData()
  } catch (error) {
    console.error('上班打卡失敗：', error)

    // 更新失敗訊息
    actionErrorMessage.value = error.response?.data?.message ?? '上班打卡失敗'
  } finally {
    isSubmitting.value = false // 目前不送出 -> 上班打卡按鈕可以按了
  }
}

// 下班打卡
async function clockOut() {
  actionMessage.value = ''
  actionErrorMessage.value = ''
  isSubmitting.value = true

  try {
    const response = await api.post('/api/AdminClocks/clock-out')

    actionMessage.value = response.data.message ?? '下班打卡成功'

    // 打卡完成後，重新取得今日狀態與歷史紀錄
    await loadClockData()
  } catch (error) {
    console.error('下班打卡失敗：', error)

    actionErrorMessage.value = error.response?.data?.message ?? '下班打卡失敗'
  } finally {
    isSubmitting.value = false
  }
}


// #region onMounted() 什麼時候才會執行？
// 只有這個打卡 Vue 元件真的被使用時才會執行。
// 例如建立：
// src/views/AdminClockView.vue
//
// 還需要在 Router 註冊:
// {
//   path: '/admin/clock',
//   name: 'admin-clock',
//   component: () =>
//     import('@/views/AdminClockView.vue'),
// }
// ===> 表示：當瀏覽器網址是 /admin/clock → 載入 AdminClockView.vue
// #endregion
// 載入並顯示後，該元件才會觸發：
onMounted(() => {
  loadClockData()
  if (!isAllAdminRecordsPage.value) {
    nextTick(() => dashboardMain.value?.focus())
  }
})
</script>

<!-- 畫面 -->
<template>
  <div class="admin-clock-page">
    <a class="dashboard-skip-link" href="#dashboard-main">跳到主要內容</a>
    <aside class="dashboard-sidebar">
      <div class="dashboard-avatar" aria-hidden="true">
        <img :src="dashboardProfile" alt="" />
      </div>
      <nav class="dashboard-nav" aria-label="主要導覽">
        <RouterLink
          class="dashboard-nav-link"
          :class="{ 'is-section-active': isAllAdminRecordsPage }"
          :aria-current="isAllAdminRecordsPage ? 'location' : undefined"
          to="/admin/clock"
        >
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
        <RouterLink class="dashboard-nav-link" to="/admin/users">
          <img :src="navAdminIcon" alt="" aria-hidden="true" />
          <span>帳號管理</span>
        </RouterLink>
      </nav>
      <div class="dashboard-logout-wrap drip-button-wrap">
        <span class="drip-button-drips" aria-hidden="true"></span>
        <button class="dashboard-logout drip-button" type="button" @click="logout">
          登出
        </button>
      </div>
    </aside>

    <main id="dashboard-main" ref="dashboardMain" class="dashboard-paper" tabindex="-1">
      <img class="dashboard-paper-texture" :src="dashboardPaper" alt="" aria-hidden="true" />
      <div
        class="dashboard-content"
        :class="{
          'dashboard-content--welcome': !isAllAdminRecordsPage,
          'dashboard-content--admin-records-today': isAllAdminRecordsPage && !showAllDates,
          'dashboard-content--admin-records-all-dates': isAllAdminRecordsPage && showAllDates,
        }"
      >
        <div v-if="!isAllAdminRecordsPage" class="dashboard-welcome-grid">
        <h1 class="dashboard-welcome">
            歡迎，{{ currentUser?.nickname || currentUser?.account || '管理員' }}
          </h1>
        </div>
        <div
          v-if="isAllAdminRecordsPage"
          class="admin-records-heading"
          :class="{
            'admin-records-heading--today': !showAllDates,
            'admin-records-heading--all-dates': showAllDates,
          }"
        >
          <div
            v-if="!showAllDates"
            class="attendance-summary-items admin-records-header-summary"
            role="status"
            aria-live="polite"
            aria-atomic="true"
          >
            <span>全部啟用管理員：{{ todayAttendanceSummary.totalAdmins }}人</span>
            <span>今日已打卡：{{ todayAttendanceSummary.clockedIn }}人</span>
          </div>
          <h1
            id="all-admin-heading"
            ref="adminRecordsHeading"
            class="admin-records-title"
            tabindex="-1"
          >
            管理員打卡紀錄
          </h1>
          <RouterLink
            class="comic-button admin-records-back"
            :to="{ name: 'admin-clock', hash: '#personal-records' }"
            aria-label="返回個人打卡紀錄"
          >
            返回
          </RouterLink>
        </div>
        <div class="dashboard-feedback">
          <p class="visually-hidden" role="alert" aria-live="assertive">
            {{ actionErrorMessage }}
          </p>
          <p class="loading-message" role="status" aria-live="polite">
            {{ isLoading ? '載入中...' : '' }}
          </p>
          <p
            v-if="actionErrorMessage && (isAllAdminRecordsPage || !todayStatus.workDate)"
            class="error feedback-error"
            aria-hidden="true"
          >
            {{ actionErrorMessage }}
          </p>
        </div>

        <!-- -----首次載入失敗 -> 重新整理----- -->
        <button
          v-if="!isLoading && !todayStatus.workDate"
          class="dashboard-reload-button"
          type="button"
          @click="loadClockData"
        >
          重新載入資料
        </button>

    <!-- todayStatus.workDate 初始是空字串，第一次成功取得資料後才有日期。 -->
    <template v-if="todayStatus.workDate">
      <template v-if="!isAllAdminRecordsPage">
      <div class="dashboard-top-grid">
        <section class="dashboard-clock" aria-label="時間顯示">
          <DigitalClock />
        </section>

        <section class="dashboard-status-card" aria-labelledby="today-status-title">
          <div class="dashboard-status-heading">
            <p class="success dashboard-action-message" role="status" aria-live="polite">
              {{ actionMessage }}
            </p>
            <h2 id="today-status-title">今日打卡狀態</h2>
            <RouterLink class="comic-button all-admin-link" to="/admin/clock/records">
              <span>所有管理員紀錄</span>
            </RouterLink>
          </div>
          <div
            class="today-table-scroll"
            role="region"
            aria-label="今日打卡狀態資料"
            tabindex="0"
          >
            <table class="today-status-table">
              <caption class="visually-hidden">今日打卡狀態</caption>
              <thead>
                <tr>
                  <th scope="col">工作日期</th>
                  <th scope="col">目前狀態</th>
                  <th scope="col">上班時間</th>
                  <th scope="col">下班時間</th>
                  <th scope="col">上班狀態</th>
                  <th scope="col">應下班時間</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>{{ formatDate(todayStatus.workDate) }}</td>
                  <td>{{ todayStatusText }}</td>
                  <td>
                    {{
                      todayStatus.record?.clockInTime
                        ? formatDateTime(todayStatus.record.clockInTime)
                        : '尚未上班打卡'
                    }}
                  </td>
                  <td>
                    {{
                      todayStatus.record?.clockOutTime
                        ? formatDateTime(todayStatus.record.clockOutTime)
                        : '尚未下班打卡'
                    }}
                  </td>
                  <td>
                    {{
                      todayStatus.record
                        ? todayStatus.record.isBeforeAllowedTime
                          ? '早於09:00'
                          : todayStatus.record.isLate
                            ? '遲到'
                            : '未遲到'
                        : '—'
                    }}
                  </td>
                  <td>
                    {{
                      todayStatus.record?.expectedClockOutTime
                        ? formatDateTime(todayStatus.record.expectedClockOutTime)
                        : '—'
                    }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="clock-actions">
          <button
            type="button"
            :disabled="!todayStatus.canClockIn || isSubmitting"
            @click="clockIn"
          >
            {{ isSubmitting && todayStatus.canClockIn ? '上班打卡中...' : '上班打卡' }}
          </button>
          <button
            type="button"
            :disabled="!todayStatus.canClockOut || isSubmitting"
            @click="clockOut"
          >
            {{ isSubmitting && todayStatus.canClockOut ? '下班打卡中...' : '下班打卡' }}
          </button>
        </div>
        </section>
      </div>
      <!-- -----加入我的打卡歷史紀錄----- -->
      <section
        id="personal-records"
        class="personal-records"
        aria-labelledby="personal-records-title"
      >
        <div class="records-heading">
          <p
            v-if="actionErrorMessage"
            class="error records-error-message"
            aria-hidden="true"
          >
            {{ actionErrorMessage }}
          </p>
          <h2
            id="personal-records-title"
            ref="personalRecordsHeading"
            tabindex="-1"
          >
            個人打卡紀錄
          </h2>
        </div>
        <p v-if="records.length === 0" class="empty-state" role="status" aria-live="polite">
          目前沒有打卡紀錄
        </p>

        <div v-else class="table-scroll" role="region" aria-label="個人打卡紀錄表格" tabindex="0">
        <table class="records-table">
          <caption class="visually-hidden">個人打卡紀錄</caption>
          <thead>
            <tr>
              <th scope="col">工作日期</th>
              <th scope="col">管理員帳號</th>
              <th scope="col">上班時間</th>
              <th scope="col">上班判定</th>
              <th scope="col">應下班時間</th>
              <th scope="col">下班時間</th>
              <th scope="col">工作時數</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="record in personalPageRecords" :key="record.id">
              <td>{{ formatDate(record.workDate) }}</td>
              <td>{{ record.adminAccount }}</td>

              <!-- 前面的v-for代表 record 一定是陣列中的一筆物件，不需要再次判斷 record 是否存在 -->
              <td>
                {{
                  record.clockInTime ? formatDateTime(record.clockInTime) : '尚未上班打卡'
                }}
              </td>
              <td>
                {{ record.isBeforeAllowedTime ? '早於09:00' : record.isLate ? '遲到' : '未遲到' }}
              </td>
              <td>
                {{
                  record.expectedClockOutTime
                    ? formatDateTime(record.expectedClockOutTime)
                    : '未設定'
                }}
              </td>
              <td>
                {{ record.clockOutTime ? formatDateTime(record.clockOutTime) : '尚未下班打卡' }}
              </td>
              <td>{{ formatWorkDuration(record.clockInTime, record.clockOutTime) }}</td>
            </tr>
          </tbody>
        </table>
        </div>
        <nav
          v-if="records.length > 0"
          class="record-pagination"
          aria-label="個人打卡紀錄頁數"
        >
          <button
            class="pagination-arrow"
            type="button"
            aria-label="上一頁"
            :disabled="personalCurrentPage === 1"
            @click="setPersonalPage(personalCurrentPage - 1)"
          >
            <img :src="pagePreviousIcon" alt="" aria-hidden="true" />
          </button>
          <ol>
            <li v-for="page in personalPageNumbers" :key="page">
              <button
                type="button"
                :aria-current="page === personalCurrentPage ? 'page' : undefined"
                :aria-label="`第 ${page} 頁`"
                :class="{ 'is-current': page === personalCurrentPage }"
                @click="setPersonalPage(page)"
              >
                <span>{{ page }}</span>
              </button>
            </li>
          </ol>
          <button
            class="pagination-arrow"
            type="button"
            aria-label="下一頁"
            :disabled="personalCurrentPage === personalTotalPages"
            @click="setPersonalPage(personalCurrentPage + 1)"
          >
            <img :src="pageNextIcon" alt="" aria-hidden="true" />
          </button>
          <span class="visually-hidden" aria-live="polite" aria-atomic="true">
            第 {{ personalCurrentPage }} 頁，共 {{ personalTotalPages }} 頁
          </span>
        </nav>
      </section>
      </template>

      <!-- -----加入所有管理員的打卡歷史紀錄----- -->
      <section
        v-if="isAllAdminRecordsPage"
        id="all-admin-records"
        class="admin-records-page"
        aria-labelledby="all-admin-heading"
      >
        <div
          v-if="!showAllDates"
          class="admin-attendance-overview admin-attendance-overview--today"
        >
          <div class="admin-attendance-actions admin-records-unclocked-controls">
            <button
              class="comic-button"
              type="button"
              :disabled="notClockedInAdminAccounts.length === 0"
              popovertarget="not-clocked-in-popover"
              aria-haspopup="dialog"
              aria-controls="not-clocked-in-popover"
            >
              今日尚未打卡：{{ todayAttendanceSummary.notClockedIn }}人
            </button>
          </div>
        </div>

        <div
          id="not-clocked-in-popover"
          ref="notClockedInPopover"
          class="not-clocked-in-popover"
          popover="auto"
          role="dialog"
          aria-labelledby="not-clocked-in-title"
          @toggle="handleNotClockedInPopoverToggle"
          @keydown.esc.prevent.stop="closeNotClockedInPopover"
        >
          <div class="popover-heading">
          <h2
            id="not-clocked-in-title"
            ref="notClockedInDialogHeading"
            tabindex="-1"
          >
            今日尚未打卡人員
          </h2>
            <button
              class="popover-close-icon"
              type="button"
              aria-label="關閉今日尚未打卡人員名單"
              popovertarget="not-clocked-in-popover"
              popovertargetaction="hide"
            @click="closeNotClockedInPopover"
            >
              ×
            </button>
          </div>
          <ul v-if="notClockedInAdminAccounts.length > 0">
            <li v-for="account in notClockedInAdminAccounts" :key="account">
              {{ account }}
            </li>
          </ul>
          <p v-else>今日所有管理員均已打卡。</p>
          <button
            class="comic-button popover-close-button"
            type="button"
            popovertarget="not-clocked-in-popover"
            popovertargetaction="hide"
            @click="closeNotClockedInPopover"
          >
            關閉
          </button>
        </div>

        <section
          class="admin-records-content"
          :class="{
            'admin-records-content--today': !showAllDates,
            'admin-records-content--all-dates': showAllDates,
          }"
          aria-labelledby="admin-records-list-title"
        >
          <div class="admin-records-toolbar">
            <h2 id="admin-records-list-title">
              {{ showAllDates ? '所有管理員打卡紀錄' : '今日打卡紀錄' }}
            </h2>
            <div class="attendance-filters">
              <div>
                <label for="admin-account-search">管理員帳號：</label>
                <input
                  id="admin-account-search"
                  type="search"
                  v-model.trim="adminAccountSearch"
                  placeholder="Search"
                />
              </div>
              <div v-if="showAllDates">
                <label for="work-date-filter">工作日期：</label>
                <input id="work-date-filter" type="date" v-model="workDateFilter" />
              </div>
              <button
                class="comic-button clear-filters-button"
                type="button"
                @click="clearFilters"
              >
                清除篩選
              </button>
              <button
                v-if="!showAllDates"
                class="comic-button admin-records-mode-toggle"
                type="button"
                @click="toggleAllDates"
              >
                顯示全部日期
              </button>
            </div>
          </div>
          <div
            v-if="showAllDates"
            class="admin-attendance-actions admin-records-mode-controls"
          >
            <button class="comic-button" type="button" @click="toggleAllDates">
              顯示今日
            </button>
          </div>
          <div v-if="showAllDates" class="attendance-summary">
            <span>紀錄總數：{{ attendanceSummary.total }}</span>
            <span>已完成：{{ attendanceSummary.completed }}</span>
            <span>尚未完成：{{ attendanceSummary.incomplete }}</span>
          </div>

        <p
          class="visually-hidden"
          role="status"
          aria-live="polite"
          aria-atomic="true"
        >
          {{ filteredRecordsAnnouncement }}
        </p>
        <p v-if="filteredAllRecords.length === 0" class="empty-state" aria-hidden="true">
          查無符合的打卡紀錄
        </p>

        <!-- 所有管理員打卡紀錄的清單 -->
        <div v-else class="table-scroll" role="region" aria-label="所有管理員打卡紀錄表格" tabindex="0">
        <table class="records-table">
          <caption class="visually-hidden">所有管理員打卡紀錄</caption>
          <thead>
            <tr>
              <th scope="col">工作日期</th>
              <th scope="col">管理員帳號</th>
              <th scope="col">上班時間</th>
              <th scope="col">上班狀態</th>
              <th scope="col">應下班時間</th>
              <th scope="col">下班時間</th>
              <th scope="col">工作時數</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="record in pageAllRecords" :key="record.id">
              <td>{{ formatDate(record.workDate) }}</td>
              <td>{{ record.adminAccount }}</td>
              <td>
                {{
                  record.clockInTime ? formatDateTime(record.clockInTime) : '尚未上班打卡'
                }}
              </td>
              <td>
                {{ record.isBeforeAllowedTime ? '早於09:00' : record.isLate ? '遲到' : '未遲到' }}
              </td>
              <td>
                {{
                  record.expectedClockOutTime
                    ? formatDateTime(record.expectedClockOutTime)
                    : '未設定'
                }}
              </td>
              <td>
                {{ record.clockOutTime ? formatDateTime(record.clockOutTime) : '尚未下班打卡' }}
              </td>
              <td>{{ formatWorkDuration(record.clockInTime, record.clockOutTime) }}</td>
            </tr>
          </tbody>
        </table>
        </div>
        <div v-if="filteredAllRecords.length > 0" class="admin-records-pagination">
          <nav class="record-pagination" aria-label="所有管理員紀錄頁數">
            <button
              class="pagination-arrow"
              type="button"
              aria-label="上一頁"
              :disabled="currentPage === 1"
              @click="currentPage--"
            >
              <img :src="pagePreviousIcon" alt="" aria-hidden="true" />
            </button>
            <ol>
              <li v-for="page in allAdminPageNumbers" :key="page">
                <button
                  type="button"
                  :aria-current="page === currentPage ? 'page' : undefined"
                  :aria-label="`第 ${page} 頁`"
                  :class="{ 'is-current': page === currentPage }"
                  @click="currentPage = page"
                >
                  <span>{{ page }}</span>
                </button>
              </li>
            </ol>
            <button
              class="pagination-arrow"
              type="button"
              aria-label="下一頁"
              :disabled="currentPage === totalPages"
              @click="currentPage++"
            >
              <img :src="pageNextIcon" alt="" aria-hidden="true" />
            </button>
            <span class="visually-hidden" aria-live="polite" aria-atomic="true">
              第 {{ currentPage }} 頁，共 {{ totalPages }} 頁
            </span>
          </nav>
        </div>
        </section>
      </section>
    </template>
      </div>
    </main>
  </div>
</template>

<!-- 樣式 -->
<style scoped>
.success {
  color: #17653b;
}
.error {
  color: #8e2424;
}

main {
  width: 100%;
  /* 代表它使用父層可以提供的全部寬度。 */
}
section {
  margin-bottom: 2rem;
  /* 元素外部下方的空間 */
  /* 1rem = 16px -> 2rem = 32px */

  /*
<section>今日打卡狀態</section>

<section>我的打卡紀錄</section>
|
v
今日打卡狀態
────────────
2rem 空白
────────────
我的打卡紀錄
 */
}
.clock-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: var(--dashboard-space-2);
  margin-top: 1rem;

  /* 
<div class="clock-actions">
  <button>上班打卡</button>
  <button>下班打卡</button>
</div>
|
v
display: flex => 讓裡面的按鈕預設橫向排列 = [上班打卡] [下班打卡]
gap: 12px => 設定 flex 子元素之間的距離 = [上班打卡] ← 12px → [下班打卡]

margin-top: 1rem => 在按鈕區域上方留空白：
下班時間：尚未下班打卡
    |
約 16px 空白
    |
[上班打卡] [下班打卡]
*/
}

table {
  width: 100%;
  /* 讓表格填滿父層可用寬度 */
  /* 沒有設定時，表格可能只依照文字需要的寬度顯示： 
  | 日期 | 上班 | 下班 | */
  /* 設定後會延伸到整個頁面內容寬度： 
  | 工作日期       | 管理員帳號       | 上班時間       | 下班時間       |*/

  border-collapse: collapse;
  /* HTML 表格中，每一格原本各自擁有邊框，可能看起來像雙線： 
  | 日期 || 帳號 || 上班時間 |

  | border-collapse
  v
  會把相鄰儲存格的邊框合併成一條線：
  | 日期 | 帳號 | 上班時間 |
  */
}

th,
td {
  padding: 8px;
  /* padding 是元素邊框與內容之間的內部空間 */
  /* 沒有 padding： |2026/10/17| -加入後-> |  2026/10/17  | */
  /* 四個方向都留 8px：        
      上 8px
左 8px  文字  右 8px
        下 8px 
  */

  border: 1px solid darkgray;
  /* 
1px   → 邊框粗細
solid → 實線
#999  → 灰色
===> 所以每個表格欄位會有一圈灰色實線 */

  text-align: left;
  /* 文字靠左對齊 */
  /* 
| 工作日期          |
| 2026/10/17        | */
}

.pagination-controls {
  display: flex;
  /* 橫向排列 */
  /* [上一頁]　第 1 / 5 頁　[10 筆 ▼]　[下一頁] */

  align-items: center;
  /* 讓按鈕、文字和選單在垂直方向置中對齊 */
  /* 沒有設定時，不同高度的元素可能看起來上下不齊 */

  flex-wrap: wrap;
  /* 當畫面寬度不夠時，允許元素換到下一行：
  [上一頁]　第 1 / 5 頁
[10 筆 ▼]　[下一頁] */
  /* 若沒有設定，預設是：flex-wrap: nowrap;
  -> 元素可能被擠在同一行或超出容器 */

  gap: 8px;
  /* 設定每個分頁元素之間有 8px 的距離 */

  margin-top: 16px;
  /* 在分頁控制區上方留出 16px 空間，避免它緊貼表格 */
}

.pagination-controls button,
.pagination-controls select {
  padding: 6px 10px;
  /* 上下內距：6px
    左右內距：10px */
  /* 讓按鈕和選單不會緊貼文字，也比較容易點擊 */
}

.pagination-controls button:disabled {
  cursor: not-allowed;
  /* 滑鼠移到停用按鈕上時，顯示禁止操作的游標 */

  opacity: 0.5;
  /* 把按鈕透明度降到一半，使它看起來像不可使用。 */
}

/* 浮動按鈕 */
#not-clocked-in-popover {
  box-sizing: border-box;
  /* 設定的寬度會包含內距與邊框。 */
  width: min(488px, calc(100vw - 32px));
  /*大螢幕最多寬 360px；手機寬 400px 時，面板寬度最多是 360px，左右留出空間*/
  max-height: 50vh;
  /* 面板最高是視窗高度的一半。例如視窗高 800px，面板最多高 400px。名單少時，面板會自然縮短 */
  overflow-y: auto;
  /* 內容超過最大高度時，面板內才會出現垂直捲動；內容少時不會出現捲軸。 */
  overflow-wrap: anywhere;
  /* 遇到很長、沒有空格的帳號時允許換行，避免文字跑出面板。 */
}

.admin-clock-page .today-status-table th,
.admin-clock-page .today-status-table td,
.admin-clock-page .records-table th,
.admin-clock-page .records-table td {
  border-right: 0;
  border-left: 0;
}

.admin-clock-page .today-status-table th {
  border-top: 0;
  border-bottom: 1px solid var(--dashboard-border);
}

.admin-clock-page .today-status-table td {
  border-top: 1px solid var(--dashboard-border);
  border-bottom: 0;
}

.admin-clock-page .records-table th {
  border-top: 0;
  border-bottom: 2px solid var(--dashboard-border);
}

.admin-clock-page .records-table td {
  border-top: 0;
  border-bottom: 1px solid var(--dashboard-border);
}
</style>

<style scoped src="../assets/admin-clock.css"></style>
