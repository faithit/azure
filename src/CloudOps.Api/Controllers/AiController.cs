using CloudOps.Application.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudOps.Api.Controllers;

[ApiController, Authorize, Route("api/ai")]
public sealed class AiController(IAiService ai) : ControllerBase
{
    [HttpPost("chat")]
    [ProducesResponseType<AiChatResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AiChatResponse>> Chat(AiChatRequest request, CancellationToken cancellationToken) => Ok(await ai.AskAsync(request, cancellationToken));
}