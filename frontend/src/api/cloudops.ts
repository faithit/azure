import { api } from './client'
import type { AuthResponse, Dashboard, PagedResult, Project, ProjectMember, ProjectTask, ProjectPriority, ProjectStatus, TaskPriority, TaskStatus } from '../types'

export const cloudops = {
  login: (email: string, password: string) => api<AuthResponse>('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  register: (displayName: string, email: string, password: string) => api<AuthResponse>('/auth/register', { method: 'POST', body: JSON.stringify({ displayName, email, password }) }),
  dashboard: () => api<Dashboard>('/dashboard'),
  projects: async () => {
    const page = await api<PagedResult<Project>>('/projects')
    return page.items ?? []
  },
  project: (id: string) => api<Project>(`/projects/${id}`),
  createProject: (payload: {
    name: string
    projectCode?: string
    description?: string
    status?: ProjectStatus
    priority?: ProjectPriority
    startDateUtc?: string
    dueDateUtc?: string
    projectManagerId?: string
  }) => api<Project>('/projects', { method: 'POST', body: JSON.stringify(payload) }),
  updateProject: (id: string, payload: {
    name: string
    projectCode?: string
    description?: string
    status?: ProjectStatus
    priority?: ProjectPriority
    startDateUtc?: string
    dueDateUtc?: string
    projectManagerId?: string
  }) => api<Project>(`/projects/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  deleteProject: (id: string) => api<void>(`/projects/${id}`, { method: 'DELETE' }),
  projectMembers: async (projectId: string) => {
    const members = await api<ProjectMember[]>(`/projects/${projectId}/members`)
    return members ?? []
  },
  addProjectMember: (projectId: string, userId: string) => api<ProjectMember>(`/projects/${projectId}/members`, { method: 'POST', body: JSON.stringify({ userId }) }),
  removeProjectMember: (projectId: string, userId: string) => api<void>(`/projects/${projectId}/members/${userId}`, { method: 'DELETE' }),
  tasks: async (projectId: string) => {
    const page = await api<PagedResult<ProjectTask>>(`/projects/${projectId}/tasks`)
    return page.items ?? []
  },
  createTask: (projectId: string, payload: {
    title: string
    description?: string
    status?: TaskStatus
    priority?: TaskPriority
    assignedUserId?: string
    dueDateUtc?: string
  }) => api<ProjectTask>(`/projects/${projectId}/tasks`, { method: 'POST', body: JSON.stringify(payload) }),
  updateTask: (task: ProjectTask, payload: Partial<ProjectTask>) => api<ProjectTask>(`/tasks/${task.id}`, { method: 'PUT', body: JSON.stringify({
    title: payload.title ?? task.title,
    description: payload.description ?? task.description,
    status: payload.status ?? task.status,
    priority: payload.priority ?? task.priority,
    dueDateUtc: payload.dueDateUtc ?? task.dueDateUtc,
    assignedUserId: payload.assignedUserId ?? task.assignedUserId,
    createdById: payload.createdById ?? task.createdById,
  }) }),
  deleteTask: (taskId: string) => api<void>(`/tasks/${taskId}`, { method: 'DELETE' })
}
