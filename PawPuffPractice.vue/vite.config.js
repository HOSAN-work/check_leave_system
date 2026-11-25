import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  // 告訴 Vite（你的前端開發伺服器）兩件事：
  // 「接受 ngrok 的網址」和「把 API 請求轉給後端」。
  server: {
    // Vite 預設只接受用 localhost 開的請求,
    // 「ngrok-free.dev 結尾的網域也放行」
  allowedHosts: ['.ngrok-free.dev'],
  // 設定轉發規則：
  // 只要請求的路徑是 /api 開頭，就照下面的規則處理。
  proxy: {
    '/api': {
      // 轉發的目的地，也就是你的後端
      target: 'http://localhost:5140',
      // 轉發時把請求的 Host 改成目的地的（localhost:5140），避免後端看到 ngrok 網址而拒絕。
      changeOrigin: true,
      // SignalR 優先使用 WebSocket；一併代理 WebSocket，避免握手逾時後才改用 SSE。
      ws: true,
      // 轉發前把路徑開頭的 /api 拿掉。舉例：
      // 前端呼叫：/api/Login/Auth
      // 轉給後端時變成：/Login/Auth
      // 這是因為你的後端路由本身沒有 /api 這一層，/api 只是前端用來讓 Vite 辨識「這是 API 請求」的標記。
      rewrite: (path) => path.replace(/^\/api/, ''),
    },
  },
},
})
