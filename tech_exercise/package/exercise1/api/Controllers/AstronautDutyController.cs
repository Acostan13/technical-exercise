using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;
using StargateAPI.Business.Responses;

namespace StargateAPI.Controllers
{
    /// <summary>
    /// Errors are not caught here: they flow to <see cref="Infrastructure.StargateExceptionHandler"/>,
    /// which maps every failure to a consistent response.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
    public class AstronautDutyController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AstronautDutyController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Retrieves a person's current astronaut information and duty history, most recent first.</summary>
        [HttpGet("{name}")]
        [ProducesResponseType(typeof(GetAstronautDutiesByNameResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAstronautDutiesByName(string name, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAstronautDutiesByName { Name = name }, cancellationToken);
            return this.GetResponse(result);
        }

        /// <summary>
        /// Adds an astronaut duty. The previous duty ends the day before; a 'RETIRED' duty ends the career.
        /// </summary>
        [HttpPost("")]
        [ProducesResponseType(typeof(CreateAstronautDutyResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateAstronautDuty([FromBody] CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return this.GetResponse(result);
        }
    }
}
