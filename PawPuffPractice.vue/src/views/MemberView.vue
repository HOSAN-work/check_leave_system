<script setup>
import {useAuth} from '@/composables/useAuth'
import { RouterLink } from 'vue-router';

const{
    currentUser,
    authInitialized,
    isLoggedIn
} = useAuth()

</script>

<template>
<main>
    <h1>會員中心</h1>

    <!-- 有沒有向後端確認登入狀態 -->
    <!-- true => !true = false => 判斷下面的 v-else-if 和 v-else  -->
    <!-- false => !false =true => 正在確認登入狀態 
    下面的 v-else-if 和 v-else 都不再判斷 -->
    <p v-if="!authInitialized">
        正在確認登入狀態...
    </p>

    <!-- 有向後端確認登入狀態 => isLoggedIn = true -->
    <section v-else-if="isLoggedIn">
        <p>會員編號：{{ currentUser.id }}</p>
        <p>帳號：{{currentUser.account }}</p>
        <p>暱稱：{{ currentUser.nickname }}</p>
        <p>Email：{{ currentUser.email }}</p>
        <p>電話：{{ currentUser.phone || '尚未填寫' }}</p>
        <p>點數：{{ currentUser.pointBalance }}</p>

        <RouterLink to="/member/edit">
            修改會員資料
        </RouterLink>
        <!-- 或者 :to="{ name: 'change-password' }" -->
        <!-- : 是v-bind:的簡寫 -->
        <RouterLink to="/member/password">
            修改密碼
        </RouterLink>
    </section>

    <!-- 登入狀態已確認完成而且使用者沒有登入 -->
    <!-- 如果前面的條件全部不成立，就顯示這一段 -->
    <p v-else >請先登入</p>


    <!-- 將它理解成 JavaScript：
    if (!authInitialized) {
        顯示「正在確認登入狀態」
} else if (isLoggedIn) {
        顯示會員內容
} else {
        顯示「請先登入」
} -->

</main>
</template>

<style scoped>
</style>