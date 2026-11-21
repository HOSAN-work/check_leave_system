<!-- 資料與功能 -->
<script setup>
defineEmits(['done'])
import '../assets/login.css'
</script>

<!-- 畫面 -->
<template>
  <main>
    <div class="intro">
      <video
        ref="videoEl"
        src="/login_video.mp4"
        autoplay
        muted
        playsinline
        @ended="$emit('done')"
      />
      <button class="login-button drip-button" type="button" @click="$emit('done')">跳過 ▶</button>
    </div>
  </main>
</template>

<!-- 樣式 -->
<style scoped>
.intro {
  position: fixed;
  inset: 0;
  background: #000;
  z-index: 9999;
}
.intro video {
  width: 100%;
  height: 100%;
  object-fit: contain;
}
</style>
