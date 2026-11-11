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
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
    public class PersonController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PersonController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Retrieves all people, with current astronaut information where they have it.</summary>
        [HttpGet("")]
        [ProducesResponseType(typeof(GetPeopleResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPeople(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPeople(), cancellationToken);
            return this.GetResponse(result);
        }

        /// <summary>Retrieves a person by name (case-insensitive).</summary>
        [HttpGet("{name}")]
        [ProducesResponseType(typeof(GetPersonByNameResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPersonByName(string name, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPersonByName { Name = name }, cancellationToken);
            return this.GetResponse(result);
        }

        /// <summary>Adds a person. Names are unique.</summary>
        [HttpPost("")]
        [ProducesResponseType(typeof(CreatePersonResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreatePerson([FromBody] CreatePerson request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return this.GetResponse(result);
        }

        /// <summary>Updates (renames) the person with the given name.</summary>
        [HttpPut("{name}")]
        [ProducesResponseType(typeof(UpdatePersonResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdatePerson(string name, [FromBody] UpdatePersonRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new UpdatePerson { CurrentName = name, Name = request.Name }, cancellationToken);
            return this.GetResponse(result);
        }
    }

    public class UpdatePersonRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}
