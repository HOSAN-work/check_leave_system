<!-- App.vue 是整個 Vue 網站的「最外層根元件」
負責放整個網站共同存在的結構 -->

<script setup>
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'
// useRoute => 讀取目前網址資料
// useRouter => 控制頁面跳轉
// import HelloWorld from './components/HelloWorld.vue'

// 從 Vue 套件匯入 onMounted 生命週期函式
// 等這個 Vue 元件已經建立並顯示到頁面後，再檢查使用者是否已登入
// 決定：什麼時候執行 initializeAuth()
//
//
import { computed, onMounted, watch } from 'vue'

import { useAuth } from './composables/useAuth.js'

const route = useRoute()
const hidesGlobalHeader = computed(() =>
  ['login', 'member', 'admin-clock', 'admin-clock-records', 'admin-users', 'admin-leave'].includes(
    route.name,
  ),
)
const router = useRouter()

const { currentUser, isLoggedIn, authInitialized, initializeAuth, logout } = useAuth()
//這叫做 Object Destructuring，物件解構
// 使用解構後,就能直接寫：
// currentUser
// isLoggedIn
// authInitialized
// initializeAuth()
// logout()

// 為什麼沒有取出 login？
// useAuth() 有回傳：login
// 但 App.vue 不一定需要執行登入

// 當這個 Vue 元件第一次顯示到畫面上後，自動執行 initializeAuth()
// 如果放在 App.vue，通常很適合，因為 App.vue 是整個網站的根元件，網站啟動時就能檢查一次登入狀態
// App.vue 出現在畫面後，執行 initializeAuth()
onMounted(() => {
  initializeAuth()
})

// 例如 LoginView.vue：
// <script setup>
// import { onMounted } from 'vue'

// onMounted(() => {
//   console.log('登入頁出現在畫面了')
// })
// 當瀏覽器顯示 LoginView.vue 的內容時，就稱為：
// LoginView 元件已經掛載（mounted）

// 監看登入狀態
watch(isLoggedIn, (newIsLoggedIn, oldIsLoggedIn) => {
  // 使用者原本是登入狀態，但現在變成未登入狀態了嗎？
  const loginJustBecameInvalid = oldIsLoggedIn === true && newIsLoggedIn === false

  // 為什麼需要 oldIsLoggedIn？
  // 假設：oldIsLoggedIn = true && newIsLoggedIn = false
  // 表示原本確實登入，後來因為 Cookie 失效或登出而變成未登入。
  //
  // 如果網站剛開啟，本來就沒有登入：
  // oldIsLoggedIn = false && newIsLoggedIn = false
  // 這不算「登入突然失效」，不需要這個監看器處理
  //

  // 使用者現在所在的 Vue 頁面，是不是需要登入才能進入？
  // route 是目前頁面
  // meta 是路由的附加設定
  // requiresAuth: true 意思是：/profile 是需要登入的頁面
  const currentPageRequiresAuth = route.meta.requiresAuth === true

  // 如果使用者原本已登入、現在登入失效 = true
  // &&
  // 目前頁面需要登入 = true
  if (loginJustBecameInvalid && currentPageRequiresAuth) {
    // router.replace()：負責用login頁取代目前的受保護頁面
    // 導向：路由名為login的頁面
    // query 會在login網址加入query string
    // 也就是？後面 =>  redirect=/profile
    //
    // route.fullPath 代表目前頁面的完整路由
    // 例如目前網址：/orders?page=2#history
    //
    // 導向登入頁後：/login?redirect=/orders?page=2#history

    router.replace({
      name: 'login',
      query: {
        redirect: route.fullPath,
      },
    })
  }
})
</script>

<template>
  <!-- /login → 不显示 Vue Logo、HelloWorld、导航列
其他页面 → 正常显示 Header -->
  <header v-if="!hidesGlobalHeader">
    <img alt="Vue logo" class="logo" src="@/assets/logo.svg" width="125" height="125" />

    <div class="wrapper">
      <!--  nav 代表網站的導覽區域，例如首頁、登入、註冊和登出 
可以這麼多 template ?
  可以。因為這些 template 各自負責不同層級的判斷，而且它們不會真的產生 HTML 標籤。*/}
  第一層：登入狀態檢查完成了嗎？ 
  第二層：使用者有登入嗎？ -->

      <!--  authInitialized 是 false
        └─ 什麼都不顯示

authInitialized 是 true
├─ isLoggedIn 是 true
│   └─ 顯示「歡迎、暱稱、登出」
│
└─ isLoggedIn 是 false
    └─ 顯示「註冊、登入」 -->
      <nav>
        <RouterLink to="/">打卡功能</RouterLink>

        <template v-if="authInitialized">
          <!--  isLogged = true  -->
          <template v-if="isLoggedIn">
            <!--  span 不會預設換行  -->
            <span> 歡迎, {{ currentUser.nickname }} </span>

            <RouterLink to="/member"> 會員中心 </RouterLink>

            <RouterLink v-if="currentUser.role === 'Admin'" to="/admin/users">
              帳號管理
            </RouterLink>

            <RouterLink v-if="currentUser.role === 'Admin'" to="/admin/clock">
              管理員打卡
            </RouterLink>

            <RouterLink v-if="currentUser.role === 'Admin'" to="/admin/leave">
              管理員請假
            </RouterLink>

            <button type="button" @click="logout">登出</button>

            <!--  type="button"？
            type="button">一般按鈕
            type="submit">送出表單
            type="reset">清除表單

            type="button"
            意思是：這只是一顆普通按鈕，點擊後執行指定的 JavaScript
              -->

            <!--  
              畫面：
            歡迎，小明  [登出]
              -->
          </template>

          <!--  isLoggedIn = false -->
          <template v-else>
            <RouterLink to="/register"> 註冊 </RouterLink>

            <RouterLink to="/login"> 登入 </RouterLink>
          </template>
        </template>
      </nav>
    </div>
  </header>

  <RouterView />
</template>

<style scoped>
header {
  line-height: 1.5;
  max-height: 100vh;
}

.logo {
  display: block;
  margin: 0 auto 2rem;
}

nav {
  width: 100%;
  font-size: 12px;
  text-align: center;
  margin-top: 2rem;
}

nav a.router-link-exact-active {
  color: var(--color-text);
}

nav a.router-link-exact-active:hover {
  background-color: transparent;
}

nav a {
  display: inline-block;
  padding: 0 1rem;
  border-left: 1px solid var(--color-border);
}

nav a:first-of-type {
  border: 0;
}

@media (min-width: 1024px) {
  header {
    display: flex;
    place-items: center;
    padding-right: calc(var(--section-gap) / 2);
  }

  .logo {
    margin: 0 2rem 0 0;
  }

  header .wrapper {
    display: flex;
    place-items: flex-start;
    flex-wrap: wrap;
  }

  nav {
    text-align: left;
    margin-left: -1rem;
    font-size: 1rem;

    padding: 1rem 0;
    margin-top: 1rem;
  }
}
</style>
