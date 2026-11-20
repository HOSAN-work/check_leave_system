import { createRouter, createWebHistory } from 'vue-router'
// import HomeView from '../views/HomeView.vue'
// 實作「路由守衛」：未登入不能進入 /member，會自動跳到登入頁
import {useAuth} from '@/composables/useAuth.js'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
{
  path: '/',
  name: 'home',
  redirect: { name: 'admin-clock' },
},
    // {
    //   path: '/about',
    //   name: 'about',
    //   // route level code-splitting
    //   // this generates a separate chunk (About.[hash].js) for this route
    //   // which is lazy-loaded when the route is visited.
    //   component: () => import('../views/AboutView.vue'),
    // },
    // 使用者輸入哪個網址 → Vue 應該顯示哪個頁面
    // http://localhost:5173/xxx
    
    // {
    //   網址: '/xxx',
    //   路由名稱: 'xxx',
    //   要顯示的頁面: 'xxxView.vue',
    // }
    
    // name: 'register'
    // 之後可以寫：
    // <RouterLink :to="{ name: 'register' }">
    //   註冊
    // </RouterLink>
    
    // component: () => import('....')
    // component 是在指定： 使用者進入這個網址時，要顯示哪個 Vue 頁面？
    {
      path: '/register',
      name: 'register',
      component:() => 
        import('../views/RegisterView.vue'),
    },
    {
      path: '/login',
      name: 'login',
      component:() => 
        import('../views/LoginView.vue'),
    },
    {
      path: '/member',
      name: 'member',
      component:() => 
        import('../views/MemberView.vue'),
      //  meta.requiresAuth「路由標記」
      // 進入這個頁面以前，使用者必須先登入
      meta:{
        // requiresAuth = 需要登入驗證
        requiresAuth:true
      }
    },
  
      // Vue Router 有兩種載入頁面元件的寫法，兩種都正確
      // 寫法一 :「直接載入」
      // 優點：
      // - 寫法容易理解
      // - 適合首頁或經常使用的主要頁面
      // 缺點：
      // - 網站剛啟動就要載入這個頁面
      // - 頁面很多時，初始 JavaScript 可能比較大
      // import ChangePasswordView from '@/views/ChangePasswordView.vue'
      // {
      // path:...,
      // name:...,
      // component: ChangePasswordView,
      // }
      // 
      // 寫法二：「延遲載入」
      // 優點：
      // - 減少網站第一次載入的程式量
      // - 適合不是每位使用者都會進入的頁面
      // 缺點：
      // - 第一次進入該頁時才需要載入檔案
      // - 寫法對初學者稍微抽象
      // {
      // path:...,
      // name:...,
      // component: () =>
      // import('@/views/ChangePasswordView.vue'),
      // }
      // 
      //
      // 開首頁
      // → 不載入修改密碼頁
      // 進入 /member/password
      // → 才載入修改密碼頁
      //
      // =>這不是硬性規定，只是一種效能安排
    {
      path: '/admin/users',
      name: 'admin-users',
      component:() => 
        import('../views/AdminUsersView.vue'),
      meta:{
        title: '帳號管理 - 打卡暨請假系統',
        // requiresAuth = 需要登入驗證
        requiresAuth:true,
        requiresAdmin:true 
        // requiresAdmin: true 本身「不會做任何事情」,它只是在這個 Route 上貼了一張標籤
        //自定義 ,代表必須是管理員
      }
    },
    {
      path: '/admin/clock',
      name: 'admin-clock',
      component:() => 
        import('../views/AdminClockView.vue'),
      meta:{
        title: '打卡儀表板 - 打卡暨請假系統',
        // requiresAuth = 需要登入驗證
        requiresAuth:true,
        requiresAdmin:true 
        // requiresAdmin: true 本身「不會做任何事情」,它只是在這個 Route 上貼了一張標籤
        //自定義 ,代表必須是管理員
      }
    },
    {
      path: '/admin/clock/records',
      name: 'admin-clock-records',
      component:() =>
        import('../views/AdminClockView.vue'),
      meta:{
        title: '所有管理員打卡紀錄 - 打卡暨請假系統',
        requiresAuth:true,
        requiresAdmin:true
      }
    },
    {
      path: '/admin/leave',
      name: 'admin-leave',
      component:() => 
        import('../views/AdminLeaveView.vue'),
      meta:{
        title: '管理員請假紀錄 - 打卡暨請假系統',
        // requiresAuth = 需要登入驗證
        requiresAuth:true,
        requiresAdmin:true 
        // requiresAdmin: true 本身「不會做任何事情」,它只是在這個 Route 上貼了一張標籤
        //自定義 ,代表必須是管理員
      }
    },
],
})

// | 準備進入的頁面 | `to.meta.requiresAuth` |
// |---           |---|
// | `/`          | `undefined` |
// | `/login`     | `undefined` |
// | `/member`    | `true` |



// 每次準備切換頁面以前，先檢查目標頁面是否需要登入；如果需要但使用者沒有登入，就送去登入頁。
// router      → Vue Router
// beforeEach  → 每一次路由切換之前
router.beforeEach(async (to) =>{
  const{
    //取得目前會員
    currentUser,
    // 現在是否已登入
    isLoggedIn,
    // 是否已經向後端確認完登入狀態
    authInitialized,
    // 向後端確認 Cookie 的函式
    initializeAuth
  } = useAuth()

  // 只有「需要登入」的頁面才必須等後端確認 Cookie。
  // /login 和 /register 是公開頁面；若這裡也 await，後端暫時沒有回應時，
  // 使用者點「註冊」就會看起來像連結完全沒反應。
  if (to.meta.requiresAuth && !authInitialized.value) {
    await initializeAuth()
  }

// 如果使用者已經登入，但試圖訪問登入或註冊頁面，則重定向到相應的首頁
  if ((to.name === 'login' || to.name === 'register') && isLoggedIn.value) {
  return { name: currentUser.value?.role === 'Admin' ? 'admin-clock' : 'member' }
}

  
  // 目標頁面需要登入，但目前沒有登入 => true && !false => 才會進入迴圈
  //to.meta.requiresAuth 讀取目標頁面的「需要登入」標記 => '/member/ => true
  // to.fullPath 取得完整目標網址
  if(to.meta.requiresAuth && !isLoggedIn.value){
    return{
      // 前往路由名稱為 login 的頁面
      name:'login',

// 使用者原本想進入 /member
// 但是因為尚未登入
// 所以先被送到 /login
// 同時用 redirect 記住原本想去 /member


      // query 是網址中 ? 後面的參數
      // 網址：/login?redirect=/member
      // redirect = /member
      //
      // {
      //   name: 'login',
      //   query: {
      //     redirect: '/member',
      //   }
      // }
      //
      //假設使用者原本要進入：/member
      //to.fullPath === '/member'

      // 登入成功後要回去的網址
      query:{
        redirect:to.fullPath
      }
      // redirect:to.fullPath 他怎麼知道 to.fullPath 是指/member ?
      // 因為使用者「正在準備前往 /member」時，
      // Vue Router 會自動建立 to 物件，然後把它傳給 beforeEach()
      // Vue Router 就會建立類似這樣的物件：
      // const to = {
      //   path: '/member',
      //   fullPath: '/member',
      //   name: 'member',
      //   meta: {
      //     requiresAuth: true,
      //   },
      // }
    }
  }


  // 一定要先判斷「有沒有登入」，再判斷「是不是管理員」
  //
  // 看到 requiresAdmin 標籤之後，我要怎麼處理？
  //
  // to.meta.requiresAdmin = true 
  // && 
  // 'Admin' !== 'Admin' = false  => 不會進入迴圈
  // 'Member' !== 'Admin' = true  => 進入迴圈
  // 
  // 已登入，但不是管理員
  if(to.meta.requiresAdmin && currentUser.value?.role !== 'Admin'){
    // 不要進入原本的管理員頁，改去名稱為 member 的路由
    return{
      name:'member'
    }
  }
})

router.afterEach((to) => {
  document.title = to.meta.title ?? '打卡暨請假系統'
})

export default router
