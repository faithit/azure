using CloudOps.Application.Tasks;
using CloudOps.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudOps.Api.Controllers;

[ApiController, Authorize, Route("api/tasks")]
public sealed class TasksController(ITaskService tasks) : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetTask")]
    public async Task<ActionResult<TaskDto>> GetById(Guid id, CancellationToken cancellationToken) => Ok(await tasks.GetByIdAsync(id, cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.ManageTasks), HttpPut("{id:guid}")] public async Task<ActionResult<TaskDto>> Update(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken) => Ok(await tasks.UpdateAsync(id, request, cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.ManageTasks), HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) { await tasks.DeleteAsync(id, cancellationToken); return NoContent(); }
}
