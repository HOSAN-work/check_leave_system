// 建立共用 Axios

import axios from "axios";

//建立了一個已經設定好的 Axios 物件，名稱叫做：api
//它已經記住：後端網址、要攜帶 Cookie、逾時時間
const api = axios.create({
    baseURL:import.meta.env.VITE_API_BASE_URL ,

    withCredentials: true
});

//如果只有：const api = axios.create(...)
//這個 api 只能在 api.js 裡使用

// 把這個檔案裡建立好的 api 提供給其他檔案使用。
// 把它從 api.js 對外提供
export default api;

//之後所有會員 API 都使用這個 api：
//import api from '@/services/api'
//
//註冊：
// api.post('/api/Auth/register', form)
//
//登入：
// api.post('/api/Auth/login', form)
//
//取得登入會員：
// api.get('/api/Auth/me')
//
//登出：
// api.post('/api/Auth/logout')
//
//不需要每次重複寫：withCredentials: true
//因為已經在 axios.create() 裡統一設定