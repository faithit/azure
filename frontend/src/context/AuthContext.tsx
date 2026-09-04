import { createContext, useContext, useMemo, useState } from 'react'
import type { AuthResponse, User } from '../types'

interface AuthContextValue { user: User | null; authenticated: boolean; acceptAuth: (response: AuthResponse) => void; signOut: () => void }
const AuthContext = createContext<AuthContextValue | undefined>(undefined)
const storedUser = () => { try { return JSON.parse(localStorage.getItem('cloudops.user') ?? 'null') as User | null } catch { return null } }

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(storedUser)
  const value = useMemo<AuthContextValue>(() => ({ user, authenticated: Boolean(user && localStorage.getItem('cloudops.accessToken')), acceptAuth: (response) => { localStorage.setItem('cloudops.accessToken', response.accessToken); localStorage.setItem('cloudops.user', JSON.stringify(response.user)); setUser(response.user) }, signOut: () => { localStorage.removeItem('cloudops.accessToken'); localStorage.removeItem('cloudops.user'); setUser(null) } }), [user])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
export function useAuth() { const context = useContext(AuthContext); if (!context) throw new Error('useAuth must be used within AuthProvider'); return context }
