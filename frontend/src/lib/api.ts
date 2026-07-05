import axios from 'axios'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.response.use(
  (res) => res,
  (error) => {
    const message =
      error.response?.data?.detail ??
      error.response?.data?.title ??
      'Erro inesperado. Tente novamente.'
    return Promise.reject(new Error(message))
  },
)

export default api
