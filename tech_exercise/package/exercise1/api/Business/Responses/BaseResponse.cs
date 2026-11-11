using System.Net;

namespace StargateAPI.Business.Responses
{
    /// <summary>
    /// Common envelope returned by every endpoint, for both successful and failed requests.
    /// </summary>
    public class BaseResponse
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = "Successful";
        public int ResponseCode { get; set; } = (int)HttpStatusCode.OK;
    }

    /// <summary>
    /// Returned for invalid requests; lists every validation failure so a client can fix them all at once.
    /// </summary>
    public class ValidationErrorResponse : BaseResponse
    {
        public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
    }
}
