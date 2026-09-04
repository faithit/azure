import type { ProjectMember, User } from '../types'

export const hasRole = (user: User | null | undefined, role: string) => (user?.roles ?? []).includes(role)

export const canManageProjects = (user: User | null | undefined) => hasRole(user, 'Admin') || hasRole(user, 'ProjectManager')

export const canManageProject = (user: User | null | undefined, project?: { projectManagerId?: string; createdById?: string } | null) => {
  if (!user) return false
  if (hasRole(user, 'Admin')) return true
  if (!project) return false
  if (hasRole(user, 'ProjectManager') && project.projectManagerId === user.id) return true
  return false
}

export const canViewProject = (user: User | null | undefined, project?: { projectManagerId?: string; createdById?: string } | null, members?: ProjectMember[] | null) => {
  if (!user) return false
  if (hasRole(user, 'Admin')) return true
  if (!project) return false
  if (hasRole(user, 'ProjectManager') && project.projectManagerId === user.id) return true
  return members?.some((member) => member.userId === user.id) ?? false
}

export const canCreateTasks = (user: User | null | undefined, project?: { projectManagerId?: string; createdById?: string } | null, members?: ProjectMember[] | null) => {
  if (!user) return false
  if (hasRole(user, 'Admin')) return true
  if (hasRole(user, 'ProjectManager') && project?.projectManagerId === user.id) return true
  if (hasRole(user, 'Developer')) return members?.some((member) => member.userId === user.id) ?? false
  return false
}

export const canUpdateTask = (
  user: User | null | undefined,
  task?: { assignedUserId?: string } | null,
  project?: { projectManagerId?: string } | null,
) => {
  if (!user) return false
  if (hasRole(user, 'Admin')) return true
  if (hasRole(user, 'ProjectManager') && project?.projectManagerId === user.id) return true
  if (hasRole(user, 'Developer')) return task?.assignedUserId === user.id
  return false
}

export const canDeleteTask = (
  user: User | null | undefined,
  task?: { assignedUserId?: string } | null,
  project?: { projectManagerId?: string } | null,
) => canUpdateTask(user, task, project)
