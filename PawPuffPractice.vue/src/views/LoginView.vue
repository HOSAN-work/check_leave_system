<!-- 登入頁 -->
<script setup>
import { reactive, ref } from 'vue'

// 為什麼現在的 LoginView 需要？
// 一開始的 LoginView.vue 登入成功後只顯示資料：
// successMessage.value = '登入成功'
// currentUser.value = response.data.user
// 沒有切換頁面，所以不需要 useRouter()
// 現在我們改成登入成功後自動回首頁：
// await router.push('/')
import { useRouter, useRoute, RouterLink } from 'vue-router'

import '../assets/login.css'

import profileperson from '../assets/profile.png'
import loginbackground from '../assets/login-background.png'
import fallGuyOne from '../assets/fall guy .png'
import fallGuyTwo from '../assets/fall guy 2 .png'
import fallGuyThree from '../assets/fall guy 3 .png'
import loginUserIcon from '../assets/login-user.svg'
import loginEyeIcon from '../assets/login-eye.svg'
import loginInfoIcon from '../assets/login-info.svg'
import loginErrorIcon from '../assets/login-error.svg'
import oauthGoogleIcon from '../assets/oauth-google.svg'
import oauthAppleIcon from '../assets/oauth-apple.svg'

// useRouter() → 控制「要去哪裡」=> 控制頁面跳轉
// 取得 Router：const router = useRouter()
//
// router.push('/member') // 前往新頁面
// router.replace('/login') // 替換目前瀏覽紀錄
// router.back() // 回上一頁
// router.forward() // 前往下一頁
//
//
// useRoute() → 讀取「目前在哪裡、網址帶了什麼」=> 讀取目前網址資料
// 取得目前 Route：const route = useRoute()
//
//假設網址：/login?redirect=/profile
//可以讀取：
// route.path -> '/login'
//route.name -> 'login'
//route.query.redirect -> '/profile'

// 用途相近，但使用方式不同：
// RouterLink   → 使用者點擊連結後跳轉
// router.push  → 程式判斷完成後自動跳轉
// 登入成功後要自動跳轉，所以使用router.push()

// import api from '@/services/api'
// 取代為：
import { useAuth } from '@/composables/useAuth'
import '../assets/drip-button.css'

// 呼叫 useRouter()，取得 Router 物件並存到 router
const router = useRouter()

const route = useRoute()

const { login } = useAuth()

const form = reactive({
  account: '',
  password: '',
})

// 表示「登入表單現在是否正在送出」
// 初始:目前沒有送出登入請求
const isSubmitting = ref(false)

// 用來保存要顯示在畫面上的登入錯誤
const errorMessage = ref('')
const isCapsLockOn = ref(false)
const isPasswordVisible = ref(false)

// Google／Apple 登入還沒接上後端 OAuth，先只做外觀，點擊時顯示提示
const socialMessage = ref('')

function openSocialLogin(provider) {
  // 之後接上第三方登入時，這裡改成導向該平台的授權頁
  socialMessage.value = `${provider} 登入尚未開放，請先使用帳號密碼登入`
}

function updateCapsLock(event) {
  isCapsLockOn.value = event.getModifierState('CapsLock')
}

// const successMessage = ref('')
// const currentUser = ref(null)

// 使用者送出登入表單時要執行的函式
async function submitlogin() {
  // 清除舊錯誤
  errorMessage.value = ''

  // successMessage.value = ''
  // currentUser.value = null

  // 進入送出狀態
  isSubmitting.value = true

  try {
    // 登入並接收 HttpOnly Cookie
    // const loginResponse = await api.post(
    //     '/api/Auth/login',
    //     {
    //         account:form.account,
    //         password:form.password
    //     }
    // )

    //把使用者輸入的帳號和密碼傳給 login()
    await login(form.account, form.password)
    // 跳轉到原本想去的admin/clock，或者回首頁
    router.push(route.query.redirect || '/admin/clock')

    // 登入成功後回首頁
    // await router.push('/')

    //如果使用者是因為未登入而被送到登入頁，登入成功後就回到原本想去的頁面；否則回首頁
    // 例如使用者原本要去：/profile
    // 因為未登入，網址變成：/login?redirect=/profile
    // redirectPath，用來保存「登入成功後要前往的路徑」
    //
    // 取得 query
    // route.query.redirect
    // route 表示目前所在的路由
    // 如果目前網址是：/login?redirect=/profile
    // Vue Router 解析後：route.query
    // {
    //   redirect: '/profile',
    // }
    //
    // route.query.redirect 取得：'/profile'
    //
    // typeof 用來檢查資料型別, 為什麼要檢查？
    // 因為 route.query.redirect 不保證一定有正常的單一文字值
    // 它可能是：
    // undefined   // 網址沒有 redirect
    // null
    // '/profile'  // 正常字串
    //
    //
    // 如果 redirect 是字串
    //     使用 redirect
    // 否則
    //     使用首頁 /
    const redirectPath = typeof route.query.redirect === 'string' ? route.query.redirect : '/'

    // 所以登入成功後回（redirect或者首頁）
    await router.push(redirectPath)
  } catch (error) {
    // 給開發者看：完整錯誤顯示在瀏覽器 Console
    console.error('登入錯誤：', error)

    // 給使用者看：顯示在登入頁面
    errorMessage.value = error.response?.data?.message ?? '登入失敗,請檢查帳號與密碼'
  } finally {
    isSubmitting.value = false
  }
}

// Login() 最後執行：
// return Ok(new
// {
//     message = "登入成功",
//     user = new
//     {
//         user.Id,
//         user.Account,
//         user.Nickname
//     }
// });
//ASP.NET Core 會把它轉成 HTTP Response Body：
//         {
//   "message": "登入成功",
//   "user": {
//     "id": 1,
//     "account": "pawpuff01",
//     "nickname": "泡芙"
//   }
// }
//Axios 收到 HTTP Response 後，會建立一個 Response 物件：
// {
//   data: {
//     message: '登入成功',
//     user: {
//       id: 1,
//       account: 'pawpuff01',
//       nickname: '泡芙',
//     },
//   },

//   status: 200,
//   statusText: 'OK',
//   headers: {},
//   config: {},
// }
// successMessage.value =
// loginResponse.data.message ?? '登入成功'

// 使用 Cookie 取得目前會員
// 登入成功後，再問後端：「現在登入的是誰？」然後把後端回傳的會員資料存進 currentUser
// const meResponse = await api.get(
//     '/api/Auth/me',
// )
// 檢查 Cookie
// → 知道目前會員 Id
// → 去資料庫找會員
// → 回傳會員資料
//
//例如後端回傳：
// {
//   "user": {
//     "id": 1,
//     "account": "pawpuff01",
//     "nickname": "泡芙",
//     "email": "puff@example.com",
//     "pointBalance": 100
//   }
// }

// currentUser.value = meResponse.data.user
// }

//error 裡面有什麼？
//如果後端有回應，例如 401:
// error = {
//   message: 'Request failed with     status code 401',

//   response: {
//     status: 401,

//     data: {
//       message: '帳號或密碼錯誤',
//     },
//   },
// }
//
//error.response.status = 401
// error.response.data.message = 帳號或密碼錯誤

// catch(error)
// {
//     console.error('登入錯誤：',error);

//     errorMessage.value = error.response?.data?.message ?? '登入失敗,請檢查帳號與密碼'

// ?. 是什麼？
// error.response?.data?.message
//安全地往裡面找：
// error
// → 有沒有 response？
// → 有沒有 data？
// → 有沒有 message？
// 如果中途不存在，就得到：undefined
//
//情況一：有 response，沒有 data
// error = {
//   message: 'Request failed',
//   response: {
//     status: 500,
//   },
// }
// 有 response
// → 沒有 data
// → 停止尋找
// → 結果是 undefined
// => undefined ?? '登入失敗，請檢查帳號與密碼'
// => 登入失敗，請檢查帳號與密碼
//
//情況二：沒有 response
// error = {
//   message: 'Network Error',
// }
// 沒有 response
// → 直接停止尋找
// → 結果是 undefined
// => undefined ?? '登入失敗，請檢查帳號與密碼'
// => 登入失敗，請檢查帳號與密碼
//
//情況三：有 response、有 data、沒有 message
// error = {
//   response: {
//     status: 500,
//     data: {
//       title: 'Server Error',
//     },
//   },
// }
// 有 response
// → 有 data
// → 沒有 data.message
// → undefined
// => undefined ?? '登入失敗，請檢查帳號與密碼'
// => 登入失敗，請檢查帳號與密碼
//
// 情況四：有 response、有 data、有 message
// error = {
//   response: {
//     status: 401,
//     data: {
//       message: '帳號或密碼錯誤',
//     },
//   },
// }
// => 帳號或密碼錯誤 ?? '登入失敗，請檢查帳號與密碼'
// => 帳號或密碼錯誤

//     }
//     finally{
//     isSubmitting.value = false
//     }
// }
</script>

<template>
  <main class="login-page">
    <img class="login-background" :src="loginbackground" alt="" />

    <div class="login-decoration login-decoration--profile" aria-hidden="true">
      <img class="profile" :src="profileperson" alt="" />
    </div>
    <img
      class="login-decoration login-decoration--character login-decoration--one"
      :src="fallGuyOne"
      alt=""
    />
    <img
      class="login-decoration login-decoration--character login-decoration--two"
      :src="fallGuyTwo"
      alt=""
    />
    <img
      class="login-decoration login-decoration--character login-decoration--three"
      :src="fallGuyThree"
      alt=""
    />

    <section class="login-content" aria-labelledby="login-title">
      <h1 id="login-title" class="login-title">登入</h1>

      <form class="login-form" :aria-busy="isSubmitting" @submit.prevent="submitlogin">
        <div class="account-input">
          <label for="account">帳號<span aria-hidden="true">*</span></label>
          <div class="account-field">
            <img class="field-icon" :src="loginUserIcon" alt="" aria-hidden="true" />
            <input
              id="account"
              v-model.trim="form.account"
              type="text"
              autocomplete="username"
              placeholder="請輸入帳號"
              aria-describedby="account-hint"
              required
            />
          </div>
          <p id="account-hint" class="account-input-hint">必填，請輸入您的帳號</p>
        </div>

        <div class="password-input">
          <div class="password-label-row">
            <label for="password">密碼<span aria-hidden="true">*</span></label>
            <span v-if="isCapsLockOn" class="caps-lock-hint" aria-hidden="true">
              <img :src="loginInfoIcon" alt="" aria-hidden="true" />
              Caps Lock is on
            </span>
            <span class="visually-hidden" aria-live="polite" aria-atomic="true">
              {{ isCapsLockOn ? 'Caps Lock is on' : '' }}
            </span>
          </div>
          <div class="password-field" :class="{ 'input-error': errorMessage }">
            <input
              id="password"
              v-model="form.password"
              :type="isPasswordVisible ? 'text' : 'password'"
              autocomplete="current-password"
              :aria-describedby="errorMessage ? 'password-hint' : undefined"
              @keydown="updateCapsLock"
              @keyup="updateCapsLock"
              @blur="isCapsLockOn = false"
              required
            />
            <button
              class="password-toggle"
              type="button"
              aria-label="顯示密碼"
              :aria-pressed="isPasswordVisible"
              @click="isPasswordVisible = !isPasswordVisible"
            >
              <img :src="loginEyeIcon" alt="" aria-hidden="true" />
            </button>
          </div>
          <p id="password-hint" class="password-input-hint" role="alert" aria-atomic="true">
            <img v-if="errorMessage" :src="loginErrorIcon" alt="" aria-hidden="true" />
            {{ errorMessage }}
          </p>
        </div>

        <div class="login-button-wrap drip-button-wrap">
          <span class="drip-button-drips" aria-hidden="true"></span>
          <button class="login-button drip-button" type="submit" :disabled="isSubmitting">
            {{ isSubmitting ? '登入中...' : '登入' }}
          </button>
        </div>
      </form>

      <!-- 還沒有帳號？註冊：位置在登入按鈕下方、分隔線上方 -->
      <p class="login-register">
        還沒有帳號？<RouterLink to="/register">註冊</RouterLink>
      </p>

      <!-- Google／Apple 登入：目前沒有後端 OAuth，只做外觀，點擊顯示尚未開放 -->
      <div class="login-social">
        <p class="login-social-divider">或</p>
        <div class="login-social-buttons">
          <button
            class="login-social-button"
            type="button"
            aria-label="使用 Google 帳號登入"
            @click="openSocialLogin('Google')"
          >
            <img :src="oauthGoogleIcon" alt="" aria-hidden="true" />
          </button>
          <button
            class="login-social-button"
            type="button"
            aria-label="使用 Apple 帳號登入"
            @click="openSocialLogin('Apple')"
          >
            <img :src="oauthAppleIcon" alt="" aria-hidden="true" />
          </button>
        </div>
        <p class="login-social-message" role="status" aria-atomic="true">{{ socialMessage }}</p>
      </div>

      <p class="visually-hidden" role="status" aria-atomic="true">
        {{ isSubmitting ? '登入中' : '' }}
      </p>
    </section>
  </main>
</template>
