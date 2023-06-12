using Microsoft.AspNetCore.Mvc;
using System.Fabric;

namespace PingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MyApiController : ControllerBase
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private string apiEndpoint;
        public MyApiController(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        [HttpGet("ping")]
        public IActionResult Ping(CancellationToken cancellationToken)
        {
            var request = httpContextAccessor.HttpContext.Request;
            apiEndpoint = $"{request.Scheme}://{request.Host.Value}/api/endpoint";

            // Your logic using the apiEndpoint value

            return Ok();
        }
        public string ApiEndpoint => apiEndpoint;
    }
}



