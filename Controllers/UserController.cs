using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace WebAPI_Pattern_Implementation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UserController> _logger;
        private readonly ConnectionMultiplexer _redisConnection;

        public UserController(ILogger<UserController> logger, HttpClient httpClient, ConnectionMultiplexer redisConnection)
        {
            _logger = logger;
            _httpClient = httpClient;
            _redisConnection = redisConnection;
        }

        [HttpGet(Name = "GetUserDetails")]
        public async Task<IActionResult> Get()
        {
            IDatabase cache = _redisConnection.GetDatabase();
            // Check if user data exists in cache
            string cachedUsers = await cache.StringGetAsync("users");
            if (!string.IsNullOrEmpty(cachedUsers))
            {
                var users = JsonSerializer.Deserialize<Users[]>(cachedUsers);
                Response.Headers.Append("X-Data-Source", "Redis Cache");
                return Ok(users);
            }

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
                    // Store user data in cache for 1 hour
                    await cache.StringSetAsync("users", JsonSerializer.Serialize(users), TimeSpan.FromHours(1));
                    Response.Headers.Append("X-Data-Source", "jsonplaceholder");
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
