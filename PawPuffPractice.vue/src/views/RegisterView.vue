<!-- 完整的會員註冊頁面 -->

<!-- JavaScript 邏輯 -->
<script setup>
import { nextTick, reactive, ref } from 'vue'

// nextTick()      // 等 Vue 更新完畫面
// reactive()      // 建立響應式物件，例如表單
// ref()           // 建立響應式單一資料
// RouterLink      // 建立可以點擊的頁面連結
// useRouter()     // 取得控制頁面跳轉的 router

// RouterLink：按下「登入」時由 Vue Router 切換頁面，不會重新載入整份網站
import { RouterLink, useRouter } from 'vue-router'
import api from '@/services/api'
import '../assets/register.css'

// 頁面背景與圖示沿用登入頁的素材
import registerbackground from '../assets/login-background.png'
// 左下角的紅色角色（去背 PNG，站在頁面底部）
import registerCharacter from '../assets/register-character.png'
import loginEyeIcon from '../assets/login-eye.svg'
import oauthGoogleIcon from '../assets/oauth-google.svg'
import oauthAppleIcon from '../assets/oauth-apple.svg'
import { HttpStatusCode } from 'axios'

// useRouter() 取得目前 Vue 應用程式的路由控制器，註冊成功後用它前往登入頁。
const router = useRouter()

//表單資料
const form = reactive({
  account: '',
  password: '',
  confirmpassword: '',
  nickname: '',
  email: '',
  phone: '',
})

//畫面狀態
const isSubmitting = ref(false)
const successMessage = ref('')
// 不是任何單一欄位造成的錯誤（例如後端沒啟動）才放在表單最下面
const errorMessage = ref('')

// 每個欄位自己的錯誤訊息：後端回 409 或 DTO 驗證失敗時，訊息要顯示在出錯的欄位下方
const fieldErrors = reactive({
  account: '',
  nickname: '',
  email: '',
  phone: '',
  password: '',
  confirmpassword: '',
})

// 欄位名稱對應到 input 的 id，衝突時可以把焦點移到出錯的欄位
const fieldElementIds = {
  account: 'register-account',
  nickname: 'register-nickname',
  email: 'register-email',
  phone: 'register-phone',
  password: 'register-password',
  confirmpassword: 'register-confirm-password',
}

// 後端 409 的 message 會直接指出是哪一個欄位重複
const conflictMessages = {
  account: '此帳號已被使用，請換一個帳號',
  email: '此 Email 已註冊過其他帳號，請換一個 Email',
  phone: '這支電話已註冊過其他帳號，請換一支號碼，或清空改為不填',
}

// ASP.NET Core 的 DTO 驗證錯誤 key（不分大小寫）對應回前端欄位
const dtoFieldByKey = {
  account: 'account',
  password: 'password',
  confirmpassword: 'confirmpassword',
  nickname: 'nickname',
  email: 'email',
  phone: 'phone',
}

// 表單元素，用來把焦點移到出錯的欄位
const formRef = ref(null)

// 電話格式：09 開頭，共 10 個數字
const PHONE_PATTERN = /^09\d{8}$/

function clearFieldErrors() {
  Object.keys(fieldErrors).forEach((field) => {
    fieldErrors[field] = ''
  })
}

// 後端 message 只有文字，這裡判斷是哪一個欄位撞到唯一性規則
function conflictFieldFor(message) {
  if (!message) {
    return ''
  }
  if (message.includes('電話')) {
    return 'phone'
  }
  if (message.toLowerCase().includes('email')) {
    return 'email'
  }
  if (message.includes('帳號')) {
    return 'account'
  }
  return ''
}

// field 是什麼？
// field 是「哪一個欄位發生錯誤」。
// 如 Email 重複時，程式會執行：focusField('email')
// 因此函式裡：field === 'email'
//
// 如果帳號重複：focusField('account')
// field === 'account'
async function focusField(field) {
  // 這裡的 await 可以理解成：
  // 先停在這一行，等 Vue 更新完成，再繼續往下執行。
  //
  // await nextTick() 等 Vue 完成這次畫面更新。
  // Vue 為了效能，通常會把多個資料變更集中起來，再一次更新畫面
  await nextTick()

  // # 代表「用 HTML 的 id 尋找元素」。
  // `#${fieldElementIds[email]}`
  // 結果是：'#register-email'
  //
  // formRef.value 是什麼？
  // 表單有：<form ref="formRef">
  // JavaScript 則有：const formRef = ref(null)
  // Vue 顯示表單後，會把真正的 <form> HTML 元素放進：
  // formRef.value
  // 可以把它想成：formRef.value = 目前畫面上的註冊表單
  //
  // querySelector() 在做什麼？
  // const input = formRef.value?.querySelector('#register-email')
  // 意思是：從註冊表單裡，尋找 id="register-email" 的 HTML 元素。
  // 它找到的是這個輸入框：<input id="register-email" />
  // 找到後，`input` 裡面保存的就是這個 Email 輸入框。
  const input = formRef.value?.querySelector(`#${fieldElementIds[field]}`)
  input?.focus() //將輸入游標移到該欄位
}

// 電話只收數字：貼上含空白或連字號的號碼也會自動清乾淨
function handlePhoneInput(event) {
  const digitsOnly = event.target.value.replace(/\D/g, '')
  if (digitsOnly !== event.target.value) {
    event.target.value = digitsOnly
    form.phone = digitsOnly
  }
  fieldErrors.phone = ''
}

// 以下三個是「只影響畫面」的狀態，不會送進後端
// 密碼／確認密碼是否要顯示成明碼
const isPasswordVisible = ref(false)
const isConfirmPasswordVisible = ref(false)
// 是否已勾選同意條款（後端沒有這個欄位，所以只在前端檢查）
const acceptedTerms = ref(false)
// Google／Apple 登入還沒接上 OAuth，點擊時顯示提示
const socialMessage = ref('')

function openSocialLogin(provider) {
  // 之後接上第三方登入時，這裡改成導向該平台的授權頁
  socialMessage.value = `${provider} 登入尚未開放，請先使用帳號密碼註冊`
}

//按下「註冊」後執行...
async function registerMember() {
  //使用者按下註冊,先清除上一次的成功/錯誤訊息
  //三個都是 ref，JavaScript 裡都必須使用 .value
  successMessage.value = ''
  errorMessage.value = ''
  clearFieldErrors()

  // 尚未勾選同意條款就不送 API
  if (!acceptedTerms.value) {
    errorMessage.value = '請先同意 Terms 與 Privacy Policies'
    return
  }

  // 電話是選填，但有填就必須符合「09 開頭，共 10 個數字」
  if (form.phone && !PHONE_PATTERN.test(form.phone)) {
    fieldErrors.phone = '電話需為 09 開頭、共 10 個數字，或清空改為不填'
    focusField('phone')
    return
  }

  //表示：註冊請求正在處理中
  isSubmitting.value = true

  // 後端回傳成功（2xx） → 執行 try
  // 後端回傳錯誤（4xx、5xx）→ 跳到 catch
  // try {
  //   // 註冊成功的處理
  // } catch (error) {
  //   // 註冊失敗的處理
  // }

  //若 Axios 發生錯誤，就跳到 catch
  try {
    //發送註冊請求
    //Axios 發送：
    // POST /api/Auth/register
    // Vue 前端把註冊資料送到 ASP.NET Core 後端
    // api.post(網址, 要送的資料)
    const response = await api.post('/api/Auth/register', form)

    // form     → 前端送給後端的資料（Request）
    // response → 後端回給前端的資料（Response）
    //後端：
    // return StatusCode(
    //     201,new{
    //         message = "會員建立成功",
    //         user = new
    //         {
    //             user.Id,
    //             user.Account
    //         }
    //     }
    // );
    //
    //回給前端：
    // {
    //   "message": "會員建立成功",
    //   "user": {
    //     "id": 8,
    //     "account": "hosan123"
    //   }
    // }

    // successMessage.value 一開始為空字串
    // 優先使用後端回傳的 message；如果後端沒有回傳 message，就使用前端預設的「註冊成功」
    successMessage.value = response.data.message ?? '註冊成功'
    // 左邊有值，因此結果為：會員建立成功
    //
    // 如果後端回傳：{"userId": 8},因為沒有 message,
    // 所以使用右邊：註冊成功

    // 註冊成功後清空表單
    Object.assign(form, {
      account: '',
      password: '',
      confirmpassword: '',
      nickname: '',
      email: '',
      phone: '',
    })
    // vue Router
    // axios的HttpStatusCode裡的狀態
    if (response.status === HttpStatusCode.Created) {
      alert('註冊成功')
      router.push('/login')
    }
  } catch (error) {
    //註冊 API 失敗
    //如果後端回傳：400、409、500
    //如果 error.response 存在，就取得裡面的 data
    const responseData = error.response?.data

    // 例如後端回傳：
    // {
    //   "message": "此帳號已被使用"
    // }
    //
    // responseData 就是：
    // {
    //   message: '此帳號已被使用'
    // }
    //
    // 為什麼使用 ?.
    // ?. 表示安全地繼續讀取
    // 如果後端沒啟動，根本沒有收到Response
    // error.response === undefined
    // error.response?.data 只會得到： undefined ,不會產生額外錯誤

    // 情況一：後端有回傳 message
    // responseData.message = '此帳號已被使用' => true
    // errorMessage.value = '此帳號已被使用'

    // 情況二：responseData 存在，但沒有 message
    // responseData.message = undefined
    // 在 if 中，undefined 會被視為 false
    // 不會進入大括號，也不會報錯

    // 情況三：沒有收到後端 Response
    // responseData = undefined
    // responseData?.message = undefined
    // undefined 在 if 中視為 false
    // 不會進入大括號，也不會報錯

    if (responseData?.message) {
      // 後端唯一性檢查會回 409，message 已經指出是 帳號 / Email / 電話 重複
      const conflictField = conflictFieldFor(responseData.message)
      if (conflictField) {
        fieldErrors[conflictField] = conflictMessages[conflictField]
        focusField(conflictField)
        return
      }

      errorMessage.value = responseData.message
      return
    }

    //處理「後端 DTO 驗證失敗」時，ASP.NET Core 回傳的錯誤訊息
    //例如註冊時密碼太短、Email 格式錯誤
    // responseData.errors 是「欄位名稱 → 錯誤訊息陣列」的物件，
    // 所以可以依照 key 把訊息放回對應的欄位，而不是全部擠在表單最下面。
    if (responseData?.errors) {
      // 找不到對應欄位的訊息（例如 ConfirmPassword 這種沒有獨立欄位的檢查）才留給表單下方顯示
      const unmatchedMessages = []

      Object.entries(responseData.errors).forEach(([key, messages]) => {
        const field = dtoFieldByKey[String(key).toLowerCase()]
        const message = Array.isArray(messages) ? messages[0] : String(messages)

        // 同一個欄位只保留第一則訊息
        if (field && !fieldErrors[field]) {
          fieldErrors[field] = message
          return
        }

        unmatchedMessages.push(message)
      })

      errorMessage.value = unmatchedMessages[0] ?? ''

      // 把焦點移到第一個出錯的欄位，使用者才知道要看哪裡
      const firstErrorField = Object.keys(fieldElementIds).find((field) => fieldErrors[field])
      if (firstErrorField) {
        focusField(firstErrorField)
      }

      return
    }
    //// 條件不成立，才會走到這裡
    // 無法連線或其他錯誤
    errorMessage.value = '註冊失敗，請確認後端是否已啟動'
  } finally {
    //從「註冊中……」恢復成「註冊」，並解除 disabled
    isSubmitting.value = false
  }
}
</script>

<!-- 畫面與表單 -->
<template>
  <main class="register-page">
    <img class="register-background" :src="registerbackground" alt="" aria-hidden="true" />
    <img
      class="register-character"
      :src="registerCharacter"
      alt=""
      aria-hidden="true"
      draggable="false"
    />

    <div class="register-content">
      <h1 class="register-title">會員註冊</h1>
      <p class="register-subtitle">Let's get you all up so you can access your personal account.</p>

      <form
        ref="formRef"
        class="register-form"
        :aria-busy="isSubmitting"
        @submit.prevent="registerMember"
      >
        <div class="register-field">
          <label for="register-account">帳號</label>
          <input
            id="register-account"
            v-model.trim="form.account"
            :class="{ 'is-invalid': fieldErrors.account }"
            :aria-invalid="fieldErrors.account ? 'true' : undefined"
            :aria-describedby="fieldErrors.account ? 'register-account-error' : undefined"
            type="text"
            autocomplete="username"
            required
            @input="fieldErrors.account = ''"
          />
          <p
            v-if="fieldErrors.account"
            id="register-account-error"
            class="register-field-error"
            role="alert"
          >
            {{ fieldErrors.account }}
          </p>
        </div>

        <div class="register-field">
          <label for="register-nickname">暱稱</label>
          <input
            id="register-nickname"
            v-model.trim="form.nickname"
            :class="{ 'is-invalid': fieldErrors.nickname }"
            :aria-invalid="fieldErrors.nickname ? 'true' : undefined"
            :aria-describedby="fieldErrors.nickname ? 'register-nickname-error' : undefined"
            type="text"
            autocomplete="nickname"
            required
            @input="fieldErrors.nickname = ''"
          />
          <p
            v-if="fieldErrors.nickname"
            id="register-nickname-error"
            class="register-field-error"
            role="alert"
          >
            {{ fieldErrors.nickname }}
          </p>
        </div>

        <div class="register-field">
          <label for="register-email">Email</label>
          <input
            id="register-email"
            v-model.trim="form.email"
            :class="{ 'is-invalid': fieldErrors.email }"
            :aria-invalid="fieldErrors.email ? 'true' : undefined"
            :aria-describedby="fieldErrors.email ? 'register-email-error' : undefined"
            type="email"
            autocomplete="email"
            required
            @input="fieldErrors.email = ''"
          />
          <p
            v-if="fieldErrors.email"
            id="register-email-error"
            class="register-field-error"
            role="alert"
          >
            {{ fieldErrors.email }}
          </p>
        </div>

        <div class="register-field">
          <label for="register-phone">電話（選填）</label>
          <input
            id="register-phone"
            v-model.trim="form.phone"
            :class="{ 'is-invalid': fieldErrors.phone }"
            :aria-invalid="fieldErrors.phone ? 'true' : undefined"
            :aria-describedby="fieldErrors.phone ? 'register-phone-error' : undefined"
            type="tel"
            inputmode="numeric"
            maxlength="10"
            pattern="09[0-9]{8}"
            title="09 開頭，共 10 個數字"
            autocomplete="tel"
            placeholder="→ 09 開頭，共 10 個數字"
            @input="handlePhoneInput"
            @invalid.prevent="fieldErrors.phone = '電話需為 09 開頭、共 10 個數字，或清空改為不填'"
          />
          <p
            v-if="fieldErrors.phone"
            id="register-phone-error"
            class="register-field-error"
            role="alert"
          >
            {{ fieldErrors.phone }}
          </p>
        </div>

        <div class="register-field register-field--wide">
          <label for="register-password">密碼</label>
          <div class="register-input-wrap">
            <input
              id="register-password"
              v-model="form.password"
              :class="{ 'is-invalid': fieldErrors.password }"
              :aria-invalid="fieldErrors.password ? 'true' : undefined"
              :aria-describedby="fieldErrors.password ? 'register-password-error' : undefined"
              :type="isPasswordVisible ? 'text' : 'password'"
              autocomplete="new-password"
              required
              @input="fieldErrors.password = ''"
            />
            <button
              class="register-eye"
              type="button"
              :aria-label="isPasswordVisible ? '隱藏密碼' : '顯示密碼'"
              :aria-pressed="isPasswordVisible"
              @click="isPasswordVisible = !isPasswordVisible"
            >
              <img :src="loginEyeIcon" alt="" aria-hidden="true" />
            </button>
          </div>
          <p
            v-if="fieldErrors.password"
            id="register-password-error"
            class="register-field-error"
            role="alert"
          >
            {{ fieldErrors.password }}
          </p>
        </div>

        <div class="register-field register-field--wide">
          <label for="register-confirm-password">確認密碼</label>
          <div class="register-input-wrap">
            <input
              id="register-confirm-password"
              v-model="form.confirmpassword"
              :class="{ 'is-invalid': fieldErrors.confirmpassword }"
              :aria-invalid="fieldErrors.confirmpassword ? 'true' : undefined"
              :aria-describedby="
                fieldErrors.confirmpassword ? 'register-confirm-password-error' : undefined
              "
              :type="isConfirmPasswordVisible ? 'text' : 'password'"
              autocomplete="new-password"
              required
              @input="fieldErrors.confirmpassword = ''"
            />
            <button
              class="register-eye"
              type="button"
              :aria-label="isConfirmPasswordVisible ? '隱藏確認密碼' : '顯示確認密碼'"
              :aria-pressed="isConfirmPasswordVisible"
              @click="isConfirmPasswordVisible = !isConfirmPasswordVisible"
            >
              <img :src="loginEyeIcon" alt="" aria-hidden="true" />
            </button>
          </div>
          <p
            v-if="fieldErrors.confirmpassword"
            id="register-confirm-password-error"
            class="register-field-error"
            role="alert"
          >
            {{ fieldErrors.confirmpassword }}
          </p>
        </div>

        <label class="register-terms">
          <input v-model="acceptedTerms" type="checkbox" />
          <span>
            I agree to all the <a class="register-terms-link" href="#" @click.prevent>Terms</a> and
            <a class="register-terms-link" href="#" @click.prevent>Privacy Policies</a>
          </span>
        </label>

        <!-- type="submit" 表示這是「送出表單」按鈕 -->
        <!-- :disabled="isSubmitting" 註冊中會停用按鈕，避免重複送出 -->
        <button class="register-submit" type="submit" :disabled="isSubmitting">
          {{ isSubmitting ? '建立中...' : '建立帳號' }}
        </button>

        <p v-if="errorMessage" class="register-message register-message--error" role="alert">
          {{ errorMessage }}
        </p>
        <p v-if="successMessage" class="register-message register-message--success" role="status">
          {{ successMessage }}
        </p>
      </form>

      <p class="register-login">已經有帳號？<RouterLink to="/login">登入</RouterLink></p>

      <div class="register-social">
        <p class="register-social-divider">或者用其他登入？</p>
        <div class="register-social-buttons">
          <button
            class="register-social-button"
            type="button"
            aria-label="使用 Google 帳號註冊"
            @click="openSocialLogin('Google')"
          >
            <img :src="oauthGoogleIcon" alt="" aria-hidden="true" />
          </button>
          <button
            class="register-social-button"
            type="button"
            aria-label="使用 Apple 帳號註冊"
            @click="openSocialLogin('Apple')"
          >
            <img :src="oauthAppleIcon" alt="" aria-hidden="true" />
          </button>
        </div>
        <p class="register-social-message" role="status" aria-atomic="true">{{ socialMessage }}</p>
      </div>
    </div>
  </main>
</template>
