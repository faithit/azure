export type TaskStatus = 'ToDo' | 'InProgress' | 'Blocked' | 'Completed' | 'InReview' | 'Done'
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical'
export type ProjectStatus = 'Planning' | 'Active' | 'OnHold' | 'Completed' | 'Cancelled'
export type ProjectPriority = 'Low' | 'Medium' | 'High' | 'Critical'

export interface User { id: string; displayName: string; email: string; roles: string[] }
export interface AuthResponse { accessToken: string; expiresAtUtc: string; user: User }
export interface PagedResult<T> { items: T[]; totalCount: number; pageNumber: number; pageSize: number }
export interface ProjectMember { id: string; projectId: string; userId: string; joinedAtUtc: string }
export interface Project {
  id: string
  name: string
  description?: string
  projectCode: string
  status: ProjectStatus
  priority: ProjectPriority
  startDateUtc?: string
  dueDateUtc?: string
  projectManagerId?: string
  createdById: string
  ownerId: string
  createdAtUtc: string
  updatedAtUtc?: string
  taskCount: number
  memberCount: number
}
export interface ProjectTask {
  id: string
  projectId: string
  title: string
  description?: string
  status: TaskStatus
  priority: TaskPriority
  dueDateUtc?: string
  assignedUserId?: string
  createdById?: string
  createdAtUtc: string
  updatedAtUtc?: string
  completedAtUtc?: string
}
export interface Dashboard { projectCount: number; taskCount: number; completedTaskCount: number; overdueTaskCount: number; tasksByStatus: Record<TaskStatus, number> }
export interface AiSource { projectId: string; projectName: string; taskCount: number }
export interface AiChatResponse { answer: string; provider: string; generatedAtUtc: string; sources: AiSource[] }
