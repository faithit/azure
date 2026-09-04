import { useAuth } from '../context/AuthContext'

export function ProfilePage() {
  const { user } = useAuth()

  if (!user) return null

  return <section><div className="page-heading"><div><div className="eyebrow">ACCOUNT</div><h1>Your profile</h1><p>Manage the identity connected to this workspace.</p></div></div><div className="profile-layout"><div className="panel profile-summary"><div className="avatar">{user.displayName.slice(0, 1).toUpperCase()}</div><h2>{user.displayName}</h2><p>{user.email}</p><div className="role-list">{user.roles.map((role) => <span className="role-badge" key={role}>{role}</span>)}</div></div><div className="panel profile-details"><h2>Account details</h2><dl><div><dt>Display name</dt><dd>{user.displayName}</dd></div><div><dt>Email address</dt><dd>{user.email}</dd></div><div><dt>Access level</dt><dd>{user.roles.join(', ')}</dd></div><div><dt>User ID</dt><dd className="profile-id">{user.id}</dd></div></dl></div></div></section>
}