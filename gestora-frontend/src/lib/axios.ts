 import axios from 'axios'

  const apiClient = axios.create({
    baseURL: import.meta.env.VITE_API_URL,
  })

  apiClient.interceptors.request.use((config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  })

  apiClient.interceptors.response.use(
    (response) => response,
    (error) => {
      // Il redirect automatico serve per la sessione scaduta (token presente ma rifiutato),
      // non per un 401 su una richiesta anonima come il login stesso: altrimenti una password
      // sbagliata provoca un reload a pagina intera invece di mostrare l'errore nel form.
      const hadToken = Boolean(error.config?.headers?.Authorization)
      if (error.response?.status === 401 && hadToken) {
        localStorage.removeItem('token')
        window.location.href = '/login'
      }
      return Promise.reject(error)
    }
  )

  export default apiClient