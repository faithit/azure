import { FormEvent, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { cloudops } from '../api/cloudops'
import { useAuth } from '../context/AuthContext'

export function AuthPage({ mode }: { mode: 'login' | 'register' }) {
  const [displayName, setDisplayName] = useState(''); const [email, setEmail] = useState(''); const [password, setPassword] = useState(''); const [error, setError] = useState(''); const [loading, setLoading] = useState(false); const { acceptAuth } = useAuth(); const navigate = useNavigate(); const registering = mode === 'register'

  async function useDemoAdmin() {
    if (registering) return
    setEmail('admin@cloudops.local')
    setPassword('AdminPass123!')
    setError('')
    setLoading(true)
    try {
      const session = await cloudops.login('admin@cloudops.local', 'AdminPass123!')
      acceptAuth(session)
      navigate('/')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to continue.')
    } finally {
      setLoading(false)
    }
  }

  async function submit(event: FormEvent) { event.preventDefault(); setLoading(true); setError(''); try { acceptAuth(registering ? await cloudops.register(displayName, email, password) : await cloudops.login(email, password)); navigate('/') } catch (reason) { setError(reason instanceof Error ? reason.message : 'Unable to continue.') } finally { setLoading(false) } }
  return <div className="auth-page"><form className="auth-card" onSubmit={submit}><div className="eyebrow">CLOUDOPS AI PLATFORM</div><h1>{registering ? 'Create your workspace' : 'Welcome back'}</h1><p>{registering ? 'Start organizing cloud delivery work.' : 'Sign in to your delivery dashboard.'}</p>{registering && <label>Name<input required minLength={2} value={displayName} onChange={(e) => setDisplayName(e.target.value)} /></label>}<label>Email<input required type="email" value={email} onChange={(e) => setEmail(e.target.value)} /></label><label>Password<input required minLength={8} type="password" value={password} onChange={(e) => setPassword(e.target.value)} /></label>{error && <p className="error">{error}</p>}<button disabled={loading}>{loading ? 'Working…' : registering ? 'Create account' : 'Sign in'}</button>{!registering && <button type="button" className="secondary" onClick={useDemoAdmin} disabled={loading}>Use admin demo login</button>}<p className="auth-switch">{registering ? 'Already have an account?' : 'New to CloudOps?'} <Link to={registering ? '/login' : '/register'}>{registering ? 'Sign in' : 'Create one'}</Link></p></form></div>
}
