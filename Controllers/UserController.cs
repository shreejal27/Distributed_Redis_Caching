using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace WebAPI_Pattern_Implementation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        [HttpGet(Name = "GetUserDetails")]
        public async Task<IActionResult> Get()
        {
            int maxRetryAttempts = 3;
            TimeSpan retryInterval = TimeSpan.FromSeconds(1);
            int retryCount = 0;

            do
            {
                try
                {
                    var response = await _httpClient.GetAsync("https://jsonplaceholder.typicode.com/users");
                    response.EnsureSuccessStatusCode();
                    var users = await response.Content.ReadFromJsonAsync<Users[]>();
                    return Ok(users);
                }
                catch (HttpRequestException)
                {
                    if (retryCount < maxRetryAttempts)
                    {
                        await Task.Delay(retryInterval);
                        retryCount++;
                    }
                    else
                    {
                        return StatusCode(500, "Failed to fetch user data after multiple attempts.");
                    }
                }
            } while (retryCount < maxRetryAttempts);

            return StatusCode(500, "Failed to fetch user data after multiple attempts.");
        }
    }
}
