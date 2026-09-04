using CloudOps.Application.Projects;
using CloudOps.Application.Tasks;
using CloudOps.Application.Common;
using CloudOps.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudOps.Api.Controllers;

[ApiController, Authorize, Route("api/projects")]
public sealed class ProjectsController(IProjectService projects, ITaskService tasks) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<PagedResult<ProjectDto>>> GetAll([FromQuery] ProjectListQuery query, CancellationToken cancellationToken) => Ok(await projects.GetAllAsync(query, cancellationToken));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ProjectDto>> GetById(Guid id, CancellationToken cancellationToken) => Ok(await projects.GetByIdAsync(id, cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.ManageProjects), HttpPost] public async Task<ActionResult<ProjectDto>> Create(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await projects.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { project.Id }, project);
    }
    [Authorize(Policy = AuthorizationPolicies.ManageProjects), HttpPut("{id:guid}")] public async Task<ActionResult<ProjectDto>> Update(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken) => Ok(await projects.UpdateAsync(id, request, cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.ManageProjects), HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) { await projects.DeleteAsync(id, cancellationToken); return NoContent(); }
    [HttpGet("{projectId:guid}/members")] public async Task<ActionResult<IReadOnlyList<ProjectMemberDto>>> GetMembers(Guid projectId, CancellationToken cancellationToken) => Ok(await projects.GetMembersAsync(projectId, cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.ManageProjects), HttpPost("{projectId:guid}/members")] public async Task<ActionResult<ProjectMemberDto>> AddMember(Guid projectId, AddProjectMemberDto request, CancellationToken cancellationToken)
    {
        var member = await projects.AddMemberAsync(projectId, request, cancellationToken);
        return CreatedAtAction(nameof(GetMembers), new { projectId }, member);
    }
    [Authorize(Policy = AuthorizationPolicies.ManageProjects), HttpDelete("{projectId:guid}/members/{userId}")] public async Task<IActionResult> RemoveMember(Guid projectId, string userId, CancellationToken cancellationToken) { await projects.RemoveMemberAsync(projectId, userId, cancellationToken); return NoContent(); }
    [HttpGet("{projectId:guid}/tasks")] public async Task<ActionResult<PagedResult<TaskDto>>> GetTasks(Guid projectId, [FromQuery] TaskListQuery query, CancellationToken cancellationToken) => Ok(await tasks.GetByProjectIdAsync(projectId, query, cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.ManageTasks), HttpPost("{projectId:guid}/tasks")]
    public async Task<ActionResult<TaskDto>> CreateTask(Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await tasks.CreateAsync(projectId, request, cancellationToken);
        return CreatedAtRoute("GetTask", new { task.Id }, task);
    }
}
