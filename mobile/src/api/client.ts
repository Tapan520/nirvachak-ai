import axios from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';

// ?? API Base URL ???????????????????????????????????????????????????????????????
//  PRODUCTION  (Railway)  — currently active
export const API_BASE_URL = 'https://nirvachakai.up.railway.app';

//  LOCAL DEVELOPMENT  (uncomment one when testing locally)
//  Android emulator  ? http://10.0.2.2:5211
//  iOS simulator     ? http://localhost:5211
//  Physical device   ? http://192.168.29.24:5211  (your machine LAN IP)
// export const API_BASE_URL = 'http://10.0.2.2:5211';

const apiClient = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
});

apiClient.interceptors.request.use(async (config) => {
  const token = await AsyncStorage.getItem('auth_token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

apiClient.interceptors.response.use(
  (res) => res,
  async (error) => {
    if (error.response?.status === 401) {
      await AsyncStorage.multiRemove(['auth_token', 'auth_user']);
    }
    return Promise.reject(error);
  }
);

export default apiClient;
