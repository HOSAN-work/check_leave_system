<!-- 
使用者輸入目前密碼
使用者輸入新密碼
使用者再次輸入新密碼
送出按鈕 
--------
前端確認：
兩次新密碼一致
新密碼至少需要6個字
新密碼不能與目前密碼相同
成功顯示成功訊息
失敗顯示錯誤訊息
請求期間不能重複送出 -->

<!-- JavaScript 邏輯 -->
<script setup>
import { reactive, ref } from 'vue'
import { useAuth } from '@/composables/useAuth'

// 盡量跟後端Dto裡的欄位一致
const form = reactive({
  currentPassword: '',
  newPassword: '',
  confirmNewPassword: '',
})

const isSubmitting = ref(false)
const successMessage = ref('')
const errorMessage = ref('')

const { changePassword } = useAuth()

async function submitPassword() {
  successMessage.value = ''
  errorMessage.value = ''

  if (form.currentPassword === form.newPassword) {
    errorMessage.value = '新密碼不能與目前密碼相同'
    return
  }

  if (form.newPassword !== form.confirmNewPassword) {
    errorMessage.value = '兩次輸入的新密碼不一致'
    return
  }

  if (form.newPassword.length < 6) {
    errorMessage.value = '新密碼至少需要6個字'
    return
  }

  isSubmitting.value = true //修改中...

  try {
    // changePassword(一個物件)
    // 呼叫端
    // {
    //   currentPassword: form.currentPassword,
    //   newPassword: form.newPassword,
    // }
    //               ↓ 傳進去
    // changePassword(passwordData)
    //                ↑
    //         passwordData 接住它
    const result = await changePassword({
      currentPassword: form.currentPassword,
      newPassword: form.newPassword,
      confirmNewPassword: form.confirmNewPassword,
    })

    successMessage.value = result.message ?? '密碼修改成功'
    // 假設後端回傳:
    // return Ok(new
    // {
    //     message = "密碼修改成功"
    // });
    // Axios 收到之後：response.data :
    // {
    //   message: '密碼修改成功'
    // }
    // changePassword()：return response.data
    // 等於：
    // return {
    //   message: '密碼修改成功'
    // }
    // 因此外面：
    // const result = await changePassword(...)
    // result 就是：
    // {
    //   message: '密碼修改成功'
    // }
    // result.message 就是：密碼修改成功

    // 為什麼放在 try 裡？ => 因為只有修改成功，才應該清空表單
    // 修改成功後，清空密碼欄位
    form.currentPassword = ''
    form.newPassword = ''
    form.confirmNewPassword = ''
  } catch (error) {
    // 例如：目前密碼錯誤\Cookie 已失效\後端沒有啟動\資料庫錯誤
    console.error('修改密碼錯誤：', error)

    errorMessage.value = error.response?.data?.message ?? '密碼修改失敗'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<!-- HTML 畫面 -->
<template>
  <main>
    <h1>修改密碼</h1>

    <!-- 表單送出
→ prevent 阻止瀏覽器重新整理
→ 執行 submitPassword() 
-->
    <form @submit.prevent="submitPassword">
      <div>
        <label for="current-password"> 目前密碼 </label>

        <!--label -> for="current-password"
對應  id="current-password" -->
        <!-- autocomplete 到底在幹嘛？
假設你使用 Safari、Chrome，或 iCloud Keychain、Google Password Manager
=> 假設要設定「新的密碼」=> 因此瀏覽器／密碼管理器可能提供「建議強密碼」、產生新密碼，或避免誤把舊密碼填進去 -->

        <!-- autocomplete="current-password" 
=> 瀏覽器可能從已保存的登入資料填入舊密碼-->

        <!-- 為什麼需要v-model 「雙向」？
因為表單通常會發生兩件事：
① 使用者在畫面輸入東西
   ↓
   JavaScript 要拿得到

一開始：form.currentPassword  // ''
畫面：     目前密碼：[          ]
使用者輸入：目前密碼：[abc123]
因為有：<input v-model="form.currentPassword">
Vue 自動幫你變成：form.currentPassword. // 'abc123'
所以之後你送 API：
await api.put('/password', {
  currentPassword: form.currentPassword
})
就能取得：'abc123'

② JavaScript 修改資料
   ↓
   畫面的 input 也要跟著改

假設現在：form.currentPassword = 'abc123'
畫面的 input 也會自動顯示： 目前密碼：[abc123]
如果你：form.currentPassword = ''
畫面就會變：目前密碼：[          ]

form.currentPassword = ''
        ↓
v-model
        ↓
input 也被清空 -->

        <input
          id="current-password"
          v-model="form.currentPassword"
          type="password"
          autocomplete="current-password"
          required
        />
      </div>

      <!-- 
autocomplete="new-password"
將剛才建議的新密碼也填入確認欄位
將使用者選擇的新密碼填入這兩個新密碼欄位
把它辨識為密碼更新流程的一部分
避免把舊密碼誤填到這裡 
-->
      <div>
        <label for="new-password"> 新密碼 </label>
        <input
          id="new-password"
          v-model="form.newPassword"
          type="password"
          autocomplete="new-password"
          required
        />
      </div>

      <div>
        <label for="confirm-password"> 確認新密碼 </label>
        <input
          id="confirm-password"
          v-model="form.confirmNewPassword"
          type="password"
          autocomplete="new-password"
          required
        />
      </div>

      <button type="submit" :disabled="isSubmitting">
        {{ isSubmitting ? '修改中...' : '修改密碼' }}
      </button>

      <p v-if="successMessage" class="success">
        {{ successMessage }}
      </p>

      <p v-if="errorMessage" class="error">
        {{ errorMessage }}
      </p>
    </form>
  </main>
</template>

<!-- CSS 樣式  -->
<style scoped>
main {
  box-sizing: border-box;
  width: min(100%, 500px);
  margin: 40px auto;
  padding: 24px;
}

form {
  display: grid;
  gap: 16px;
}

form > div {
  display: grid;
  gap: 8px;
}

input,
button {
  box-sizing: border-box;
  width: 100%;
  padding: 10px;
}

.success {
  color: green;
}

.error {
  color: red;
}
</style>
