using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Queries;
using StargateAPI.Business.Responses;

namespace StargateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
    public class ProcessLogController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProcessLogController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Retrieves the most recent process log entries (successes and failures), newest first.</summary>
        /// <param name="take">Number of entries to return (1-500, default 50).</param>
        /// <param name="cancellationToken">Cancels the request.</param>
        [HttpGet("")]
        [ProducesResponseType(typeof(GetProcessLogsResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProcessLogs([FromQuery] int take = Business.Queries.GetProcessLogs.DefaultTake, CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new GetProcessLogs { Take = take }, cancellationToken);
            return this.GetResponse(result);
        }
    }
}
