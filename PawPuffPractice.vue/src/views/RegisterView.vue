<!-- 完整的會員註冊頁面 -->

<!-- JavaScript 邏輯 -->
<script setup>
import { reactive, ref } from 'vue'
import api from '@/services/api'

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
const errorMessage = ref('')

//按下「註冊」後執行...
async function registerMember() {
  //使用者按下註冊,先清除上一次的成功/錯誤訊息
  //三個都是 ref，JavaScript 裡都必須使用 .value
  successMessage.value = ref('')
  errorMessage.value = ref('')

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
      errorMessage.value = responseData.message
      return
    }

    //處理「後端 DTO 驗證失敗」時，ASP.NET Core 回傳的錯誤訊息
    //例如註冊時密碼太短、Email 格式錯誤
    //?. 稱為 Optional Chaining（選擇性串連）
    // responseData 存在 → 讀取 errors
    // responseData 不存在 → 得到 undefined，不會當機
    if (responseData?.errors) {
      const messages = Object.values(responseData.errors).flat()

      // 假如：
      // responseData.errors = {
      //   Password: ['密碼必須至少 8 個字元'],
      //   Email: ['Email 格式不正確'],
      // }
      // Object.values(responseData.errors)
      // => 外層原本是物件 {} 把它轉成陣列 [],會移除 Password、Email 這些欄位名稱，只保留內容
      //Object.values() 的作用就是：不要物件的屬性名稱，只取出每個屬性的值，並把這些值裝進一個新陣列。
      // [
      //   ['密碼必須至少 8 個字元'],
      //   ['Email 格式不正確'],
      // ]
      //接著 .flat() 再把內層陣列攤平：
      // [
      //   '密碼必須至少 8 個字元',
      //   'Email 格式不正確',
      // ]

      errorMessage.value = messages[0] ?? '輸入資料不正確'
      //messages[0]:'密碼必須至少 8 個字元'
      // => errorMessage.value = '密碼必須至少 8 個字元'
      //
      // 而：undefined ?? '輸入資料不正確'
      // => errorMessage.value = '輸入資料不正確'

      return
      //迴圈結束
      // errorMessage.value = '註冊失敗，請確認後端是否已啟動' 不會執行
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
  <!-- main代表頁面的主要內容 -->
  <main class="register-page">
    <h1>會員註冊</h1>

    <!-- 加入表單 -->
    <!-- @submit 表示使用者送出表單時.prevent 阻止瀏覽器重新整理頁面 -->
    <!-- 送出表單時，不要重新整理頁面，改成執行 registerMember() -->
    <form @submit.prevent="registerMember" class="register-form">
      <!-- 先建立一個帳號欄位 -->
      <!-- 輸入框的名稱是「帳號」 -->
      <label>
        帳號
        <!-- v-model 雙向綁定 -->
        <!--使用者在輸入框輸入的 與  form.account 做綁定-->
        <!-- 輸入 pawpuff01 -綁定-> form.account === 'pawpuff01' -->
        <!-- type="text" 代表一般文字輸入框  -->
        <!-- autocomplete="username" 是在告訴 Chrome、Safari 等瀏覽器：這個輸入框是使用者的登入帳號 就可能顯示建議：pawpuff01 
使用者點選後，瀏覽器會協助填入帳號-->
        <!-- required 代表必填。 沒有輸入時，瀏覽器會阻止表單送出-->
        <input v-model.trim="form.account" type="text" autocomplete="username" required />
      </label>

      <!-- 為什麼密碼沒有 .trim？ 因為空白可能是密碼的一部分。
    因此密碼通常不要自動 .trim。-->
      <!-- type="password" 會用圓點或星號遮住輸入內容：••• -->
      <!-- autocomplete="new-password"  瀏覽器可能會：建議安全密碼/提示是否儲存密碼/避免自動填入其他網站的舊密碼-->
      <label>
        密碼
        <input v-model="form.password" type="password" autocomplete="new-password" required />
      </label>

      <label>
        確認密碼
        <input
          v-model="form.confirmpassword"
          type="password"
          autocomplete="new-password"
          required
        />
      </label>

      <label>
        暱稱
        <input v-model.trim="form.nickname" type="text" required />
      </label>

      <label>
        Email

        <!-- type="email" 告訴瀏覽器這是 Email 欄位 -->
        <!-- 後端仍然要有： 
        [EmailAddress(ErrorMessage = "Email 格式不正確")] -->
        <!-- autocomplete="email" 告訴瀏覽器：這個輸入框是電子郵件 -->
        <input v-model.trim="form.email" type="email" autocomplete="email" required />
      </label>

      <label>
        電話(選填)

        <!-- type="tel" 告訴瀏覽器：這是一個電話號碼輸入框 -->
        <!-- 在電腦上看起來和普通文字輸入框很像。  -->
        <!-- 在手機上，瀏覽器通常會顯示適合輸入電話的鍵盤 -->

        <!-- autocomplete="tel" 告訴瀏覽器：這個欄位要填電話號碼 -->
        <!-- 如果使用者曾經在瀏覽器中保存個人資料，瀏覽器可能建議： 0912345678 -->
        <input
          v-model="form.phone"
          type="tel"
          autocomplete="tel"
          placeholder="-> 09開頭,共10個數字"
        />
      </label>

      <!-- type="submit" 表示這是「送出表單」按鈕 -->

      <!-- 完整寫法是：v-bind:disabled="isSubmitting" -->
      <!-- 簡寫成： :disabled="isSubmitting"  -->
      <!-- : 表示 disabled 後面接的是 JavaScript 資料，不是普通文字 -->
      <!-- isSubmitting.value = true => 按鈕會變成停用狀態 disabled-->

      <!-- {{ }} 表示把 JavaScript 的結果顯示在畫面上 -->
      <!-- isSubmitting 是 true
          → 顯示「註冊中…」

          isSubmitting 是 false
          → 顯示「註冊」 -->

      <!-- 為什麼模板裡不用 .value？ -->
      <!-- JavaScript 裡要寫：isSubmitting.value = true  -->
      <!-- 但 Vue <template> 會自動把 ref 解開，所以模板直接寫： isSubmitting -->
      <button type="submit" :disabled="isSubmitting">
        {{ isSubmitting ? '註冊中...' : '註冊' }}
      </button>
    </form>

    <!-- true => 顯示成功訊息 -->
    <p v-if="successMessage" class="success-message">
      {{ successMessage }}
    </p>

    <!-- false => 顯示失敗訊息 -->
    <p v-if="errorMessage" class="error-message">
      {{ errorMessage }}
    </p>
  </main>
</template>

<!-- 這個頁面的 CSS -->
<!-- <style scoped> 表示這些 CSS 只套用在目前的 RegisterView.vue，盡量不要影響其他 Vue 頁面 -->
<!-- <style> 可能會影響整個網站 -->
<style scoped>
/* class = "register-page" */
.register-page {
  /* min() : 從兩個數值中選擇較小的  */
  /* 例如瀏覽器寬度是 1200px：min(1200px, 480px) → 480px */
  width: min(100%, 480px);

  /* margin:上下、左右的簡寫 */
  /* 上下留 40px 左右自動分配空間 */
  margin: 40px auto;
  /* 表示內容和容器邊緣之間留 24px */
  padding: 24px;
  /* 外框
┌─────────────────────┐
│      24px 空間       │
│   會員註冊與表單內容   │
│      24px 空間       │
└─────────────────────┘ */

  /* margin 是元素外面的空間，padding 是元素裡面的空間 */
}
/* display: grid; 把表單變成 CSS Grid 排版 */
/* 所以表單裡的每一個子元素會由上往下排列:
帳號欄位
密碼欄位
確認密碼欄位
暱稱欄位
Email 欄位
電話欄位
註冊按鈕*/
/* gap: 16px; 讓表單裡每個項目之間保留 16px 距離 */
.register-form {
  display: grid;
  gap: 16px;
}
/* 
┌──────────────────┐
│ 第一個完整 label   │
├──────────────────┤
│ 第二個完整 label   │
├──────────────────┤
│ button           │
└──────────────────┘
*/

/*  display: grid; 讓 label 裡的文字和輸入框上下排列
帳號
[輸入框]
如果沒有這個設定，它們可能排在同一行： 帳號 [輸入框]
*/

/* gap: 6px  欄位名稱和輸入框之間留 6px
帳號
  ↕ 6px
[輸入框]
*/
label {
  display: grid;
  gap: 6px;
}
/* 
帳號
[帳號輸入框]

密碼
[密碼輸入框]

[註冊]
 */

/* padding: 12px; 表示按鈕文字和按鈕邊框之間留 12px */
/* cursor: pointer;滑鼠移到按鈕上時，游標會變成手指形狀 */
button {
  padding: 12px;
  cursor: pointer;
}

/* cursor: not-allowed; 滑鼠移到停用按鈕時，游標顯示「禁止操作」的樣式  */
/* opacity: 1   → 完全清楚
opacity: 0.6 → 稍微透明
opacity: 0   → 完全看不到 */
button:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.error-message {
  color: red;
}

.success-message {
  color: green;
}
</style>
