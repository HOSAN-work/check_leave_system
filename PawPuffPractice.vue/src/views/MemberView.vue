<script setup>
// nextTick：等 Vue 更新完畫面後，才去取得或操作 <dialog> 元素
// reactive：把多個欄位組成的表單包成一個物件
// ref：保存單一狀態，例如「是否正在儲存」
import { nextTick, reactive, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import { useAuth } from '@/composables/useAuth'
import '../assets/admin-clock.css'
import '../assets/drip-button.css'
import '../assets/member.css'
import dashboardPaper from '../assets/dashboard-paper.png'
import dashboardProfile from '../assets/profile.png'
import navClockIcon from '../assets/dashboard-nav-clock.svg'
import navUserIcon from '../assets/dashboard-nav-user.svg'
import navLeaveIcon from '../assets/dashboard-nav-leave.svg'
import navAdminIcon from '../assets/dashboard-nav-admin.svg'

const {
  currentUser,
  authInitialized,
  isLoggedIn,
  logout,
  // 修改會員資料
  updateProfile,
  // 修改密碼
  changePassword,
} = useAuth()

// 兩個彈窗的 DOM 參照，對應 template 的 ref="profileDialog" 與 ref="passwordDialog"
const profileDialog = ref(null)
const passwordDialog = ref(null)

// 修改會員資料表單：欄位要與 UpdateProfileRequestDto 一致
const profileForm = reactive({
  nickname: '',
  email: '',
  phone: '',
  biography: '',
})

// 修改密碼表單：欄位要與 ChangePasswordRequestDto 一致
const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmNewPassword: '',
})

const isSavingProfile = ref(false)
const isSavingPassword = ref(false)
const profileErrorMessage = ref('')
const passwordErrorMessage = ref('')

// 彈窗關閉後顯示在使用者中心的成功訊息
const feedbackMessage = ref('')

// 把目前使用者資料放進表單
function fillProfileForm(user) {
  profileForm.nickname = user?.nickname ?? ''
  profileForm.email = user?.email ?? ''
  profileForm.phone = user?.phone ?? ''
  profileForm.biography = user?.biography ?? ''
}

// /api/Auth/me 是非同步請求：畫面先建立時 currentUser 可能還是 null，
// 所以用 watch 等資料回來後再填入表單。
watch(
  currentUser,
  (user) => {
    if (!user) {
      return
    }
    fillProfileForm(user)
  },
  { immediate: true },
)

// 每次開啟彈窗都重新填入目前會員資料，
// 避免上一次按「取消」時留下的輸入內容還留在欄位裡。
async function openProfileDialog() {
  fillProfileForm(currentUser.value)
  profileErrorMessage.value = ''
  feedbackMessage.value = ''
  await nextTick()
  profileDialog.value?.showModal()
}

function openPasswordDialog() {
  // 密碼欄位一律從空白開始
  passwordForm.currentPassword = ''
  passwordForm.newPassword = ''
  passwordForm.confirmNewPassword = ''
  passwordErrorMessage.value = ''
  feedbackMessage.value = ''
  passwordDialog.value?.showModal()
}

// 儲存期間不允許關閉，避免請求成功後畫面狀態對不上
function closeProfileDialog() {
  if (isSavingProfile.value) {
    return
  }
  profileDialog.value?.close()
  profileErrorMessage.value = ''
}

function closePasswordDialog() {
  if (isSavingPassword.value) {
    return
  }
  passwordDialog.value?.close()
  passwordErrorMessage.value = ''
}

// 按下「儲存修改」：成功後關閉彈窗，並在會員中心顯示訊息
async function submitProfile() {
  profileErrorMessage.value = ''
  feedbackMessage.value = ''
  isSavingProfile.value = true

  try {
    const result = await updateProfile({
      nickname: profileForm.nickname,
      email: profileForm.email,
      phone: profileForm.phone || null,
      biography: profileForm.biography || null,
    })

    profileDialog.value?.close()
    feedbackMessage.value = result.message ?? '會員資料修改成功'
  } catch (error) {
    console.error('修改會員資料錯誤', error)
    profileErrorMessage.value = error.response?.data?.message ?? '會員資料修改失敗'
  } finally {
    isSavingProfile.value = false
  }
}

// 按下「修改密碼」：前端先做三項檢查，再呼叫後端
async function submitPassword() {
  passwordErrorMessage.value = ''
  feedbackMessage.value = ''

  if (passwordForm.currentPassword === passwordForm.newPassword) {
    passwordErrorMessage.value = '新密碼不能與目前密碼相同'
    return
  }

  if (passwordForm.newPassword !== passwordForm.confirmNewPassword) {
    passwordErrorMessage.value = '兩次輸入的新密碼不一致'
    return
  }

  if (passwordForm.newPassword.length < 6) {
    passwordErrorMessage.value = '新密碼至少需要6個字'
    return
  }

  isSavingPassword.value = true

  try {
    const result = await changePassword({
      currentPassword: passwordForm.currentPassword,
      newPassword: passwordForm.newPassword,
      confirmNewPassword: passwordForm.confirmNewPassword,
    })

    passwordDialog.value?.close()
    passwordForm.currentPassword = ''
    passwordForm.newPassword = ''
    passwordForm.confirmNewPassword = ''
    feedbackMessage.value = result.message ?? '密碼修改成功'
  } catch (error) {
    console.error('修改密碼錯誤', error)
    passwordErrorMessage.value = error.response?.data?.message ?? '密碼修改失敗'
  } finally {
    isSavingPassword.value = false
  }
}
</script>

<template>
  <div class="admin-clock-page member-page">
    <a class="dashboard-skip-link" href="#member-main">跳到主要內容</a>
    <aside class="dashboard-sidebar">
      <div class="dashboard-avatar" aria-hidden="true">
        <img :src="dashboardProfile" alt="" />
      </div>
      <nav class="dashboard-nav" aria-label="主要導覽">
        <RouterLink class="dashboard-nav-link" to="/admin/clock">
          <img :src="navClockIcon" alt="" aria-hidden="true" />打卡功能
        </RouterLink>
        <RouterLink class="dashboard-nav-link is-section-active" aria-current="page" to="/member">
          <img :src="navUserIcon" alt="" aria-hidden="true" />使用者中心
        </RouterLink>
        <RouterLink
          v-if="currentUser?.role === 'Admin'"
          class="dashboard-nav-link"
          to="/admin/leave"
        >
          <img :src="navLeaveIcon" alt="" aria-hidden="true" />請假功能
        </RouterLink>
        <RouterLink
          v-if="currentUser?.role === 'Admin'"
          class="dashboard-nav-link"
          to="/admin/users"
        >
          <img :src="navAdminIcon" alt="" aria-hidden="true" />帳號管理
        </RouterLink>
      </nav>
      <div class="dashboard-logout-wrap drip-button-wrap">
        <span class="drip-button-drips" aria-hidden="true"></span>
        <button class="dashboard-logout drip-button" type="button" @click="logout">登出</button>
      </div>
    </aside>

    <main id="member-main" class="dashboard-paper" tabindex="-1">
      <img class="dashboard-paper-texture" :src="dashboardPaper" alt="" aria-hidden="true" />
      <div class="dashboard-content member-content">
        <h1>使用者中心</h1>
        <p v-if="!authInitialized" class="member-state">正在確認登入狀態...</p>
        <template v-else-if="isLoggedIn">
          <p v-if="feedbackMessage" class="member-feedback" role="status">
            {{ feedbackMessage }}
          </p>

          <section class="member-card" aria-labelledby="member-profile-title">
            <div class="member-card-heading">
              <img :src="dashboardProfile" alt="" aria-hidden="true" />
              <div>
                <h2 id="member-profile-title">{{ currentUser.nickname }}</h2>
                <p>{{ currentUser.account }}</p>
              </div>
            </div>
            <dl class="member-details">
              <div>
                <dt>使用者編號</dt>
                <dd>{{ currentUser.id }}</dd>
              </div>
              <div>
                <dt>Email</dt>
                <dd>{{ currentUser.email }}</dd>
              </div>
              <div>
                <dt>電話</dt>
                <dd>{{ currentUser.phone || '尚未填寫' }}</dd>
              </div>
              <div>
                <dt>點數</dt>
                <dd>{{ currentUser.pointBalance }}</dd>
              </div>
            </dl>

            <!-- 兩顆都是按鈕：點擊後開啟對應彈窗，不再換頁 -->
            <div class="member-actions">
              <button
                class="comic-button member-primary-action"
                type="button"
                aria-haspopup="dialog"
                @click="openProfileDialog"
              >
                修改使用者資料
              </button>

              <button
                class="comic-button member-secondary-action"
                type="button"
                aria-haspopup="dialog"
                @click="openPasswordDialog"
              >
                修改密碼
              </button>
            </div>
          </section>
        </template>
        <p v-else class="member-state">請先登入</p>
      </div>
    </main>

    <!-- ------修改會員資料：彈出視窗------ -->
    <dialog
      v-if="isLoggedIn"
      ref="profileDialog"
      class="member-dialog"
      aria-labelledby="member-edit-title"
      @cancel.prevent="closeProfileDialog"
      @click.self="closeProfileDialog"
    >
      <section class="member-dialog-window member-dialog-window--profile">
        <h2 id="member-edit-title">修改使用者資料</h2>

        <form @submit.prevent="submitProfile">
          <div class="member-field">
            <label for="member-nickname">暱稱</label>
            <div class="member-field-control">
              <input
                id="member-nickname"
                v-model.trim="profileForm.nickname"
                type="text"
                maxlength="50"
                autocomplete="nickname"
                required
              />
            </div>
          </div>

          <div class="member-field">
            <label for="member-email">Email</label>
            <div class="member-field-control">
              <input
                id="member-email"
                v-model.trim="profileForm.email"
                type="email"
                maxlength="255"
                autocomplete="email"
                required
              />
            </div>
          </div>

          <div class="member-field">
            <label for="member-phone">電話（選填）</label>
            <div class="member-field-control">
              <input
                id="member-phone"
                v-model.trim="profileForm.phone"
                type="tel"
                inputmode="numeric"
                pattern="[0-9]{10}"
                maxlength="10"
                autocomplete="tel"
                placeholder="例如：0912345678"
                aria-describedby="member-phone-help"
              />
              <p id="member-phone-help" class="member-field-help">請輸入 10 位數字。</p>
            </div>
          </div>

          <div class="member-field member-field--wide">
            <label for="member-biography">簡介（選填）</label>
            <textarea
              id="member-biography"
              v-model.trim="profileForm.biography"
              maxlength="500"
              placeholder="例如：平日在 3 樓辦公室，週五遠端。"
            ></textarea>
          </div>

          <p v-if="profileErrorMessage" class="error" role="alert">
            {{ profileErrorMessage }}
          </p>

          <div class="member-dialog-actions">
            <button
              class="comic-button member-dialog-button member-dialog-button--secondary"
              type="button"
              :disabled="isSavingProfile"
              @click="closeProfileDialog"
            >
              取消
            </button>
            <button
              class="comic-button member-dialog-button member-dialog-button--primary"
              type="submit"
              :disabled="isSavingProfile"
            >
              {{ isSavingProfile ? '儲存中...' : '儲存修改' }}
            </button>
          </div>
        </form>
      </section>
    </dialog>

    <!-- ------修改密碼：彈出視窗------ -->
    <dialog
      v-if="isLoggedIn"
      ref="passwordDialog"
      class="member-dialog"
      aria-labelledby="member-password-title"
      @cancel.prevent="closePasswordDialog"
      @click.self="closePasswordDialog"
    >
      <section class="member-dialog-window">
        <h2 id="member-password-title">修改密碼</h2>

        <form @submit.prevent="submitPassword">
          <div class="member-field">
            <label for="member-current-password">目前密碼</label>
            <div class="member-field-control">
              <input
                id="member-current-password"
                v-model="passwordForm.currentPassword"
                type="password"
                autocomplete="current-password"
                required
              />
            </div>
          </div>

          <div class="member-field">
            <label for="member-new-password">新密碼</label>
            <div class="member-field-control">
              <input
                id="member-new-password"
                v-model="passwordForm.newPassword"
                type="password"
                autocomplete="new-password"
                aria-describedby="member-new-password-help"
                required
              />
              <p id="member-new-password-help" class="member-field-help">新密碼至少需要 6 個字。</p>
            </div>
          </div>

          <div class="member-field">
            <label for="member-confirm-password">確認新密碼</label>
            <div class="member-field-control">
              <input
                id="member-confirm-password"
                v-model="passwordForm.confirmNewPassword"
                type="password"
                autocomplete="new-password"
                required
              />
            </div>
          </div>

          <p v-if="passwordErrorMessage" class="error" role="alert">
            {{ passwordErrorMessage }}
          </p>

          <div class="member-dialog-actions">
            <button
              class="comic-button member-dialog-button member-dialog-button--secondary"
              type="button"
              :disabled="isSavingPassword"
              @click="closePasswordDialog"
            >
              取消
            </button>
            <button
              class="comic-button member-dialog-button member-dialog-button--primary"
              type="submit"
              :disabled="isSavingPassword"
            >
              {{ isSavingPassword ? '修改中...' : '修改密碼' }}
            </button>
          </div>
        </form>
      </section>
    </dialog>
  </div>
</template>
