<script setup>
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

const { currentUser, authInitialized, isLoggedIn, logout } = useAuth()

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
        <section v-else-if="isLoggedIn" class="member-card" aria-labelledby="member-profile-title">
          <div class="member-card-heading">
            <img :src="dashboardProfile" alt="" aria-hidden="true" />
            <div>
              <h2 id="member-profile-title">{{ currentUser.nickname }}</h2>
              <p>{{ currentUser.account }}</p>
            </div>
          </div>
          <dl class="member-details">
            <div>
              <dt>會員編號</dt>
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









          
          <div class="member-actions">
            <RouterLink class="comic-button member-primary-action" to="/member/edit"
              >修改會員資料</RouterLink
            >







            
            <RouterLink class="comic-button member-secondary-action" to="/member/password"
              >修改密碼</RouterLink
            >
          </div>
        </section>
        <p v-else class="member-state">請先登入</p>
      </div>



      
    </main>
  </div>
</template>
