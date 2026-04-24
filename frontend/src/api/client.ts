import axios from 'axios'

export const apiClient = axios.create({
  baseURL: `${import.meta.env.VITE_API_URL ?? ''}/api`,
})

apiClient.interceptors.request.use((config) => {
  const raw = localStorage.getItem('lis_auth')
  if (raw) {
    const auth = JSON.parse(raw)
    config.headers.Authorization = `Bearer ${auth.token}`
    config.headers['X-Tenant-Code'] = auth.tenantCode
  }
  return config
})

apiClient.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('lis_auth')
      window.location.href = '/login'
    }
    return Promise.reject(err)
  },
)
