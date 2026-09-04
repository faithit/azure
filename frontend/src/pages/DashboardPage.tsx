import { useEffect, useMemo, useState } from 'react'
import { cloudops } from '../api/cloudops'
import type { Dashboard, Project } from '../types'

const labels: Record<string, string> = { Todo: 'To do', ToDo: 'To do', InProgress: 'In progress', InReview: 'In review', Done: 'Done', Completed: 'Completed', Blocked: 'Blocked' }

export function DashboardPage() {
  const [data, setData] = useState<Dashboard | null>(null)
  const [projects, setProjects] = useState<Project[] | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    void (async () => {
      try {
        const [dashboard, projectList] = await Promise.all([cloudops.dashboard(), cloudops.projects()])
        setData(dashboard)
        setProjects(projectList)
      } catch (reason) {
        setError(reason instanceof Error ? reason.message : 'Unable to load dashboard data.')
      }
    })()
  }, [])

  const summary = useMemo(() => {
    const projectList = projects ?? []
    return {
      projectCount: projectList.length || data?.projectCount || 0,
      activeProjects: projectList.filter((project) => project.status === 'Active').length,
      completedProjects: projectList.filter((project) => project.status === 'Completed').length,
      tasks: data?.taskCount ?? 0,
      completedTasks: data?.completedTaskCount ?? 0,
      overdueTasks: data?.overdueTaskCount ?? 0,
      statusEntries: Object.entries(data?.tasksByStatus ?? {}).map(([status, count]) => ({
        status,
        label: labels[status] ?? status,
        count: Number(count || 0)
      }))
    }
  }, [data, projects])

  if (error) return <PageError message={error} />
  if (!data || !projects) return <Loading />

  return (
    <section>
      <div className="page-heading">
        <div>
          <div className="eyebrow">OVERVIEW</div>
          <h1>Delivery dashboard</h1>
          <p>Monitor cloud work across your active projects.</p>
        </div>
      </div>

      <div className="metrics">
        <Metric label="Projects" value={summary.projectCount} accent="blue" />
        <Metric label="Active" value={summary.activeProjects} accent="blue" />
        <Metric label="Completed" value={summary.completedProjects} accent="green" />
        <Metric label="Tasks" value={summary.tasks} accent="violet" />
        <Metric label="Done" value={summary.completedTasks} accent="green" />
        <Metric label="Overdue" value={summary.overdueTasks} accent="red" />
      </div>

      <div className="panel">
        <h2>Task flow</h2>
        <div className="status-grid">
          {summary.statusEntries.map(({ status, label, count }) => (
            <div className="status-item" key={status}>
              <span>{label}</span>
              <strong>{count}</strong>
              <div className="progress"><i style={{ width: `${summary.tasks ? (count / summary.tasks) * 100 : 0}%` }} /></div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}

export const Loading = () => <div className="loading">Loading workspace…</div>
export const PageError = ({ message }: { message: string }) => <div className="error panel">{message}</div>

function Metric({ label, value, accent }: { label: string; value: number; accent: string }) {
  return (
    <div className={`metric ${accent}`}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}
