import { FormEvent, useEffect, useState } from 'react'
import { cloudops } from '../api/cloudops'
import type { AiChatResponse, Project } from '../types'
import './AssistantPage.css'

type ChatTurn = { id: number; question: string; response?: AiChatResponse }

const suggestedPrompts = [
  'What tasks are overdue?',
  'What are the highest priority tasks?',
  'Summarize the current project status.',
  'What project risks can you identify?',
  'Suggest tasks that should be created based on the project description.'
]

export function AssistantPage() {
  const [projects, setProjects] = useState<Project[]>([])
  const [projectId, setProjectId] = useState('')
  const [question, setQuestion] = useState('')
  const [turns, setTurns] = useState<ChatTurn[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    void cloudops.projects().then(setProjects).catch(() => setError('Unable to load project choices. You can still ask about all available projects.'))
  }, [])

  async function submit(event: FormEvent) {
    event.preventDefault()
    const nextQuestion = question.trim()
    if (nextQuestion.length < 3 || loading) return

    const id = Date.now()
    setLoading(true)
    setError('')
    setTurns((current) => [...current, { id, question: nextQuestion }])
    setQuestion('')

    try {
      const response = await cloudops.askAi(nextQuestion, projectId || undefined)
      setTurns((current) => current.map((turn) => turn.id === id ? { ...turn, response } : turn))
    } catch (reason) {
      setTurns((current) => current.filter((turn) => turn.id !== id))
      setQuestion(nextQuestion)
      setError(reason instanceof Error ? reason.message : 'The assistant could not answer right now.')
    } finally {
      setLoading(false)
    }
  }

  function usePrompt(prompt: string) {
    setQuestion(prompt)
  }

  return (
    <section>
      <div className="page-heading">
        <div>
          <div className="eyebrow">DELIVERY INTELLIGENCE</div>
          <h1>AI Assistant</h1>
          <p>Analyze the projects and tasks you have access to.</p>
        </div>
      </div>

      <div className="assistant-layout">
        <aside className="panel assistant-intro">
          <div className="assistant-sigil" aria-hidden="true">✦</div>
          <h2>Project-aware help</h2>
          <p>Ask for status, priorities, overdue work, risks, or recommended next tasks. Answers use only your selected project context.</p>
          <label className="assistant-scope">Project scope
            <select value={projectId} onChange={(event) => setProjectId(event.target.value)} disabled={loading}>
              <option value="">All my projects</option>
              {projects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}
            </select>
          </label>
          <div className="prompt-list">
            {suggestedPrompts.map((prompt) => <button className="prompt-button" type="button" key={prompt} onClick={() => usePrompt(prompt)} disabled={loading}>{prompt}</button>)}
          </div>
        </aside>

        <div className="panel chat-panel">
          <div className="chat-history" aria-live="polite">
            {turns.length === 0 && !loading && <div className="chat-empty"><strong>What would you like to know?</strong><span>Choose a prompt or write your own question below.</span></div>}
            {turns.map((turn) => <div className="chat-turn" key={turn.id}>
              <div className="chat-question">{turn.question}</div>
              {turn.response && <div className="chat-answer"><p>{turn.response.answer}</p><small>{turn.response.provider} · {turn.response.sources.map((source) => source.projectName).join(', ') || 'No project context'}</small></div>}
            </div>)}
            {loading && <div className="assistant-thinking"><i className="thinking-dot" />Analyzing project context…</div>}
          </div>

          {error && <p className="error assistant-error" role="alert">{error}</p>}
          <form className="assistant-form" onSubmit={submit}>
            <label htmlFor="assistant-question" className="sr-only">Ask the AI assistant</label>
            <textarea id="assistant-question" value={question} onChange={(event) => setQuestion(event.target.value)} placeholder="Ask about risks, priority, task status, or next steps…" minLength={3} maxLength={2000} disabled={loading} required />
            <div className="assistant-form-footer"><small>{question.length}/2000</small><button type="submit" disabled={loading || question.trim().length < 3}>{loading ? 'Analyzing…' : 'Ask assistant'}</button></div>
          </form>
        </div>
      </div>
    </section>
  )
}
