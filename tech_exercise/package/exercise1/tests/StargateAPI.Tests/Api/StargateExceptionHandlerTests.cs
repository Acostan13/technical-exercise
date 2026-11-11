using StargateAPI.Business.Exceptions;
using StargateAPI.Business.Responses;
using StargateAPI.Infrastructure;

namespace StargateAPI.Tests.Api;

public class StargateExceptionHandlerTests
{
    [Fact]
    public void ValidationFailure_Maps_To400_WithEveryError()
    {
        var response = StargateExceptionHandler.ToResponse(
            new RequestValidationException(new[] { "Name is required.", "Rank is required." }));

        var validation = Assert.IsType<ValidationErrorResponse>(response);
        Assert.False(validation.Success);
        Assert.Equal(400, validation.ResponseCode);
        Assert.Equal(2, validation.Errors.Count);
    }

    [Fact]
    public void NotFound_Maps_To404_WithItsMessage()
    {
        var response = StargateExceptionHandler.ToResponse(NotFoundException.Person("Nobody"));

        Assert.Equal(404, response.ResponseCode);
        Assert.Equal("No person named 'Nobody' was found.", response.Message);
    }

    [Fact]
    public void Conflict_Maps_To409()
    {
        var response = StargateExceptionHandler.ToResponse(new ConflictException("Duplicate."));

        Assert.Equal(409, response.ResponseCode);
        Assert.Equal("Duplicate.", response.Message);
    }

    [Fact]
    public void CancelledRequest_Maps_To499()
    {
        var response = StargateExceptionHandler.ToResponse(new OperationCanceledException());

        Assert.Equal(499, response.ResponseCode);
        Assert.Equal("The request was cancelled.", response.Message);
    }

    [Fact]
    public void UnexpectedException_Maps_To500_WithoutLeakingDetails()
    {
        var response = StargateExceptionHandler.ToResponse(
            new InvalidOperationException("Connection string 'secret' failed"));

        Assert.Equal(500, response.ResponseCode);
        Assert.False(response.Success);
        Assert.Equal("An unexpected error occurred.", response.Message);
    }
}
