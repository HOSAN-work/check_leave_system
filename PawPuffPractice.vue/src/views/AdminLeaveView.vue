<!-- 1. 匯入會使用的工具： 
import { } from 'vue' 
import api from '@.services/api'  -->

<!-- 2. 先想畫面需要記住什麼，再建立狀態
| 狀態            | 保存什麼                   |
|---             |---                        |
| `records`      | 多筆請假紀錄，所以初始值是陣列 |
| `isLoading`    | 是否正在等待 API            |
| `errorMessage` | 查詢失敗時要顯示的文字        | -->

<!-- 3. 寫取得資料的函式
async function xxx(){....} -->

<!-- 4. 讓頁面開啟後自動查詢
onMounted(loadMyLeaves) -->

<!-- 5. 最後用狀態決定畫面
<template>
<main>
</main>
</template> -->
<!-- ========================== -->
<!-- 資料與功能 -->
<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { RouterLink } from 'vue-router'
import { useAuth } from '@/composables/useAuth.js'
import { createAdminLeaveConnection } from '@/services/adminLeaveConnection'
import api from '@/services/api'
import { getCalendar } from '@/services/calendar'
import '../assets/admin-clock.css'
import '../assets/admin-leave.css'
import '../assets/drip-button.css'
import dashboardPaper from '../assets/dashboard-paper.png'
import dashboardProfile from '../assets/profile.png'
import navClockIcon from '../assets/dashboard-nav-clock.svg'
import navUserIcon from '../assets/dashboard-nav-user.svg'
import navLeaveIcon from '../assets/dashboard-nav-leave.svg'
import navAdminIcon from '../assets/dashboard-nav-admin.svg'

const { currentUser, logout } = useAuth()

// 畫面：
// 1. 統計當年度各假別請假時數 => 只計算「已核准」時數 => span
//
// 2. 自己的請假紀錄:
// 表格欄位：| 假別 | 請假日期 | 開始時間 | 結束時間 | 時數 | 審核狀態 |
//
// 3. 請假視窗

// 定義當年度欄位
// new Date() 取得目前電腦日期與時間
// .getFullYear() 取出年份
const currentYear = new Date().getFullYear()
// 定義請假視窗
// 一開始 null 表示元素還沒掛載；掛載後，Vue 會把 <dialog> 元素放進這個 ref。
const leaveDialog = ref(null)

// 行事曆彈窗
const calendarDialog = ref(null)

// 因為「新增」和「修改」共用同一個視窗，程式需要記住：這次要新增假單，還是修改哪一筆假單。
// null：新增
// 有編號：修改該筆假單
const editingId = ref(null)

// 請假視窗記住你輸入的資料，準備送出申請。
// 請假視窗正在填寫的一筆新申請
// records 保存已送出的多筆假單；leaveForm 保存視窗正在填寫的一筆申請。
const leaveForm = ref({
  leaveType: '',
  classType: '',
  startTime: '',
  endTime: '',
  reason: '',
})

// 把請假視窗準備"送出"
//
// 是否正在送出這一筆請假申請
const isSubmitting = ref(false) //初始,否

// 請假視窗內的送出錯誤
const submitError = ref('')

// 按「申請請假」時，明確切回新增模式並清空表單。
function openLeaveDialog() {
  // 是否正在送出 || 刪除哪一筆假單
  if (isSubmitting.value || deletingId.value !== null) return

  editingId.value = null
  // isSubmitting.value = false -> 尚未送出時
  submitError.value = '' //清除舊錯誤

  leaveForm.value = {
    leaveType: '',
    classType: '',
    startTime: '',
    endTime: '',
    reason: '',
  }

  leaveDialog.value.showModal() //開啟視窗
}

// 點「修改」按鈕後，把那筆假單填進原本的請假視窗，再開啟視窗
function openEditDialog(record) {
  // 是否正在送出 || 刪除哪一筆假單 -> 正在送出 或 刪除某一筆假單 => return
  if (isSubmitting.value || deletingId.value !== null) return

  editingId.value = record.id //記住修改對象
  submitError.value = '' //清除舊錯誤

  // 把舊假單內容複製到表單
  leaveForm.value = {
    // 視窗欄位 : 原假單資料
    leaveType: record.leaveType,
    classType: record.classType,

    // slice(0, 16) 把日期時間轉成輸入框目前使用的分鐘格式：2026-10-28T09:00:00
    startTime: record.startTime.slice(0, 16),
    endTime: record.endTime.slice(0, 16),
    reason: record.reason,
  }
  // 開啟已填好內容的視窗
  leaveDialog.value.showModal()
}

// 送出請假申請函式
async function submitLeave() {
  // 阻止「上一筆還沒送完，又送一次」
  if (isSubmitting.value) return //true -> 正在送出

  submitError.value = ''

  if (new Date(leaveForm.value.endTime) <= new Date(leaveForm.value.startTime)) {
    submitError.value = '結束時間必須晚於開始時間'
    return
  }
  // 執行順序：
  // 1. 第一次按送出，目前是 false => if(false)
  // 時間檢查通過後：isSubmitting.value = true
  //
  // 2. API 還沒完成，又按第二次，目前仍是 true => if (true)，第二次呼叫立即結束
  isSubmitting.value = true

  try {
    // 新增假單
    // leaveForm.value 是傳入 API 的資料，也就是從前端「送出給後端」的請假表單內容。
    // api.post(後端網址, 要送出的資料)
    if (editingId.value === null) {
      await api.post(`/api/AdminLeaves/submit`, leaveForm.value)
    }
    // 修改假單
    else {
      await api.put(`/api/AdminLeaves/${editingId.value}`, leaveForm.value)
    }

    leaveDialog.value.close()

    leaveForm.value = {
      leaveType: '',
      classType: '',
      startTime: '',
      endTime: '',
      reason: '',
    }

    await loadMyLeaves()
  } catch (error) {
    const data = error.response?.data

    // 假設：
    // DTO 驗證失敗時，預設回應通常類似：
    // {
    //   type: '...',                  // 錯誤類型的說明連結
    //   title: 'One or more validation errors occurred.',
    //   status: 400,                  // HTTP 狀態碼
    //   errors: {
    //     ClassType: ['請選擇有效班別'],
    //     Reason: ['請填寫原因']
    //   },
    //   traceId: '...'                // 用來追查這次請求
    // }
    //
    // ---> const data = error.response?.data
    // data 就代表：
    // {
    //   type: '...',
    //   title: 'One or more validation errors occurred.',
    //   status: 400,
    //   errors: {
    //     ClassType: ['請選擇有效班別'],
    //     Reason: ['請填寫原因']
    //   },
    //   traceId: '...'
    // }
    //
    // ---> data?.errors
    // {
    //   ClassType: ['請選擇有效班別'],
    //   Reason: ['請填寫原因']
    // }
    //
    // ---> Object.values(data.errors) => 取出各欄位的值，組成陣列
    // [
    //   ['請選擇有效班別'],
    //   ['請填寫原因']
    // ]
    //
    // ---> .flat() 是把「陣列裡的陣列」攤平一層。
    // ['請選擇有效班別', '請填寫原因']
    //
    // ---> .join(';') 連成一段文字 => 字串
    // 請選擇有效班別；請填寫原因
    //
    const validationMessages = data?.errors ? Object.values(data.errors).flat().join('；') : ''
    const responseMessage =
      typeof data === 'string' ? data : data?.message || data?.detail || data?.title

    if (!error.response) {
      submitError.value = '無法連線到後端，請確認 API 是否正在執行'
    } else {
      submitError.value =
        responseMessage || validationMessages || `送出請假申請失敗（HTTP ${error.response.status}）`
    }
    // 為何不是data?.errors 而是 data?. message ?
    // 因為這裡要選的是「能直接顯示的文字」；data?.errors 是物件，不是整理好的文字。
    //
    // 1. Controller 回傳錯誤
    // data = { message: '請假時間不在班別內' }
    // validationMessages = ''
    // ===> '請假時間不在班別內' || '' || '送出請假申請失敗'
    // ===> 第一個已有文字，所以顯示：請假時間不在班別內
    //
    // 2. DTO 驗證失敗
    // data = {
    //   errors: { ClassType: ['請選擇有效班別'] }
    // }
    // validationMessages = '請選擇有效班別'
    // ===> undefined || '請選擇有效班別' || '送出請假申請失敗'
    // ===> 第一個沒有值，改取第二個，所以顯示：請選擇有效班別
    //
    // 3. 斷網，沒有後端回應
    // data = undefined
    // validationMessages = ''
    // ===> undefined || '' || '送出請假申請失敗'
    // ===> 前兩個都沒有可用文字，所以顯示：送出請假申請失敗。
    //
  } finally {
    isSubmitting.value = false
  }
}

// 定義假別分類
const leaveTypes = [
  { value: 1, label: '病假' },
  { value: 2, label: '事假' },
  { value: 3, label: '公假' },
  { value: 4, label: '婚假' },
  { value: 5, label: '喪假' },
]

// 保存「自己已送出的多筆請假紀錄」表單
const records = ref([])

// 要向後端「查詢自己的請假紀錄」，需要等待回應，所以用 isLoading 記住「現在是否正在等待」
const isLoading = ref(false)

const errorMessage = ref('')

// 從後端載入自己的請假紀錄到前端
async function loadMyLeaves() {
  isLoading.value = true
  errorMessage.value = ''

  try {
    // HttpGet(me) 查詢自己的請假紀錄 -> 存入response
    // response.data.records 有資料的話 -> 存入 records陣列
    // response.data.records 沒資料的話 -> [] -> 存入records陣列
    const response = await api.get('/api/AdminLeaves/me')
    records.value = response.data.records ?? []

    // 載入需要的日曆
    await loadCalendarsForRecords(records.value)
  } catch (error) {
    errorMessage.value = error.response?.data?.message ?? '取得請假紀錄失敗'
  } finally {
    isLoading.value = false
  }
}

// 5.
// onMounted(loadMyLeaves) //vue畫面顯示時,自動執行「載入自己請假紀錄」函式
// onMounted(() => {
//   loadMyLeaves()
//   loadPendingReviewLeaves()
// })

// 建立連線物件
const leaveConnection = createAdminLeaveConnection()
const realtimeError = ref('') //保存即時連線的錯誤文字

async function refreshLeaveData() {
  await Promise.all([loadMyLeaves(), loadPendingReviewLeaves()])
}

// 「收到 LeavesChanged 通知時，執行 refreshLeaveData。」
// 「等通知再更新」，所以refreshLeaveData不加括號
// .on(...) 註冊事件處理，告訴它收到通知後要做什麼
leaveConnection.on('LeavesChanged', refreshLeaveData)

// 開始重連
leaveConnection.onreconnecting(() => {
  realtimeError.value = '即時連線中斷,正在重新連線; 目前資料可能尚未更新'
})

// 重連成功
// 「斷線後重新連線成功，就清除錯誤提示，並重新查詢假單。
// 你斷線 → 別人審核了假單 → 你沒收到通知 → 重新連線成功 → 主動查詢 → 畫面取得最新狀態
// .onreconnected(...)：註冊「重連成功時」要執行的動作，不是第一次連線成功。
leaveConnection.onreconnected(async () => {
  realtimeError.value = ''
  await refreshLeaveData()
})

// 放棄重連
// 「連線關閉時，把提示文字存起來，讓畫面告訴使用者改用手動更新。」
// 即時連線中斷後，畫面可能仍顯示舊資料；要提醒使用者「目前不會自動更新」
// .onclose(...) 設定連線關閉後要執行的動作
leaveConnection.onclose(() => {
  realtimeError.value = '即時連線已中斷，請使用更新按鈕'
})

onMounted(async () => {
  try {
    // 開始連接 SignalR 後端，並等待連線結果。
    await leaveConnection.start()
  } catch (error) {
    realtimeError.value = '即時連線失敗,仍可使用更新按鈕'
    console.error('請假即時連線失敗', error)
  }

  // 即使連線失敗，仍載入原本的假單資料
  await refreshLeaveData()

  // 測試
  // 「請假頁能不能拿到日曆？」
  //  請假頁能從整年日曆裡找出「指定日期」的資料?
  // try {
  //   const calendar = await getCalendar(2027)

  //   const selectedDate = '2027-01-01'
  //   const dateKey = selectedDate.replace(/-/g, '')
  //   // 找到全部的 - 被空字串取代 -> '20270101'
  //   // /-/：找連字號 -
  //   // g：找全部
  //   const day = calendar.days.find((item) => item.date === dateKey)
  //   // .find() 負責找資料：遇到第一筆讓條件回傳 true 的資料，就回傳整筆資料；全部找不到則回傳 undefined
  //   //  '20270101' === '20270101' -> true , day='20270101'
  //   console.log('指定日期：', selectedDate, '是否放假：', day?.isHoliday)
  //   // 指定日期：20270101, 是否放假：true
  //   console.log('日曆年份：', calendar.year, '日期筆數：', calendar.days.length)
  //   // 日曆年份：2027,日期筆數：365
  // } catch (error) {
  //   console.error('載入日曆失敗', error)
  // }
})

// 日曆 API 讀的是 CSV，不是資料庫。也不是每支 API 都要寫 async
// 1. 前端等的是 HTTP 回應。 await getCalendar(year) 是等後端把日曆送回來。後端從 CSV、資料庫，還是記憶體拿資料，都不影響前端這一步要等回應。
// 2. 後端等的是檔案讀取。 你的日曆後端使用 await File.ReadAllTextAsync(...) 讀 CSV，所以讀取方法用了 async
//
// 「這段讀檔是在什麼時候執行？」
// 1. 每次有人呼叫 API 時讀檔
// 像你的日曆 API，可能同時有多個請求。既然 .NET 有現成的非同步讀檔方法，就選 ReadAllTextAsync，避免讓處理請求的執行緒一直等檔案。
// 2. 自己跑一次的小工具讀檔
// 例如在終端機把一份 CSV 轉成 JSON，跑完就結束。沒有其他請求要處理，用 ReadAllText 就可以。
//
// 最簡單的選法是：要等外部資料，就用現成的非同步方法；資料已經在手上，就直接處理同步。

// 「拿到整年的日曆」
// const calendar = {
//   year: 2027,
//   days: [
//     { date: '20270101', isHoliday: true },
//     { date: '20270102', isHoliday: true },
//   ],
// }
const calendarsByYear = ref({}) //空物件
// 還沒載入任何日曆
// {}
//
//  成功載入 2027 年後
// { "2027": { year: 2027, days: [] } }
//
//  又成功載入 2028 年後
// {
//   "2027": { year: 2027, days: [] },
//   "2028": { year: 2028, days: [] }
// }

// loadLeaveCalendar(2027)
// calendarsByYear.value[2027] -> 有沒有2027年資料
// 有 -> return calendarsByYear.value[2027]
//
// 沒有 -> await getCalendar(2027) -> 存入calendar
// calendar存入calendarsByYear.value[2027]
// return calendar這次物件 和 return calendarsByYear 一樣意思
//
// calendarsByYear.value[year]      // 用變數的值找 "2027"，得到整份日曆
// { "2027": { year: 2027, days: [] } }
//
// calendarsByYear.value[year].year // 再讀那份日曆的 year 欄位，得到數字 2027
// "2027": { year: 2027,....} => 2027
//
// calendarsByYear.value.year       // 直接在外層找 "year"，找不到，得到 undefined
// 找外層"year" ,所以與"year"同層的是"2027","2028",....不同年份,但是找不到"year" => undefined
// {
//   "2027": { ......},
//   "2028": { ...... }
// }
//
async function loadLeaveCalendar(year) {
  if (calendarsByYear.value[year]) {
    return calendarsByYear.value[year]
  }

  const calendar = await getCalendar(year) // 拿到日曆-> 存入calendar物件
  calendarsByYear.value[year] = calendar // calendar物件存入calendarsByYear.value[2027]
  return calendar //手上已經有這份資料，不必先存進快取，再從快取-> calendarsByYear.value[2027]拿出來回傳。
}

// 現在行事曆只顯示2027年1月
const calendarDays = ref([]) //「某一年的每日清單」
const calendarMessage = ref('')

async function openCalendarDialog() {
  calendarDialog.value.showModal()
  calendarMessage.value = '載入中...'

  try {
    const calendar = await loadLeaveCalendar(2027)
    calendarDays.value = calendar.days.filter((day) => day.date.startsWith('202701'))
    calendarMessage.value = ''
  } catch (error) {
    calendarMessage.value = '無法載入行事曆' //讓使用者在日曆彈窗看到「無法載入行事曆」
    console.error('載入2027年行事曆失敗：', error) //讓你在瀏覽器 Console 看實際錯誤，例如 API 回了 404 或連不上後端。它不會把細節顯示在彈窗裡
  }
}

// 原本是「每天都套用班別時段，再扣掉午休」 calculateLeaveHours()
// 修改為以下：
// 「跨日假單扣掉中間假日」
// 例如：早班0900-1800 -> 2027/01/08（五）09:00 到 01/11（一）18:00
// 讀到假單 → 看出它是 2027 年的 → 取得 2027 年日曆
// → 跳過週六、週日 → 只算週五和週一，共 16 小時
//
// "看假單用到哪些年份，載入那些年份的日曆"
async function loadCalendarsForRecords(leaveRecords) {
  // Set 讓同一張列表即使有多筆 2027 年假單，也只安排一次 2027 年查詢。
  const years = new Set()

  for (const record of leaveRecords) {
    // 每筆假單的開始時間取出年份
    // .getFullYear()，因為 new Date(...) 建立的是 JavaScript 的 Date 物件。
    // 假設 record.startTime 是 "2027-05-10T09:00:00"
    // .getFullYear() = 2027
    // .getYear() => 回傳的是年份減 1900 = 2027-1900 = 127
    // .year() => 	報錯，JavaScript 原生 Date 沒有這個方法
    const startYear = new Date(record.startTime).getFullYear()
    const endYear = new Date(record.endTime).getFullYear()

    for (let year = startYear; year <= endYear; year++) {
      years.add(year)
      //  year = 2027 ; 2027 <= 2027 ; 2027++
      // `years.add(2027)` 是把 2027 記進集合
    }
  }
  // Promise.allSettled() 是「等每一筆查詢都有結果，不管成功或失敗」
  // 就是同時查 2026、2027 年的日曆；即使其中一年查不到，還是能拿到另一年成功的資料。
  // 跟 Promise.all() 的差別：`all()` 遇到任何一筆失敗，外層的 await 就會拋出錯誤；`allSettled()` 則等全部完成，再讓你逐筆看成功或失敗。
  await Promise.allSettled([...years].map((year) => loadLeaveCalendar(year)))
  // [...years] 把 Set 展開成陣列：[2026, 2027,...]
  // map() 是「逐筆」 -> 每個年份呼叫一次 loadLeaveCalendar()
  // await Promise.allSettled([loadLeaveCalendar(2026), loadLeaveCalendar(2027)])
}

// 「選好"請假開始時間"時，查那一天是不是假日」
async function checkLeaveStarDate() {
  const startTime = leaveForm.value.startTime
  if (!startTime) return //空的,結束

  const selectedDate = startTime.slice(0, 10)
  const year = selectedDate.slice(0, 4)
  // int.TryParse() 是 C# 的寫法；這一行在 Vue 的 JavaScript 裡，所以用 Number()。
  // 但 getCalendar() 把年份放進 `/api/calendar/${year}` 時，兩種值組出的網址都一樣。後端的 int year 會從網址取得整數，不需要前端先轉。
  const dateKey = selectedDate.replace(/-/g, '')
  // "2027=01-01" -> "20270101"

  try {
    const calendar = await getCalendar(year)
    const day = calendar.days.find((item) => item.date === dateKey)
    console.log('指定日期：', selectedDate, '是否放假：', day?.isHoliday)
  } catch (error) {
    // await getCalendar(year) -> error -> 拿不到日曆物件
    console.error('查不到日曆：', selectedDate, error)
  }
}

//「離開頁面、元件被卸載時，清掉通知處理並關閉即時連線」
// 元件被銷毀時，Vue 自動執行裡面的程式。
onUnmounted(() => {
  // .off():取消先前的註冊
  // 即使收到 LeavesChanged，也不再執行 refreshLeaveData()
  leaveConnection.off('LeavesChanged', refreshLeaveData)
  leaveConnection.stop().catch(console.error)
  // .stop()：主動關閉連線，回傳 Promise。
  // 整條連線關閉，所有即時通知都收不到；
  //
  // .catch(console.error)：如果關閉失敗，把錯誤印到 Console
})

// 假別-> 對應假別名稱
// find() 找出編號對應的假別
// ?.label 取得名稱
//
// #region 例如： getLeaveTypeText(1)
// leaveTypes.find((1) => type.value === 1)?.label ?? '未知假別'
// find() => 找到後return {value:1,label:'病假'}
// ?.label 取出'病假'
//  return '病假' ?? '未知假別'
// return '病假'
// #endregion
function getLeaveTypeText(value) {
  return leaveTypes.find((type) => type.value === value)?.label ?? '未知假別'
}
// 假單審核狀態
// #region 例如：getApprovalStatusText(5)
// labels[5] -> undefined
// return undefined ?? '未知狀態'
// return '未知狀態'
// #endregion
function getApprovalStatusText(value) {
  const labels = {
    1: '待審核',
    2: '已核准',
    3: '已駁回',
    4: '已取消',
  }
  // labels[value] 依編號取得審核文字
  return labels[value] ?? '未知狀態'
}

// 時間規則
// 計算請假時間（開始時間,結束時間）
function calculateLeaveHours(startTime, endTime, classType) {
  // 工作時段
  const workPeriods =
    classType === 1 // 是早班嗎？
      ? [
          [9, 12],
          [13, 18],
        ] // 是 → 使用早班時段
      : classType === 2 // 否 → 再問：是晚班嗎？
        ? [
            [10, 12],
            [13, 19],
          ] // 是 → 使用晚班時段
        : null // 都不是 → 沒有有效班別

  if (workPeriods === null) return 0
  // #region 等同於：
  // let workPeriods
  // if (classType === 1) {
  //   workPeriods = [
  //     [9, 12],
  //     [13, 18],
  //   ]
  // } else if (classType === 2) {
  //   workPeriods = [
  //     [10, 12],
  //     [13, 19],
  //   ]
  // } else {
  //   workPeriods = null
  // }
  // #endregion

  // ＊＊＊把字串轉成日期，檢查資料有效＊＊＊
  const start = new Date(startTime)
  // const start = new Date('2026-10-22T09:00:00')
  const end = new Date(endTime)
  // const end = new Date('2026-10-22T12:00:00')

  // const start = new Date('不是日期'）
  // start.getTime() //NaN
  // Number.isNaN(start.getTime()) //true

  // getTime() 會把日期時間轉成數字，單位是「毫秒」
  // start not a number? || end not a number? || end<=start ?
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || end <= start) {
    return 0
  }

  // 「逐日計算請假時數」：先建立總時數累加器，再從開始日期的凌晨出發。
  let totalMilliseconds = 0 // 用來累加請假時間，單位是毫秒

  // 複製開始時間，建立另一個日期物件
  const day = new Date(start) // 兩個物件分開，之後修改 day，不會改到 start

  day.setHours(0, 0, 0, 0) // setHours(小時, 分鐘, 秒, 毫秒)

  // 執行後：
  // start：2026/10/22 09:30 ← 原本開始時間保留
  // day：  2026/10/22 00:00 ← 用來逐日計算的日期

  // 逐天檢查,每天都算上午、下午時段
  while (day < end) {
    // 「這天在行事曆上有放假嗎？」
    //
    // 例如： day 是 2027 年 1 月 5 日
    // day.getFullYear():2027
    // getMonth()從0開始 -> 1月是0 ,所以要+1 -> 1月是1再轉string -> 不滿兩碼就在左邊補 0 -> "01"
    // getDate() 取出這個月的第?天,從1開始 ,所以不用+1 -> 再轉成string -> 不滿兩碼就在左邊補 0 -> "05"
    // dateKey = `${2027}${01}${05}` => "20270105"
    const year = day.getFullYear() //取出年份
    const month = String(day.getMonth() + 1).padStart(2, '0')
    const date = String(day.getDate()).padStart(2, '0')
    const dateKey = `${year}${month}${date}`
    //dateKey 只告訴你「今天是哪一天」，沒有告訴你「這天是不是假日」
    // // JavaScript：用反引號包住，每個變數各自寫成 ${...}
    // $"{year}{month}{date}" 是 C# 的寫法

    // 在整年的日曆裡，找出 dateKey 對應的那一天
    const calendarDay = calendarsByYear.value[year]?.days.find((item) => item.date === dateKey)
    // 找不到這一天的日曆資料，整張假單的時數無法確認。
    if (!calendarDay) return null

    // 「如果今天是假日，今天不加時數，下一輪從隔天開始。」
    // 對應的那一天是不是假日
    // true, day.setDate(5 + 1) = day.setDate(6) 移到隔天 -> 再continue 跳過今天的上下午時段計算
    if (calendarDay?.isHoliday) {
      day.setDate(day.getDate() + 1)
      continue
    }

    // 假設這張假單選的是 09:00～18:00 班別，`workPeriods` 就是：
    // const workPeriods = [[9, 12], [13, 18]]
    //
    // 第一輪： 上午時段
    // for(const [9,12] of workPeriods)
    // startHour = 9 , endHour = 12
    //
    // 第二輪： 下午時段
    // for(const [13,18] of workPeriods)
    // startHour = 13 , endHour = 18
    //
    // 例如：上午時段
    // const start = new Date(startTime) = 20270101
    // const day = new Date(start) = 20270101
    // day.setHours(0, 0, 0, 0) = 20270101 00:00:00:00
    // const periodStart = 20270101 00:00:00:00
    // periodStart.setHours(9,0,0,0) = 20270101 09:00:00:00
    //
    // const periodEnd = 20270101 00:00:00:00
    // periodEnd.setHours(12, 0 ,0 ,0) = 20270101 12:00:00:00
    //
    // 「這一天的上午和下午，各有多少請假時間落在工作時段內？」
    for (const [startHour, endHour] of workPeriods) {
      // 建立這一天的工作時段起點；
      const periodStart = new Date(day)
      periodStart.setHours(startHour, 0, 0, 0)

      // 建立這一天的工作時段終點；
      const periodEnd = new Date(day)
      periodEnd.setHours(endHour, 0, 0, 0)

      // 算「上午 09:00～12:00」裡，你請了多少假，所以最多只能算到 12:00
      //
      // Math.max() 開始時間取較晚的
      // -> 請假開始 11:00、上午開始 09:00 → 取 11:00
      //
      // Math.min() 結束時間取較早的
      // -> 請假結束 14:00、上午結束 12:00 → 取 12:00
      //
      // 上午交集是 11:00～12:00；
      // 下午同樣算出 13:00～14:00
      // => 11:00~12:00 1小時
      // ----------------------------------
      // 算「下午 14:00～17:00」裡，你請了多少假，所以最多只能算到 18:00
      //
      // Math.max() 開始時間取較晚的
      // -> 請假開始 14:00、下午開始 13:00 → 取 14:00
      //
      // Math.min() 結束時間取較早的
      // -> 請假開始 17:00、下午結束 18:00 -> 取 17:00
      // => 14:00~17:00 3小時
      //
      const overlapStart = Math.max(start.getTime(), periodStart.getTime())
      const overlapEnd = Math.min(end.getTime(), periodEnd.getTime())

      // 毫秒,「算出這個時段的請假長度，再累加到總時間；沒有重疊就加 0」
      // Math.max(0,算出的時間）-> 不允許負數
      // 3,600,000 毫秒 = 1 小時
      totalMilliseconds += Math.max(0, overlapEnd - overlapStart)
      // 例如交集是 13:00～14:00：
      // overlapEnd - overlapStart = 3,600,000 毫秒 -> 1小時
      // Math.max(0,3600000) = 3600000 -> 有重疊
      // Math.max(0,-3600000) = 0 -> 沒有重疊，可能算出負數
      //
      // totalMilliseconds += 3600000
      // totalMilliseconds = totalMilliseconds + 3600000 = 0 + 3600000 = 3600000
    }
    // 當天上午、下午都算完後，把日期移到下一天，再由 while 判斷是否繼續。
    day.setDate(day.getDate() + 1)
  }
  // 顯示改成「幾小時」
  // 一小時是 60 × 60 × 1000 = 3600000 毫秒
  // toFixed(2) 把數字四捨五入到小數點後兩位，並回傳字串
  // (1.666666).toFixed(2) // '1.67'
  // (8).toFixed(2) // '8.00'
  // 除完換成小時，toFixed(2) 保留兩位小數，但會得到字串；外面的 Number() 再轉回數字
  // return Number((totalMilliseconds / 3600000).toFixed(2))

  // 顯示「幾小時」
  return totalMilliseconds / 3600000
}

// 顯示改成「幾小時幾分鐘」
function formatLeaveDuration(startTime, endTime, classType) {
  const leaveHours = calculateLeaveHours(startTime, endTime, classType) //請假時數

  if (leaveHours === null) {
    return '缺少日曆,無法計算'
  }

  const totalMinutes = Math.round(leaveHours * 60) //總分鐘

  const hours = Math.floor(totalMinutes / 60)
  const minutes = totalMinutes % 60

  return `${hours}小時 ${minutes} 分鐘`
}
// 年度各假別統計
// 今年每種假別，已核准的請假總時數」，並依每張假單的班別扣除午休
const annualLeaveSummary = computed(() => {
  // 例如：
  // | 假單 | 假別 | 日期 | 狀態 | 工作時數     |
  // |---  |---   |---  |---  |---:         |
  // | A | 病假 | 2026/10/21 | 已核准 `2` | 3 |
  // | B | 病假 | 2026/10/22 | 已核准 `2` | 2 |
  // | C | 病假 | 2026/10/23 | 待審核 `1` | 8 |
  // | D | 事假 | 2026/10/24 | 已核准 `2` | 4 |
  // | E | 病假 | 2025/10/21 | 已核准 `2` | 8 |
  //
  // 1.算「今年」，先訂出今年的範圍

  // new Date(年份, 月份, 日期)
  // 月份從 0 開始，日期則從 1 開始
  // 今年開始 2026/01/01 00:00 ──計算這段期間 ── 明年開始 2027/01/01 00:00

  const yearStart = new Date(currentYear, 0, 1).getTime()
  const yearEnd = new Date(currentYear + 1, 0, 1).getTime()

  // map() 第一次處理病假、第二次處理事假……每換一種假別
  // totalHours 都從 0 開始，避免病假與事假混在一起
  return leaveTypes.map((type) => {
    let totalHours = 0

    // 逐筆檢查假單
    // 在 <script setup> 裡，要寫 records.value，不是直接寫 records
    for (const record of records.value) {
      // type 來自你定義的假別：{ value: 1, label: '病假' }
      // type.value // 1，數字
      // type.label // '病假'，文字
      //
      // 後端回傳的假單 record
      // {
      //   id: 7,
      //   leaveType: 1,
      //   approvalStatus: 2, //已審核
      // }
      //
      if (record.leaveType !== type.value || record.approvalStatus !== 2) {
        continue // 跳過目前這筆，接著檢查下一筆，不會結束整個函式。
      }
      // 開始時間 = Math.max(假單開始時間,今年/01/01 00:00)
      const start = Math.max(new Date(record.startTime).getTime(), yearStart)
      // 結束時間 = Math.min(假單結束時間,明年/01/01 00:00)
      const end = Math.min(new Date(record.endTime).getTime(), yearEnd)

      // 結束時間 > 開始時間
      if (end > start) {
        const leaveHours = calculateLeaveHours(start, end, record.classType)

        // 加總某個假別的時數。算得出來就加進總數；如果有一張算不出來，就把這個假別的總時數標成 null，避免顯示一個不完整的總數。
        if (leaveHours === null) {
          return {
            value: type.value,
            label: type.label,
            hours: null,
          }
        }
        totalHours += leaveHours
      }
    }

    const totalMinutes = Math.round(totalHours * 60)

    return {
      value: type.value,
      label: type.label,
      hours: Math.floor(totalMinutes / 60),
      minutes: totalMinutes % 60,
    }
  })
})

// 待審核假單 -> 軟刪除
const deletingId = ref(null) //記住正在刪除哪筆
const deleteError = ref('')
const deleteDialog = ref(null)
const pendingDeleteRecord = ref(null)

function requestDeleteLeave(record) {
  pendingDeleteRecord.value = record
  deleteDialog.value?.showModal()
}

function cancelDeleteLeave() {
  if (deletingId.value !== null) return
  deleteDialog.value?.close()
  pendingDeleteRecord.value = null
}

async function confirmDeleteLeave() {
  if (!pendingDeleteRecord.value) return
  await deleteLeave(pendingDeleteRecord.value)
  deleteDialog.value?.close()
  pendingDeleteRecord.value = null
}

// 傳入那一筆record(假單)=> deleteLeave
async function deleteLeave(record) {
  if (deletingId.value !== null) return
  // 正在刪除 deletingId.value = 7 -> 7!==null -> true => 立即結束這次函式，不再送出刪除請求
  // 確認彈窗
  // 確認按鈕 -> true -> !true = false
  // 取消按鈕 -> false -> !false = true => return 立即結束
  //
  // window.confirm('確定刪除這筆待審核假單？') => 瀏覽器就會顯示內建視窗
  // ┌────────────────────────────┐
  // │ 確定刪除這筆待審核假單？       │
  // │                            │
  // │          [取消]  [確定]     │
  // └────────────────────────────┘

  deleteError.value = ''
  deletingId.value = record.id // 記住「目前正在刪除哪一筆假單」
  // 例如目前正在刪除第 7 筆：deletingId.value = 7
  // 這列的 record.id = 5 --> 7 === 5 -> false => 顯示「刪除」
  // 這列的 record.id = 7 --> 7 === 7 -> true => 顯示「刪除中」
  // 這列的 record.id = 9 --> 9 === 5 -> false => 顯示「刪除」

  try {
    // 等待後端把該筆假單的 IsDeleted 改為 true。此時資料庫已更新，前端的 records 還是原本的資料。
    await api.delete(`/api/AdminLeaves/${record.id}`)
    await loadMyLeaves() //重新向後端查詢自己的請假紀錄
  } catch (error) {
    deleteError.value = error.response?.data?.message ?? '刪除請假紀錄失敗'
  } finally {
    deletingId.value = null
  }
}

// 查詢「可由我審核的假單」
// 先想「畫面要取得什麼、等待時顯示什麼、失敗時顯示什麼」
// => 要查什麼 → 畫面要記住什麼 → 開始／成功／失敗／結束 → 何時呼叫
//
// 2. 畫面需要記住：查到哪些假單？/ 還在等待嗎？/ 查詢出了什麼錯？
const pendingReviewRecords = ref([])
const isReviewLoading = ref(false)
const reviewError = ref('')

// 3. 可由我審核的假單->查詢開始與結束
async function loadPendingReviewLeaves() {
  isReviewLoading.value = true
  reviewError.value = ''

  try {
    // 1.
    const response = await api.get(`/api/AdminLeaves/pending-review`)
    // response 通常保存「後端回傳給前端的回應」

    // 4.
    pendingReviewRecords.value = response.data.records ?? []

    // 載入需要的日曆
    await loadCalendarsForRecords(pendingReviewRecords.value)
  } catch (error) {
    reviewError.value = error.response?.data?.message ?? '取得待審核假單失敗'
  } finally {
    isReviewLoading.value = false
  }
}
//#region  1. 使用者進入「管理員請假」-> 「想看到可由我審核的假單」
// => api.get(`/api/AdminLeaves/pending-review`)
//
// 2. 想清楚畫面需要記住哪三件事
// 查到哪些假單？ -> pendingReviewRecords=[] -> 初始空陣列
//
// 還在等待嗎？ -> isReviewLoading = false -> 初始 現在沒正在等待
// => 「按下查詢後、API 還沒回來時，畫面要不要顯示載入中或停用按鈕？」如果要，就需要一個狀態記住這段等待時間。
//
// 查詢出了什麼錯？ -> reviewError = '' -> 初始空的
//
// 3. 先寫查詢開始與結束
// async function loadPendingReviewLeaves(){....}
// 開始時標記等待、清除上次錯誤；結束時無論成功或失敗，都取消等待狀態。
//
// 4. 補上成功與失敗各要做什麼
// 成功：把回傳假單存起來，供畫面使用。
// const response = await api.get(`....`)
// pendingReviewRecords.value = response.data.records ?? []
//
// 失敗：把原因存起來，供畫面顯示。
// reviewError.value = error.response?.data?.message ?? '取得待審核假單失敗'
//
//  5. 最後決定何時執行
// 需求是進入頁面就載入，因此放進：
// onMounted(() => {
// loadPendingReviewLeaves()
// })
// #endregion

// 假單的「核准、駁回」功能設計成共用同一個函式
// 1. 哪張假單？
// 2. 審核結果：已核准、駁回
// 3. 駁回原因
//
// 畫面：
// 1. 點第 7 號的「核准」-> 按鈕顯示「處理中...」，其他列不顯示這個文字 -> 記住第7號這列id
// 2. button: {{reviewingId === record.id ? '處理中...' : '核准'}}

const reviewingId = ref(null) //預設沒有正在審核的假單

// click "核准"/"駁回" button -> 審核假單API
//「審核待審核假單時，從前端送來,送給後端：
// record(Get「可由我審核的假單」API -> 因為是待審核假單所以本來就不需回傳駁回原因),approvalStatus,rejectionReason
async function reviewLeave(record, approvalStatus, rejectionReason) {
  if (reviewingId.value !== null) return //如果有正在審核的假單 -> return => 不接受重複操作

  reviewingId.value = record.id // "現在這筆假單"存入"正在審核假單"
  reviewError.value = '' //清除上一次審核錯誤

  try {
    // await api.put(a, b) :把資料 b 送到後端網址 a，並等待回應。
    // 審核假單API -> 不能審核自己的、只能審核"待審核"的、駁回時須田駁回原因
    //
    // 送出哪裡
    await api.put(`/api/AdminLeaves/${record.id}/review`, {
      // 一起送出的內容
      approvalStatus, //審核狀態
      rejectionReason, //駁回原因

      // 假設：
      //
      // 前端：
      // record.id = 7
      // approvalStatus = 3 => 「審核者選擇駁回」
      // rejectionReason = '請補充說明'  ＝> 「審核者另外輸入」
      // |
      // v
      //
      // request:
      // {
      //   "approvalStatus": 3,
      //   "rejectionReason": "請補充說明"
      // }
      //
      // |
      // 把審核結果送給API
      // v
      //
      // PUT /api/AdminLeaves/7/review
    })

    // API成功後,駁回才關閉駁回原因視窗
    if (approvalStatus === 3) {
      rejectionDialog.value.close()
    }

    await loadPendingReviewLeaves() // 成功後更新待審核列表
  } catch (error) {
    reviewError.value = error.response?.data?.message ?? '審核假單失敗'
  } finally {
    reviewingId.value = null
  }
}

// reviewingId -> 記住哪張正在送出，防止重複操作
// rejectingRecord -> 記住等一下要送出哪張假單

// click "駁回" -> 打開駁回原因彈窗
const rejectionDialog = ref(null) //視窗元素，供 .showModal() 開啟
const rejectingRecord = ref(null) //這次準備駁回哪張假單
const rejectionReason = ref('') //使用者在輸入框填寫的原因
function openRejectionDialog(record) {
  //如果有正在審核的假單 -> return => 不接受重複操作
  if (reviewingId.value !== null) return

  rejectingRecord.value = record //記住點到的假單
  rejectionReason.value = '' //清除駁回原因
  reviewError.value = '' //清除上一次的錯誤

  rejectionDialog.value.showModal() //打開駁回原因視窗 ,真正提供 showModal() 的是 <dialog> 元素

  // 關鍵是變數後來有沒有連到 <dialog>：
  // const rejectionDialog = ref(null)
  //
  // <dialog ref="rejectionDialog">駁回原因表單</dialog>
  // 掛載前：rejectionDialog.value = null
  // 掛載後：rejectionDialog.value = 那個 <dialog> 元素
}

// 駁回原因視窗：click "確認駁回"
async function submitRejection() {
  // 如果沒有準備駁回某筆假單 -> return => 沒資料不能往下"駁回"
  if (rejectingRecord.value === null) return

  //如果有正在審核的假單 -> return => 不接受重複操作
  if (reviewingId.value !== null) return

  const reason = rejectionReason.value.trim()

  if (reason === '') {
    reviewError.value = '駁回時請填寫原因'
    return
  }

  // 審核假單功能
  await reviewLeave(rejectingRecord.value, 3, reason)
  // await api.put(`/api/AdminLeaves/${record.id}/review`, {
  //   approvalStatus,
  //   rejectionReason,
  // })
  //
  // ｜
  // Ｖ
  // await api.put(`/api/AdminLeaves/${rejectingRecord}/review`,{
  //   3,
  // reason
  // })
  //
  // ｜
  // Ｖ
  // 例如：選中第 7 號、原因是「請補充說明」
  // await api.put(`/api/AdminLeaves/7/review`,{
  //   3,
  // "請補充說明"
  // })
  //
  //
  // API成功後,駁回才關閉駁回原因視窗
  // if (approvalStatus === 3) {
  //   rejectionDialog.value.close()
  // }
  //
  // await loadPendingReviewLeaves() // 成功後更新待審核列表
}

// 顯示班別
function getClassTypeText(classType) {
  if (classType === 1) return '09:00~18:00'
  if (classType === 2) return '10:00~19:00'
  return '未知班別'
}
</script>

<!-- 畫面 -->
<template>
  <div class="admin-clock-page admin-leave-page">
    <a class="dashboard-skip-link" href="#leave-main">跳到主要內容</a>
    <aside class="dashboard-sidebar">
      <div class="dashboard-avatar" aria-hidden="true"><img :src="dashboardProfile" alt="" /></div>
      <nav class="dashboard-nav" aria-label="主要導覽">
        <RouterLink class="dashboard-nav-link" to="/admin/clock"
          ><img :src="navClockIcon" alt="" />打卡功能</RouterLink
        >
        <RouterLink class="dashboard-nav-link" to="/member"
          ><img :src="navUserIcon" alt="" />使用者中心</RouterLink
        >
        <RouterLink class="dashboard-nav-link is-section-active" to="/admin/leave"
          ><img :src="navLeaveIcon" alt="" />請假功能</RouterLink
        >
        <RouterLink v-if="currentUser.role === 'Admin'" class="dashboard-nav-link" to="/admin/users"
          ><img :src="navAdminIcon" alt="" />帳號管理</RouterLink
        >
      </nav>
      <div class="dashboard-logout-wrap drip-button-wrap">
        <span class="drip-button-drips" aria-hidden="true"></span>
        <button class="dashboard-logout drip-button" type="button" @click="logout">登出</button>
      </div>
    </aside>
    <main id="leave-main" class="dashboard-paper" tabindex="-1">
      <img class="dashboard-paper-texture" :src="dashboardPaper" alt="" aria-hidden="true" />
      <div class="dashboard-content leave-page-content">
        <h1>管理員請假紀錄</h1>
        <!--「SignalR 即時連線的錯誤提示」 -->
        <p v-if="realtimeError" class="error" role="alert">{{ realtimeError }}</p>

        <!-- 行事曆彈窗 -->
        <!-- ref="calendarDialog" 把這個 <dialog> 接到上面的變數 -->
        <dialog ref="calendarDialog" class="calendar-dialog">
          <h2>2027年1月</h2>

          <p v-if="calendarMessage">{{ calendarMessage }}</p>

          <div v-else class="calendar-grid">
            <span v-for="weekday in ['日', '一', '二', '三', '四', '五', '六']" :key="weekday">
              {{ weekday }}</span
            >
            <!-- 1 月 1 日是星期五，日到四先留五格 -->
            <!-- aria-hidden="true" 是告訴螢幕閱讀器「不用讀這個元素」。 它不會讓元素從畫面上消失 -->
            <span v-for="blank in 5" :key="blank" aria-hidden="true"></span>

            <!-- 每一天都取出來 -->
            <!-- 以20270101為例, day.date="20270101" 
        , 取最後兩個字元 .slice(-2)="01" 
        Number("01")= 1  => 只顯示日期-->
            <div v-for="day in calendarDays" :key="day.date" class="calendar-cell">
              <!-- :class 決定要不要套樣式  -->
              <!-- 'calendar-holiday' 自訂CSS類別 , day.isHoliday 是條件-->
              <span :class="{ 'calendar-holiday': day.isHoliday }">{{
                Number(day.date.slice(-2))
              }}</span>

              <!-- 國定假日名稱 -->
              <div v-if="day.isHoliday && day.description" class="calendar-description">
                {{ day.description }}
              </div>

              <!-- 「開國紀念日」用內層 <div>，只是為了讓它排在 1 的下一行，不是因為假日名稱一定要用 <div>。 -->
              <!-- <div> 預設會另起一行，所以顯示成：
          1
          開國紀念日 -->
              <!-- 可以用 <p>。 它預設也會另起一行，所以「開國紀念日」一樣會排在 1 下面，而且仍是同一個日曆格。 -->
              <!-- 差別是：<p> 表示一段文字，預設上下有留白；`<div>` 是一般容器，預設沒有那個段落留白。 -->
            </div>
          </div>

          <button type="button" @click="calendarDialog.close()">關閉</button>
        </dialog>

        <!-- ----xxx年各假別請假時數---- -->
        <section class="leave-summary-section">
          <h2>{{ currentYear }}年各假別請假時數</h2>

          <p v-if="isLoading">總計載入中...</p>
          <p v-else-if="errorMessage" class="error" role="alert">暫時無法取得年度統計</p>
          <div v-else class="leave-summary-row">
            <button class="leave-calendar-button" type="button" @click="openCalendarDialog">
              行事曆
            </button>
            <div class="leave-summary">
              <span v-for="summary in annualLeaveSummary" :key="summary.value">
                <template v-if="summary.hours === null">缺少日曆</template>
                <template v-else
                  >{{ summary.label }}：{{ summary.hours }}小時 {{ summary.minutes }}分鐘</template
                >
              </span>
            </div>
          </div>
        </section>
        <!-- ----自己的請假紀錄---- -->
        <section class="leave-records-section">
          <div class="leave-records-toolbar">
            <h2>個人請假紀錄</h2>
            <p v-if="isLoading">載入中...</p>
            <p v-else-if="errorMessage" class="error" role="alert">{{ errorMessage }}</p>
            <p v-else>共{{ records.length }}筆請假紀錄</p>
            <button
              class="leave-apply-button"
              type="button"
              :disabled="isSubmitting"
              @click="openLeaveDialog"
            >
              申請請假
            </button>
          </div>

          <!-- <button
        type="button"
        :disabled="isLoading || isSubmitting || deletingId !== null"
        @click="loadMyLeaves"
      >
        {{ isLoading ? '更新中...' : '更新請假紀錄' }}
      </button> -->
          <p v-if="deleteError" class="error" role="alert">{{ deleteError }}</p>

          <!-- ----表單---- -->
          <div class="table-container">
            <!-- form 是填資料的表單；table 是顯示多筆資料的表格。 -->
            <table>
              <thead>
                <tr>
                  <th>假單編號</th>
                  <th>假別</th>
                  <th>班別</th>
                  <th>請假日期</th>
                  <th>開始時間</th>
                  <th>結束時間</th>
                  <th>時數</th>
                  <th>審核狀態</th>
                  <th>駁回原因</th>
                  <th>操作</th>
                </tr>
              </thead>

              <tbody>
                <tr v-if="isLoading">
                  <!-- colspan="6"：這一列中的一格橫跨 6 欄，顯示「請假紀錄尚未載入」。 -->
                  <td colspan="10">載入中...</td>
                </tr>

                <tr v-else-if="errorMessage">
                  <td colspan="10">無法取得請假紀錄</td>
                </tr>

                <tr v-else-if="records.length === 0">
                  <td colspan="10">目前沒有請假紀錄</td>

                  <!-- .vue 最外層的 <template> => 放整個元件的畫面 HTML -->
                  <!-- 畫面裡的 <template v-if／v-else> => <template v-if／v-else>	告訴 Vue：「前面的 v-if 不成立時，一起顯示這組內容」。 -->
                </tr>
                <!-- 例如目前這筆假單：
            record = { id: 7,leaveType: 1,}
            getLeaveTypeText(1) - value=1 -> return '病假' -->
                <template v-else>
                  <tr v-for="record in records" :key="record.id">
                    <td>{{ record.id }}</td>

                    <td>{{ getLeaveTypeText(record.leaveType) }}</td>

                    <td>{{ getClassTypeText(record.classType) }}</td>

                    <!-- 後端把日期與時間放在同一個字串裡，
                這裡用 slice() 分別取出「日期」與「時分」 -->
                    <!-- 例如：
                record.startTime = '2026-10-22T09:30:00'
                record.endTime = '2026-10-22T18:00:00'
                slice(開始位置, 結束位置) 從 0 開始數，而且不包含結束位置。 -->

                    <!-- record.startTime、record.endTime 不需要再用 const 定義，因為它們是後端回傳的假單屬性。 -->

                    <!-- 日期 -->
                    <td>{{ record.startTime.slice(0, 10) }} ~ {{ record.endTime.slice(0, 10) }}</td>

                    <!-- 時間 -->
                    <td>{{ record.startTime.slice(11, 16) }}</td>
                    <td>{{ record.endTime.slice(11, 16) }}</td>
                    <td>
                      {{ formatLeaveDuration(record.startTime, record.endTime, record.classType) }}
                    </td>
                    <td>{{ getApprovalStatusText(record.approvalStatus) }}</td>
                    <td>{{ record.rejectionReason ?? '-' }}</td>
                    <td>
                      <!-- 假單的審核狀態= 1 -> 待審核 -->
                      <template v-if="record.approvalStatus === 1">
                        <!-- 刪除某筆假單 || 是否正在送出 => 停用按鈕 -->
                        <!-- "修改" -> openEditDialog(record) -->
                        <button
                          type="button"
                          :disabled="deletingId !== null || isSubmitting"
                          @click="openEditDialog(record)"
                        >
                          修改
                        </button>
                        <!-- deletingId === record.id 判斷「這一列是不是正在刪除的那筆」 -->
                        <!-- 停用條件：正在刪除任何一筆，或正在送出申請 → 停用。 -->
                        <!-- 顯示：
                  deletingId === record.id -> true -> 正在刪除 -> "刪除中"
                  deletingId === record.id -> false -> 其他 -> "刪除" -->
                        <button
                          v-if="record.approvalStatus === 1"
                          type="button"
                          :disabled="deletingId !== null || isSubmitting"
                          @click="requestDeleteLeave(record)"
                        >
                          {{ deletingId === record.id ? '刪除中...' : '刪除' }}
                        </button>
                      </template>
                      <span v-else>-</span>
                    </td>
                  </tr>
                </template>
              </tbody>
            </table>
          </div>
        </section>

        <!-- ----可由我審核的假單---- -->
        <section class="leave-review-section">
          <!-- 1. 區塊標題 -->
          <h2>可由我審核的假單</h2>

          <!-- 兩個 <p>：各自一段
畫面：  
        目前沒有待審核假單。

        請稍後重新查詢。  -->

          <!-- 兩個 <span>：接在同一行
畫面：
        病假：8 小時 事假：4 小時 
=> span 不主動換行，但畫面寬度不夠時，文字仍可能自然折行。-->

          <!-- 2. 使用者會遇到哪幾種畫面 -->
          <!-- 正在查詢 -> 載入中 -->
          <p v-if="isReviewLoading">載入中...</p>
          <!-- 查詢失敗 -> 錯誤原因 -->
          <p v-else-if="reviewError" class="error" role="alert">{{ reviewError }}</p>
          <!-- 查完沒有資料 -> 沒有待審核假單 -->
          <!-- 在 JavaScript：== 會先嘗試轉換型別；=== 不轉換，型別與值都要相同。 -->
          <p v-else-if="pendingReviewRecords.length === 0">目前沒有待審核假單</p>
          <!-- 查完有資料 -> 顯示table -->
          <!-- 3. 審核前需要知道哪些資訊 :
誰申請？ → 申請人
請什麼假？ → 假別
什麼時候？ → 開始、結束
請多久？ → 時數
為什麼請？ → 原因 -->
          <div v-else>
            <table>
              <thead>
                <tr>
                  <th>假單編號</th>
                  <th>申請人</th>
                  <th>假別</th>
                  <th>班別</th>
                  <th>開始時間</th>
                  <th>結束時間</th>
                  <th>時數</th>
                  <th>原因</th>
                  <th>操作</th>
                </tr>
              </thead>

              <!-- slice() 用來「取一部分」； replace() 用來「替換內容」。 -->
              <!-- 例如後端回傳：record.startTime = '2026-10-29T09:30:00' 
          1.slice(0, 16) => 取前 16 個字元(索引 0～15) = 2026-10-29T09:30 2.replace('T', ' ') => T 換成空白 = 2026-10-29 09:30  -->

              <!-- 多筆假單 => v-for  -->
              <!-- :key 用假單編號識別每列。其他欄位照同樣方式補上。 -->

              <!-- 例如 ：{ value: 1, label: '病假' }
            <option :key="type.value" :value="type.value">
            {{ type.label }}
            </option> 

:key="1" -> 告訴 Vue：這個選項的識別編號是 1
:value="1" -> 告訴選單：選到這個選項，取得的值是 1
病假 -> 使用者看到的文字
-->
              <tbody>
                <tr v-for="record in pendingReviewRecords" :key="record.id">
                  <td>{{ record.id }}</td>
                  <td>{{ record.adminAccount }}</td>
                  <td>{{ getLeaveTypeText(record.leaveType) }}</td>
                  <td>{{ getClassTypeText(record.classType) }}</td>
                  <td>{{ record.startTime.slice(0, 16).replace('T', ' ') }}</td>
                  <td>{{ record.endTime.slice(0, 16).replace('T', ' ') }}</td>
                  <td>
                    {{ formatLeaveDuration(record.startTime, record.endTime, record.classType) }}
                  </td>
                  <td>{{ record.reason }}</td>
                  <td>
                    <button
                      type="button"
                      :disabled="reviewingId !== null"
                      @click="reviewLeave(record, 2)"
                    >
                      {{ reviewingId === record.id ? '處理中...' : '核准' }}
                    </button>

                    <!-- 只負責開啟駁回視窗 -> 還沒有送出 -->
                    <!-- 視窗按「確認駁回」→ 送出 API，這時才進入處理中 -->
                    <button
                      type="button"
                      :disabled="reviewingId !== null"
                      @click="openRejectionDialog(record)"
                    >
                      駁回
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <!-- ----請假彈窗---- -->
        <!-- <dialog>是對話框元素，呼叫 .showModal() 後就會以彈窗方式開啟。 -->
        <!-- leaveDialog.showModal() => 開啟彈窗 -->
        <!-- ref="leaveDialog" -> 對應const leaveDialog = ref(null) -->
        <dialog ref="leaveDialog" class="leave-dialog">
          <h2>{{ editingId === null ? '申請請假' : '修改請假申請' }}</h2>

          <!-- 選取 <form> 表單，裡面包含標籤、輸入框與按鈕區 -->
          <!-- role="alert" 是讓螢幕閱讀器知道：「這是需要立即通知使用者的重要訊息。」 -->
          <form @submit.prevent="submitLeave">
            <p v-if="submitError" class="error" role="alert">{{ submitError }}</p>

            <label for="leave-type">假別</label>
            <!-- 
        不加 .number：
        value="1" => 病假 
        => 選「病假」後：leaveForm.value.leaveType // '1'，字串
        加 .number：
        value="1" => 病假 
        => 選「病假」後：leaveForm.value.leaveType // 1，數字 -->
            <!-- .number 將選項值轉成數字，符合後端的 int LeaveType -->
            <select id="leave-type" v-model.number="leaveForm.leaveType" required>
              <option value="">請選擇假別</option>
              <option v-for="type in leaveTypes" :key="type.value" :value="type.value">
                {{ type.label }}
              </option>
            </select>

            <!-- ----選擇：上班班別---- -->
            <label for="leave-class-type">班別</label>
            <select id="leave-class-type" v-model.number="leaveForm.classType" required>
              <option value="">請選擇班別</option>
              <option :value="1">09:00～18:00</option>
              <option :value="2">10:00～19:00</option>
            </select>

            <!-- v-model 會把輸入值同步到對應屬性。例如選病假後，leaveForm.leaveType 變成 1 -->
            <!-- 例如：
        前端的 leaveForm.reason 之後會送到後端，再存成 leave.Reason  -->
            <label for="leave-start">開始日期時間</label>
            <input
              id="leave-start"
              v-model="leaveForm.startTime"
              type="datetime-local"
              lang="zh-TW"
              step="60"
              required
              @change="checkLeaveStarDate"
            />

            <label for="leave-end">結束日期時間</label>
            <input
              id="leave-end"
              v-model="leaveForm.endTime"
              type="datetime-local"
              lang="zh-TW"
              step="60"
              required
            />

            <label for="leave-reason">請假原因</label>
            <textarea
              id="leave-reason"
              v-model="leaveForm.reason"
              maxlength="100"
              required
            ></textarea>

            <!-- leaveDialog.close() => 關閉視窗 -->
            <div class="dialog-actions">
              <button type="button" @click="leaveDialog.close()">取消</button>
              <button type="submit" :disabled="isSubmitting">
                {{ isSubmitting ? '處理中...' : editingId === null ? '送出申請' : '儲存修改' }}
              </button>
              <!-- 等同於：
          if (isSubmitting) {
            return '處理中...'
          } else if (editingId === null) {
            return '送出申請'
          } else {
            return '儲存修改'
          } -->
            </div>
          </form>
        </dialog>

        <!-- ----駁回原因彈窗---- -->
        <!-- ref="leaveDialog" -> 對應const rejectionDialog = ref(null) -->
        <!-- ？. 是「可選串連」：先確認 rejectingRecord 有資料，再讀取它的 id
      => 因為初始值 const rejectingRecord = ref(null)-->

        <!-- @cancel => 阻止使用者按 Esc 關閉視窗。 => 判斷是否有假單正在審核 && 阻止關閉  -->
        <!-- 例如： reviewingId=7 -> 7!==null =true  && 阻止關閉 => 「阻止關閉」-->
        <!-- 例如： reviewingId=null -> null!==null =false && 阻止關閉 => 阻止關閉不執行，瀏覽器正常關閉視窗 -->
        <!-- 對 <dialog> 的 cancel 事件而言，預設行為是關閉視窗。所以呼叫後，這次按 Esc 就不會關閉。 -->
        <dialog
          ref="rejectionDialog"
          class="leave-dialog leave-dialog--rejection"
          @cancel="reviewingId !== null && $event.preventDefault()"
        >
          <!-- id 是假單編號，不是使用者編號 -->
          <h2>駁回假單 #{{ rejectingRecord?.id }}</h2>

          <!-- 表單 -->
          <!-- @submit 送出事件, prevent 阻止瀏覽器預設送出 => 改執行submitRejection函式-->
          <form @submit.prevent="submitRejection">
            <p v-if="reviewError" class="error" role="alert">{{ reviewError }}</p>
            <label for="rejection-reason">駁回原因</label>

            <!-- v-model="rejectionReason" 
      把輸入框與 rejectionReason 連起來，讓程式記住你輸入的駁回原因。 -->
            <!-- :disabled = 有假單正在送出審核時，停用這個按鈕或輸入框-->
            <textarea
              id="rejection-reason"
              v-model="rejectionReason"
              maxlength="500"
              placeholder="必須填入原因"
              :disabled="reviewingId !== null"
              required
            ></textarea>

            <div class="dialog-actions">
              <!-- :disabled = 有假單正在送出審核時，停用這個按鈕或輸入框 -->
              <button
                type="button"
                :disabled="reviewingId !== null"
                @click="rejectionDialog.close()"
              >
                取消
              </button>

              <!-- submit：觸發外層 <form> => 執行submitRejection函式 -->
              <button type="submit" :disabled="reviewingId !== null">
                {{ reviewingId !== null ? '處理中...' : '確認駁回' }}
              </button>
            </div>
          </form>
        </dialog>
        <dialog
          ref="deleteDialog"
          class="leave-dialog leave-dialog--confirm"
          @cancel.prevent="cancelDeleteLeave"
        >
          <h2>確定刪除這筆待審核假單？</h2>
          <button
            class="leave-dialog-close"
            type="button"
            aria-label="關閉"
            @click="cancelDeleteLeave"
          >
            ×
          </button>
          <div class="leave-confirm-actions">
            <button type="button" :disabled="deletingId !== null" @click="confirmDeleteLeave">
              {{ deletingId !== null ? '刪除中...' : '確定' }}
            </button>
            <span>or</span>
            <button type="button" :disabled="deletingId !== null" @click="cancelDeleteLeave">
              取消
            </button>
          </div>
        </dialog>
      </div>
    </main>
  </div>
</template>

