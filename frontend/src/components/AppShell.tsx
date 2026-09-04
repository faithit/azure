import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function AppShell() {
  const { user, signOut } = useAuth();
  const navigate = useNavigate();

  return (
    <div className="app-shell">
      <aside>
        <NavLink className="brand" to="/">
          <span>◈</span> CloudOps
        </NavLink>

        <nav>
          <NavLink to="/">Dashboard</NavLink>
          <NavLink to="/projects">Projects</NavLink>
          <NavLink to="/profile">Profile</NavLink>
        </nav>

        <div className="profile">
          <NavLink to="/profile">
            <strong>{user?.displayName}</strong>
            <small>{user?.roles.join(' · ')}</small>
          </NavLink>
          <button
            className="link-button"
            onClick={() => {
              signOut();
              navigate('/login');
            }}
          >
            Sign out
          </button>
        </div>
      </aside>

      <main>
        <Outlet />
      </main>
    </div>
  )
}
