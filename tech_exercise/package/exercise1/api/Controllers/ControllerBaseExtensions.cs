using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace StargateAPI.Controllers
{
    public static class ControllerBaseExtensions
    {
        public static IActionResult GetResponse(this ControllerBase controllerBase, BaseResponse response)
        {
            var httpResponse = new ObjectResult(response);
            httpResponse.StatusCode = response.ResponseCode;
            return httpResponse;
        }

        /// <summary>
        /// Maps an exception to a response. Expected business failures carry their own status code and a
        /// client-safe message; anything unexpected becomes a generic 500 so internal details are not leaked.
        /// </summary>
        public static IActionResult GetErrorResponse(this ControllerBase controllerBase, Exception exception)
        {
            var response = exception is BadHttpRequestException badRequest
                ? new BaseResponse
                {
                    Success = false,
                    Message = badRequest.Message,
                    ResponseCode = badRequest.StatusCode
                }
                : new BaseResponse
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                };

            return controllerBase.GetResponse(response);
        }
    }
}
