import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig(({ mode }) => {
  const environment = loadEnv(mode, '.', '')

  if (mode === 'production' && !environment.VITE_API_URL) {
    throw new Error('VITE_API_URL is required for a production build.')
  }

  return { plugins: [react()], server: { port: 5173 } }
})
