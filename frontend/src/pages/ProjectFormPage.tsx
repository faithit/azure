import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { cloudops } from '../api/cloudops'
import { useAuth } from '../context/AuthContext'
import type { ProjectPriority, ProjectStatus } from '../types'
import { canManageProject, canManageProjects } from '../utils/permissions'
import { PageError } from './DashboardPage'

const projectStatuses: ProjectStatus[] = ['Planning', 'Active', 'OnHold', 'Completed', 'Cancelled']
const projectPriorities: ProjectPriority[] = ['Low', 'Medium', 'High', 'Critical']
const normalizeProjectStatus = (value?: string) => (projectStatuses.includes(value as ProjectStatus) ? value : 'Planning') as ProjectStatus
const normalizeProjectPriority = (value?: string) => (projectPriorities.includes(value as ProjectPriority) ? value : 'Medium') as ProjectPriority

export function ProjectFormPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const isEdit = Boolean(id)
  const canManageProjectAccess = canManageProjects(user)
  const [form, setForm] = useState({
    name: '',
    projectCode: '',
    description: '',
    status: 'Planning' as ProjectStatus,
    priority: 'Medium' as ProjectPriority,
    startDateUtc: '',
    dueDateUtc: '',
    projectManagerId: ''
  })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!id) return
    void (async () => {
      try {
        const project = await cloudops.project(id)
        if (isEdit && !canManageProject(user, project)) {
          setError('You do not have permission to manage this project.')
          return
        }
        setForm({
          name: project.name,
          projectCode: project.projectCode ?? '',
          description: project.description ?? '',
          status: normalizeProjectStatus(project.status),
          priority: normalizeProjectPriority(project.priority),
          startDateUtc: project.startDateUtc ? project.startDateUtc.slice(0, 10) : '',
          dueDateUtc: project.dueDateUtc ? project.dueDateUtc.slice(0, 10) : '',
          projectManagerId: project.projectManagerId ?? ''
        })
      } catch (reason) {
        setError(reason instanceof Error ? reason.message : 'Unable to load project.')
      }
    })()
  }, [id, isEdit, user])

  const canSubmit = useMemo(() => form.name.trim().length > 1 && form.projectCode.trim().length > 0, [form])

  if (!canManageProjectAccess) {
    return <PageError message="You do not have permission to manage projects." />
  }
  if (error) {
    return <PageError message={error} />
  }

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError('')

    if (!canSubmit) {
      setError('Project name and code are required.')
      return
    }

    if (form.startDateUtc && form.dueDateUtc && new Date(form.dueDateUtc) < new Date(form.startDateUtc)) {
      setError('Due date cannot be earlier than the start date.')
      return
    }

    setLoading(true)
    try {
      const payload = {
        name: form.name,
        projectCode: form.projectCode,
        description: form.description || undefined,
        status: form.status,
        priority: form.priority,
        startDateUtc: form.startDateUtc ? new Date(form.startDateUtc).toISOString() : undefined,
        dueDateUtc: form.dueDateUtc ? new Date(form.dueDateUtc).toISOString() : undefined,
        projectManagerId: form.projectManagerId || undefined
      }

      const result = isEdit
        ? await cloudops.updateProject(id!, payload)
        : await cloudops.createProject(payload)

      navigate(`/projects/${result.id}`)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to save the project.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <section className="page-shell">
      <div className="page-heading">
        <div>
          <div className="eyebrow">PROJECT</div>
          <h1>{isEdit ? 'Edit project' : 'Create project'}</h1>
          <p>{isEdit ? 'Update delivery details and dates.' : 'Set up a new initiative with a clear owner and timeline.'}</p>
        </div>
      </div>

      <form className="panel project-form" onSubmit={handleSubmit}>
        <div className="form-grid">
          <label>
            Project name
            <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Azure migration" required />
          </label>

          <label>
            Project code
            <input value={form.projectCode} onChange={(event) => setForm((current) => ({ ...current, projectCode: event.target.value }))} placeholder="AZURE-01" required />
          </label>

          <label className="full-width">
            Description
            <textarea value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} rows={4} placeholder="Describe the project goal and scope" />
          </label>

          <label>
            Project manager
            <input value={form.projectManagerId} onChange={(event) => setForm((current) => ({ ...current, projectManagerId: event.target.value }))} placeholder="User ID or manager email" />
          </label>

          <label>
            Status
            <select value={form.status} onChange={(event) => setForm((current) => ({ ...current, status: event.target.value as ProjectStatus }))}>
              {projectStatuses.map((status) => <option key={status} value={status}>{status}</option>)}
            </select>
          </label>

          <label>
            Priority
            <select value={form.priority} onChange={(event) => setForm((current) => ({ ...current, priority: event.target.value as ProjectPriority }))}>
              {projectPriorities.map((priority) => <option key={priority} value={priority}>{priority}</option>)}
            </select>
          </label>

          <label>
            Start date
            <input type="date" value={form.startDateUtc} onChange={(event) => setForm((current) => ({ ...current, startDateUtc: event.target.value }))} />
          </label>

          <label>
            Due date
            <input type="date" value={form.dueDateUtc} onChange={(event) => setForm((current) => ({ ...current, dueDateUtc: event.target.value }))} />
          </label>
        </div>

        {error && <p className="error-text">{error}</p>}

        <div className="form-actions">
          <button type="button" className="button-secondary" onClick={() => navigate(-1)}>Cancel</button>
          <button type="submit" disabled={loading || !canSubmit}>{loading ? 'Saving...' : isEdit ? 'Save project' : 'Create project'}</button>
        </div>
      </form>
    </section>
  )
}
