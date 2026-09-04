import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
export function ProtectedRoute() { return useAuth().authenticated ? <Outlet /> : <Navigate to="/login" replace /> }
