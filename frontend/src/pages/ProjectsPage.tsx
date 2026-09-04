import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { cloudops } from '../api/cloudops'
import { useAuth } from '../context/AuthContext'
import type { Project, ProjectPriority, ProjectStatus } from '../types'
import { canManageProjects } from '../utils/permissions'
import { Loading, PageError } from './DashboardPage'

const statuses: ProjectStatus[] = ['Planning', 'Active', 'OnHold', 'Completed', 'Cancelled']
const priorities: ProjectPriority[] = ['Low', 'Medium', 'High', 'Critical']

const normalizeStatus = (value?: string) => (value && ['Planning', 'Active', 'OnHold', 'Completed', 'Cancelled'].includes(value) ? value : 'Planning') as ProjectStatus
const normalizePriority = (value?: string) => (value && ['Low', 'Medium', 'High', 'Critical'].includes(value) ? value : 'Medium') as ProjectPriority

export function ProjectsPage() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const [projects, setProjects] = useState<Project[] | null>(null)
  const [query, setQuery] = useState('')
  const [statusFilter, setStatusFilter] = useState<'All' | ProjectStatus>('All')
  const [priorityFilter, setPriorityFilter] = useState<'All' | ProjectPriority>('All')
  const [error, setError] = useState('')

  const load = async () => {
   try {
     const nextProjects = await cloudops.projects()
     setProjects(nextProjects)
   } catch (reason) {
     setError(reason instanceof Error ? reason.message : 'Unable to load projects.')
   }
  }

  useEffect(() => { void load() }, [])

  const filteredProjects = useMemo(() => {
   const normalized = query.trim().toLowerCase()
   return (projects ?? []).filter((project) => {
     const projectStatus = normalizeStatus(project.status)
     const projectPriority = normalizePriority(project.priority)
     const matchesQuery = !normalized || [project.name, project.projectCode ?? '', project.description ?? ''].some((value) => value.toLowerCase().includes(normalized))
     const matchesStatus = statusFilter === 'All' || projectStatus === statusFilter
     const matchesPriority = priorityFilter === 'All' || projectPriority === priorityFilter
     return matchesQuery && matchesStatus && matchesPriority
   })
  }, [projects, query, statusFilter, priorityFilter])

  const canCreateProject = canManageProjects(user)

  if (error) return <PageError message={error} />
  if (!projects) return <Loading />

  return (
   <section className="page-shell">
     <div className="page-heading">
       <div>
         <div className="eyebrow">PORTFOLIO</div>
         <h1>Projects</h1>
         <p>Track the initiatives your teams are delivering.</p>
       </div>
       {canCreateProject && <button onClick={() => navigate('/projects/new')}>+ Create project</button>}
     </div>

     <div className="panel filters-panel">
       <div className="filter-row">
         <label>
           Search projects
           <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search by name or code" />
         </label>
         <label>
           Status
           <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value as 'All' | ProjectStatus)}>
             <option value="All">All statuses</option>
             {statuses.map((status) => <option key={status} value={status}>{status}</option>)}
           </select>
         </label>
         <label>
           Priority
           <select value={priorityFilter} onChange={(event) => setPriorityFilter(event.target.value as 'All' | ProjectPriority)}>
             <option value="All">All priorities</option>
             {priorities.map((priority) => <option key={priority} value={priority}>{priority}</option>)}
           </select>
         </label>
       </div>
     </div>

     <div className="project-grid">
       {filteredProjects.map((project) => {
         const projectStatus = normalizeStatus(project.status)
         const projectPriority = normalizePriority(project.priority)
         const totalTasks = project.taskCount ?? 0
         const progress = totalTasks === 0 ? 0 : 100
         return (
           <Link className="project-card" to={`/projects/${project.id}`} key={project.id}>
             <div>
               <div className="project-card-header">
                 <span className="project-mark">◈</span>
                 <span className="badge badge-status">{projectStatus}</span>
               </div>
               <h2>{project.name}</h2>
               <p>{project.description || 'No project description provided yet.'}</p>
             </div>

             <div className="project-meta-grid">
               <span><strong>{project.projectCode ?? 'No code'}</strong></span>
               <span className={`badge badge-priority priority-${projectPriority.toLowerCase()}`}>{projectPriority}</span>
             </div>

             <div className="project-metrics">
               <span>Manager: {project.projectManagerId ?? project.createdById ?? 'Unassigned'}</span>
               <span>Due: {project.dueDateUtc ? new Date(project.dueDateUtc).toLocaleDateString() : 'No due date'}</span>
               <span>{totalTasks} tasks</span>
             </div>

             <div className="mini-progress">
               <div className="progress"><i style={{ width: `${progress}%` }} /></div>
               <small>{progress}% complete</small>
             </div>

             <footer>
               <span>{project.memberCount ?? 0} members</span>
               <span>View project →</span>
             </footer>
           </Link>
         )
       })}

       {filteredProjects.length === 0 && <div className="panel empty">No projects match your filters yet.</div>}
     </div>
   </section>
  )
}
