<!-- 
1. 顯示目前會員資料
2. 允許使用者修改資料
3. 按下儲存後呼叫後端 API
4. 成功顯示成功訊息
5. 失敗顯示錯誤訊息
6. 儲存期間避免重複點擊
7. 取消時回會員頁 -->

<script setup>
import { reactive,ref,watch } from 'vue';
import { useAuth } from '@/composables/useAuth';

import { useRouter } from 'vue-router';

const router = useRouter()

// 第一步
// UpdateProfileRequestDto 要顯示 Nickname、Email、Phone、Biography
// 所以前端必須準備相同用途的欄位 => 物件 reactive
// 為什麼用 reactive？=>因為這是由多個欄位組成的表單物件
const form =  reactive({
    nickname:'',
    email:'',
    phone:'',
    biography:''
})

// 第二步：頁面需要目前會員資料
// currentUser   → 現在的會員資料
// updateProfile → 把修改後的資料送到後端
const{
    currentUser,
    updateProfile
} = useAuth()



// 表單狀態
const isSubmitting = ref(false) 
const successMessage = ref('')
const errorMessage = ref('')


// 第三步：把會員資料放進表單
// 因為 /api/Auth/me 是非同步請求，可能是：
// 頁面先建立 => currentUser 暫時為 null
// 後端稍後回應 => currentUser 才取得會員資料
// 因此使用：watch(currentUser, ...) 
// => 監看 currentUser 的變化
//
// 之前的登入與註冊頁沒有使用 watch，是因為它們不需要等待會員原始資料填入表單。
// currentUser：你主動傳給 watch()，告訴 Vue「我要監看這個資料」
// user：當 currentUser 改變時，Vue 自動把它的新值傳給回呼函式
// watch(要監看的資料, 資料改變後執行的函式)
// 監看 currentUser；currentUser 改變時，把它的新值放進 user，然後執行函式。
watch(
    currentUser,

// 當 currentUser 改變時，Vue 會執行這個函式
    (user) => {
        // 最初 currentUser =null => user = null
        // null == false 
        // !null == true -> return 迴圈結束
        if(!user){
            return
        }
        // user.nickname 或者是 空字串
        form.nickname = user.nickname ?? ''
        form.email = user.email ?? ''
        form.phone = user.phone ?? ''
        form.biography = user.biography ?? ''
    },

    // 建立這個 watch 時，立刻執行
    // (user) => {
    // 將 currentUser 的資料放進 form
    // }
    {
        immediate:true,
    }
)

// 使用者按下「儲存修改」時執行的函式
// 
// 清除舊訊息
// → 鎖住儲存按鈕
// → 將表單資料傳給 updateProfile()
// → 等待後端修改資料
// → 成功顯示成功訊息
// → 失敗顯示錯誤訊息
// → 最後恢復按鈕
async function submitProfile(){
    successMessage.value = ''
    errorMessage.value = ''
    isSubmitting.value = true

    try{
        const result = await updateProfile({
            nickname: form.nickname,
            email: form.email,
            phone:form.phone || null,
            biography: form.biography || null
        })
        // form.nickname對應updateProfile的request.Nickname
        // form.email對應updateProfile的request.email
        // form.phone對應updateProfile的request.phone
        // form.biography對應updateProfile的request.biography

        // 如果是失敗的話,會直接跳到catch,就不會跳到successMessage
        successMessage.value = result.message ?? '會員資料修改成功'
        
    }
    catch(error){
        console.error('修改會員資料錯誤',error)

        // ?. 是安全讀取,代表：前面的東西存在，才繼續讀取下一層
        // error.response 但是如果後端根本沒有啟動、網路斷線或請求無法送達 => error.response === undefined
        // 如果直接寫：error.response.data.message 嘗試讀取：undefined.data => 產生另一個錯誤
        // 
        // 使用：error.response?.data?.message 拿不到時只會得到：undefined => 不會再次發生錯誤
        errorMessage.value = 
        error.response?.data?.message ?? '會員資料修改失敗'
    }
    finally{
        isSubmitting.value = false 
    }
}

    // 「取消編輯」功能
    // 使用者點擊「取消」按鈕時，不儲存目前輸入的資料，直接跳回會員頁 /member
    function cancelEdit(){
        // 告訴 Vue Router：導航到 /member
        router.push('/member')
    }
</script>

<template>
  <main>
    <h1>修改會員資料</h1>

    <form @submit.prevent="submitProfile">

<!-- for="nickname" 會尋找同一個 HTML 頁面中 id="nickname" 的元素 -->
<!-- <label for="nickname">
            │
            │ 尋找相同的 id
            ↓
<input id="nickname"> -->
        <div>
            <label for="nickname">
                暱稱
            </label>
            <!-- form.nickname 是對應<script setup>的const from = reactive({...})裡的nickname:'' -->
            <input
            id="nickname"
            v-model.trim="form.nickname"
            type="text"
            maxlength="50"
            required
            />
        </div>

        <div>
            <label for="email">
                Email
            </label>
            <input
            id="email"
            v-model.trim="form.email"
            type="email"
            maxlength="255"
            required
            />
        </div>

        <div>
            <label for="phone">
                電話
            </label>
            <input
            id="phone"
            v-model.trim="form.phone"
            type="tel"
            maxlength="10"
            />
        </div>

        <div>
            <label for="biography">
                簡介
            </label>
        <!-- <textarea> 是 HTML 的「多行文字輸入框」 -->
        <!-- textarea 則能輸入多行：
            第一行
            第二行
            第三行 -->
            <textarea
            id="biography"
            v-model.trim="form.biography"
            maxlength="500">
            </textarea>
        </div>

        <button 
        type="submit"
        :disabled="isSubmitting">
        {{ isSubmitting ? '儲存中...' : '儲存修改' }}
        </button>

        <button
        type="button"
        :disabled="isSubmitting"
        @click="cancelEdit">
            取消
        </button>

        <p
        v-if="successMessage"
        class="success">
            {{ successMessage }}
        </p>

        <p
        v-if="errorMessage"
        class="error">
            {{ errorMessage }}
        </p>
    </form>
  </main>
</template>

<style scoped>
main {
/*<main> 區塊最大寬度為100% 或 500px，哪個比較小就使用哪個*/
  width: min(100%, 500px);
/* margin: 上下 左右; */
/* 左右平分：auto => 所以main置中 */
/* 例如：|---350px---| main 500px |---350px---| */
  margin: 40px auto;
/* <main> 裡面的內容，與 <main> 自己的四周邊緣保持24px距離 */
  padding: 24px;
}

/* margin  → 元素外面的距離
   padding → 元素裡面的距離 */
/*              margin（外距）
    ┌───────────────────────────┐
    │       border（邊框）       │
    │   ┌───────────────────┐   │
    │   │ padding（內距）    │   │
    │   │   實際內容         │   │
    │   └───────────────────┘   │
    └───────────────────────────┘ */

form { 
/* 把 <form> 設定成 CSS Grid 排版容器  */
/* 
form
├── div
│   ├── label
│   └── input
├── div
│   ├── label
│   └── input
└── button
 */
/* 
form 的直接子元素只有：
第一個 div
第二個 div
button
  */
/* 當 button 成為 Grid item 後，Grid 預設會讓子元素在橫向伸展 */
/* 通常為什麼會使用 Grid？ => 一般會和 gap 一起用*/
/* 如果不用 Grid，就不能在一般區塊排版中直接依賴 gap */
/* 
暱稱欄位組
    16px
Email 欄位組
    16px
電話欄位組
    16px
按鈕
 */
  display: grid;
  gap: 16px;
}

form div {
  display: grid;
  gap: 6px;
}

input,textarea,button {
  padding: 10px;
}

textarea {
/* textarea 的高度最少為 120px */
  min-height: 120px;
/* 使用者只能用滑鼠垂直調整 textarea 的高度 */
  resize: vertical;
}

.success {
  color: green;
}

.error {
  color: red;
}
</style>