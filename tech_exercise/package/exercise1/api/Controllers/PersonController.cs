using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;

namespace StargateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PersonController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PersonController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetPeople(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new GetPeople(), cancellationToken);

                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetErrorResponse(ex);
            }
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetPersonByName(string name, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new GetPersonByName()
                {
                    Name = name
                }, cancellationToken);

                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetErrorResponse(ex);
            }
        }

        [HttpPost("")]
        public async Task<IActionResult> CreatePerson([FromBody] CreatePerson request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(request, cancellationToken);

                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetErrorResponse(ex);
            }
        }

        [HttpPut("{name}")]
        public async Task<IActionResult> UpdatePerson(string name, [FromBody] UpdatePersonRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new UpdatePerson()
                {
                    CurrentName = name,
                    Name = request.Name
                }, cancellationToken);

                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetErrorResponse(ex);
            }
        }
    }

    public class UpdatePersonRequest
    {
        public required string Name { get; set; }
    }
}
