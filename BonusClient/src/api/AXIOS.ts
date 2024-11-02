import { useAuth } from "@Zustand/useAuth";
import axios, { AxiosError } from "axios";

const baseURL =
  import.meta.env.VITE_API_URL ||
  `${window.location.protocol}//${window.location.hostname}:${window.location.port}`;

// 創建一個 axios 實例並設定 baseURL
const AXIOS = axios.create({
  baseURL,
  timeout: 15000,
});

// 添加請求攔截器
AXIOS.interceptors.request.use(
  (config) => {
    const { token } = useAuth.getState();
    if (config.headers && token)
      config.headers.Authorization = `Bearer ${token}`;
    return config;
  },
  (err: AxiosError) => Promise.reject(err)
);

// 添加響應攔截器
AXIOS.interceptors.response.use(
  function (response) {
    // 對響應數據做點什麼
    return response;
  },
  function (error) {
    // 對響應錯誤做點什麼
    return Promise.reject(error);
  }
);

export default AXIOS;
