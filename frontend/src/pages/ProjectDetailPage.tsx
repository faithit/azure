import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { cloudops } from '../api/cloudops'
import { useAuth } from '../context/AuthContext'
import type { Project, ProjectMember, ProjectTask, TaskPriority, TaskStatus } from '../types'
import { canCreateTasks, canDeleteTask, canManageProject, canUpdateTask } from '../utils/permissions'
import { Loading, PageError } from './DashboardPage'

const taskStatuses: TaskStatus[] = ['ToDo', 'InProgress', 'Blocked', 'Completed']
const taskPriorities: TaskPriority[] = ['Low', 'Medium', 'High', 'Critical']
const normalizeProjectStatus = (value?: string) => (['Planning', 'Active', 'OnHold', 'Completed', 'Cancelled'].includes(value ?? '') ? value : 'Planning') as Project['status']
const normalizeProjectPriority = (value?: string) => (['Low', 'Medium', 'High', 'Critical'].includes(value ?? '') ? value : 'Medium') as Project['priority']
const normalizeTaskPriority = (value?: string) => (['Low', 'Medium', 'High', 'Critical'].includes(value ?? '') ? value : 'Medium') as ProjectTask['priority']

export function ProjectDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const [project, setProject] = useState<Project | null>(null)
  const [tasks, setTasks] = useState<ProjectTask[] | null>(null)
  const [members, setMembers] = useState<ProjectMember[] | null>(null)
  const [error, setError] = useState('')
  const [showTaskForm, setShowTaskForm] = useState(false)
  const [showMemberForm, setShowMemberForm] = useState(false)
  const [taskForm, setTaskForm] = useState({ title: '', description: '', status: 'ToDo' as TaskStatus, priority: 'Medium' as TaskPriority, assignedUserId: '', dueDateUtc: '' })
  const [memberUserId, setMemberUserId] = useState('')
  const [taskSearch, setTaskSearch] = useState('')
  const [taskStatusFilter, setTaskStatusFilter] = useState<'All' | TaskStatus>('All')
  const [taskPriorityFilter, setTaskPriorityFilter] = useState<'All' | TaskPriority>('All')
  const [taskUserFilter, setTaskUserFilter] = useState('All')
  const [selectedTask, setSelectedTask] = useState<ProjectTask | null>(null)

  const load = async () => {
   try {
     const [projectData, taskData, memberData] = await Promise.all([
       cloudops.project(id),
       cloudops.tasks(id),
       cloudops.projectMembers(id)
     ])
     setProject(projectData)
     setTasks(taskData)
     setMembers(memberData)
   } catch (reason) {
     setError(reason instanceof Error ? reason.message : 'Unable to load project.')
   }
  }

  useEffect(() => { void load() }, [id])

  const filteredTasks = useMemo(() => {
   const query = taskSearch.trim().toLowerCase()
   return (tasks ?? []).filter((task) => {
     const matchesQuery = !query || [task.title, task.description ?? ''].some((value) => value.toLowerCase().includes(query))
     const matchesStatus = taskStatusFilter === 'All' || task.status === taskStatusFilter
     const matchesPriority = taskPriorityFilter === 'All' || task.priority === taskPriorityFilter
     const matchesUser = taskUserFilter === 'All' || task.assignedUserId === taskUserFilter
     return matchesQuery && matchesStatus && matchesPriority && matchesUser
   })
  }, [tasks, taskSearch, taskStatusFilter, taskPriorityFilter, taskUserFilter])

  const taskStats = useMemo(() => {
   const totals = { total: tasks?.length ?? 0, completed: 0, inProgress: 0, todo: 0, blocked: 0, overdue: 0 }
   const now = Date.now()
   for (const task of tasks ?? []) {
     if (task.status === 'Completed' || task.status === 'Done') totals.completed += 1
     if (task.status === 'InProgress') totals.inProgress += 1
     if (task.status === 'ToDo') totals.todo += 1
     if (task.status === 'Blocked') totals.blocked += 1
     if (task.dueDateUtc && new Date(task.dueDateUtc).getTime() < now && task.status !== 'Completed' && task.status !== 'Done') totals.overdue += 1
   }
   return totals
  }, [tasks])

  const projectStatus = project ? normalizeProjectStatus(project.status) : 'Planning'
  const projectPriority = project ? normalizeProjectPriority(project.priority) : 'Medium'
  const completionPercent = project && project.taskCount ? Math.min(100, Math.round((taskStats.completed / project.taskCount) * 100)) : 0

  const isAdmin = user?.roles.includes('Admin') ?? false
  const canEditProjectAccess = canManageProject(user, project)
  const canManageMembers = canEditProjectAccess
  const canAddTasks = canCreateTasks(user, project, members)
  const canDeleteSelectedTask = canDeleteTask(user, selectedTask, project)

  const submitTask = async (event: React.FormEvent) => {
   event.preventDefault()
   if (!project) return

   try {
     await cloudops.createTask(project.id, {
       title: taskForm.title,
       description: taskForm.description || undefined,
       status: taskForm.status,
       priority: taskForm.priority,
       assignedUserId: taskForm.assignedUserId || undefined,
       dueDateUtc: taskForm.dueDateUtc ? new Date(taskForm.dueDateUtc).toISOString() : undefined
     })
     setTaskForm({ title: '', description: '', status: 'ToDo', priority: 'Medium', assignedUserId: '', dueDateUtc: '' })
     setShowTaskForm(false)
     await load()
   } catch (reason) {
     setError(reason instanceof Error ? reason.message : 'Unable to create task.')
   }
  }

  const submitMember = async (event: React.FormEvent) => {
   event.preventDefault()
   if (!project || !memberUserId.trim()) return

   try {
     await cloudops.addProjectMember(project.id, memberUserId.trim())
     setMemberUserId('')
     setShowMemberForm(false)
     await load()
   } catch (reason) {
     setError(reason instanceof Error ? reason.message : 'Unable to add project member.')
   }
  }

  const updateTaskStatus = async (task: ProjectTask, status: TaskStatus) => {
   try {
     await cloudops.updateTask(task, { status })
     await load()
   } catch (reason) {
     setError(reason instanceof Error ? reason.message : 'Unable to update task.')
   }
  }

  const removeMember = async (userId: string) => {
   if (!project) return
   try {
     await cloudops.removeProjectMember(project.id, userId)
     await load()
   } catch (reason) {
     setError(reason instanceof Error ? reason.message : 'Unable to remove member.')
   }
  }

  const removeTask = async (task: ProjectTask) => {
   try {
     await cloudops.deleteTask(task.id)
     await load()
     setSelectedTask(null)
   } catch (reason) {
     setError(reason instanceof Error ? reason.message : 'Unable to delete task.')
   }
  }

  if (error) return <PageError message={error} />
  if (!project || !tasks || !members) return <Loading />

  return (
   <section className="page-shell">
     <Link className="back" to="/projects">← Back to projects</Link>

     <div className="page-heading detail-heading">
       <div>
         <div className="eyebrow">PROJECT</div>
         <h1>{project.name}</h1>
         <p>{project.description || 'No project description provided.'}</p>
       </div>
       <div className="header-actions">
         <span className="badge badge-status">{projectStatus}</span>
         <span className={`badge badge-priority priority-${projectPriority.toLowerCase()}`}>{projectPriority}</span>
         {canEditProjectAccess && <button className="button-secondary" onClick={() => navigate(`/projects/${project.id}/edit`)}>Edit</button>}
         {isAdmin && (
           <button
             className="button-danger"
             type="button"
             onClick={() => {
               if (window.confirm('Are you sure you want to delete this project? This action may remove associated project data.')) {
                 void cloudops.deleteProject(project.id).then(() => navigate('/projects'))
               }
             }}
           >
             Delete
           </button>
         )}
       </div>
     </div>

     <div className="project-summary-grid panel">
       <div><span>Project code</span><strong>{project.projectCode ?? 'N/A'}</strong></div>
       <div><span>Manager</span><strong>{project.projectManagerId ?? project.createdById ?? 'Unassigned'}</strong></div>
       <div><span>Created</span><strong>{new Date(project.createdAtUtc).toLocaleDateString()}</strong></div>
       <div><span>Start</span><strong>{project.startDateUtc ? new Date(project.startDateUtc).toLocaleDateString() : '—'}</strong></div>
       <div><span>Due</span><strong>{project.dueDateUtc ? new Date(project.dueDateUtc).toLocaleDateString() : '—'}</strong></div>
       <div><span>Updated</span><strong>{project.updatedAtUtc ? new Date(project.updatedAtUtc).toLocaleDateString() : '—'}</strong></div>
     </div>

     <div className="project-progress panel">
       <div className="progress-header"><h2>Project progress</h2><strong>{completionPercent}%</strong></div>
       <div className="progress"><i style={{ width: `${completionPercent}%` }} /></div>
       <div className="stats-row">
         <span>{project.taskCount} total</span>
         <span>{taskStats.completed} completed</span>
         <span>{taskStats.inProgress} in progress</span>
         <span>{taskStats.todo} todo</span>
         <span>{taskStats.blocked} blocked</span>
         <span>{taskStats.overdue} overdue</span>
       </div>
     </div>

     <div className="project-panels">
       <section className="panel">
         <div className="section-header">
           <h2>Project members</h2>
           {canManageMembers && <button className="button-secondary" onClick={() => setShowMemberForm((current) => !current)}>Add member</button>}
         </div>

         {showMemberForm && (
           <form className="inline-form compact-form" onSubmit={submitMember}>
             <input value={memberUserId} onChange={(event) => setMemberUserId(event.target.value)} placeholder="Enter user ID or email" />
             <button type="submit">Add</button>
           </form>
         )}

         <div className="member-list">
           {members.map((member) => (
             <div key={member.id} className="member-item">
               <div>
                 <strong>{member.userId}</strong>
                 <small>Joined {new Date(member.joinedAtUtc).toLocaleDateString()}</small>
               </div>
               {canManageMembers && <button className="button-danger" type="button" onClick={() => removeMember(member.userId)}>Remove</button>}
             </div>
           ))}
         </div>
       </section>

       <section className="panel">
         <div className="section-header">
           <h2>Tasks</h2>
           {canAddTasks && <button onClick={() => setShowTaskForm((current) => !current)}>+ Add task</button>}
         </div>

         {showTaskForm && (
           <form className="task-form" onSubmit={submitTask}>
             <input value={taskForm.title} onChange={(event) => setTaskForm((current) => ({ ...current, title: event.target.value }))} placeholder="Task title" required />
             <textarea value={taskForm.description} onChange={(event) => setTaskForm((current) => ({ ...current, description: event.target.value }))} placeholder="Description" rows={3} />
             <div className="task-form-grid">
               <select value={taskForm.status} onChange={(event) => setTaskForm((current) => ({ ...current, status: event.target.value as TaskStatus }))}>
                 {taskStatuses.map((status) => <option key={status} value={status}>{status}</option>)}
               </select>
               <select value={taskForm.priority} onChange={(event) => setTaskForm((current) => ({ ...current, priority: event.target.value as TaskPriority }))}>
                 {taskPriorities.map((priority) => <option key={priority} value={priority}>{priority}</option>)}
               </select>
                 <input value={taskForm.assignedUserId} onChange={(event) => setTaskForm((current) => ({ ...current, assignedUserId: event.target.value }))} placeholder="Assigned user ID" />
               <input type="date" value={taskForm.dueDateUtc} onChange={(event) => setTaskForm((current) => ({ ...current, dueDateUtc: event.target.value }))} />
             </div>
             <div className="form-actions"><button type="submit">Create task</button></div>
           </form>
         )}

         <div className="task-toolbar">
           <input value={taskSearch} onChange={(event) => setTaskSearch(event.target.value)} placeholder="Search tasks" />
           <select value={taskStatusFilter} onChange={(event) => setTaskStatusFilter(event.target.value as 'All' | TaskStatus)}>
             <option value="All">All statuses</option>
             {taskStatuses.map((status) => <option key={status} value={status}>{status}</option>)}
           </select>
           <select value={taskPriorityFilter} onChange={(event) => setTaskPriorityFilter(event.target.value as 'All' | TaskPriority)}>
             <option value="All">All priorities</option>
             {taskPriorities.map((priority) => <option key={priority} value={priority}>{priority}</option>)}
           </select>
           <select value={taskUserFilter} onChange={(event) => setTaskUserFilter(event.target.value)}>
             <option value="All">All assignees</option>
             {[...new Set(tasks.map((task) => task.assignedUserId).filter(Boolean))].map((userId) => <option key={userId} value={userId}>{userId}</option>)}
           </select>
         </div>

         <div className="task-list">
           {filteredTasks.map((task) => {
             const taskPriority = normalizeTaskPriority(task.priority)
             const taskStatus = task.status ?? 'Todo'
             const canUpdateThisTask = canUpdateTask(user, task, project)

             return (
               <article className="task-card" key={task.id}>
                 <div className="task-card-head">
                   <div>
                     <span className={`badge badge-priority priority-${taskPriority.toLowerCase()}`}>{taskPriority}</span>
                     <span className={`badge badge-status ${taskStatus === 'Completed' || taskStatus === 'Done' ? 'done' : ''}`}>{taskStatus}</span>
                   </div>
                   <button type="button" className="text-button" onClick={() => setSelectedTask(task)}>Details</button>
                 </div>
                 <h3>{task.title}</h3>
                 <p>{task.description || 'No description provided.'}</p>
                 <div className="task-meta">
                   <span>Assigned: {task.assignedUserId ?? 'Unassigned'}</span>
                   <span>Due: {task.dueDateUtc ? new Date(task.dueDateUtc).toLocaleDateString() : 'No date'}</span>
                 </div>
                 {canUpdateThisTask && (
                   <div className="task-footer">
                     <select value={taskStatus} onChange={(event) => void updateTaskStatus(task, event.target.value as TaskStatus)}>
                       {taskStatuses.map((status) => <option key={status} value={status}>{status}</option>)}
                     </select>
                   </div>
                 )}
               </article>
             )
           })}
         </div>
       </section>
     </div>

     {selectedTask && (
       <div className="modal-backdrop" onClick={() => setSelectedTask(null)}>
         <div className="modal-card" onClick={(event) => event.stopPropagation()}>
           <div className="section-header">
             <h2>{selectedTask.title}</h2>
             <button className="button-secondary" onClick={() => setSelectedTask(null)}>Close</button>
           </div>
           <p>{selectedTask.description || 'No description provided.'}</p>
           <div className="project-summary-grid small-grid">
             <div><span>Status</span><strong>{selectedTask.status ?? 'Todo'}</strong></div>
             <div><span>Priority</span><strong>{normalizeTaskPriority(selectedTask.priority)}</strong></div>
             <div><span>Assigned</span><strong>{selectedTask.assignedUserId ?? 'Unassigned'}</strong></div>
             <div><span>Due</span><strong>{selectedTask.dueDateUtc ? new Date(selectedTask.dueDateUtc).toLocaleDateString() : '—'}</strong></div>
           </div>
           {canDeleteSelectedTask && (
             <div className="form-actions">
               <button type="button" className="button-danger" onClick={() => void removeTask(selectedTask)}>Delete task</button>
             </div>
           )}
         </div>
       </div>
     )}
   </section>
  )
}
