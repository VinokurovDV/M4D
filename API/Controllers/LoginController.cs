using API.Models;
using API.Models.M4DModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginController> _logger;
        private readonly IMemoryCache _cache;

        private string TOKEN_CACHE_NAME = "TOKEN_CACHED";

        public LoginController(IConfiguration configuration, ILogger<LoginController> logger, IMemoryCache cache)
        {
            _configuration = configuration;
            _logger = logger;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string login, string password)
        {
            _logger.LogInformation($"Income request Login : {login} - {password}");
            try
            {
                var users = _configuration.GetSection("ClientsCreditianals").Get<List<AppUser>>();

                var user = users.FirstOrDefault(e => e.Login == login);
                if (user == null)
                {
                    _logger.LogWarning($"User Not Found : {login} - {password}");
                    return BadRequest("Incorrect Login/Password pair");
                }

                if (CheckCreditionals(password, user))
                {
                    var token = await GetM4DToken();
                    return Ok(new Answer(token));
                }
                else
                {
                    _logger.LogWarning($"PassHash doesn't equals for : {login} - {password}");
                    return BadRequest("Incorrect Login/Password pair");
                }

            }
            catch (Exception ex)
            {
                var errorGuid = Guid.NewGuid().ToString();
                _logger.LogError($"Exception in Login #{errorGuid}: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, errorGuid);
            }
        }

        private bool CheckCreditionals(string password, AppUser user) 
        {
            var inputHash = GetAppHash(password, user.Salt);
            var hashString = System.Convert.ToBase64String(inputHash);

            return hashString.Equals(user.PassHash);
        }

        private byte[] GetAppHash(string input, string salt)
        {
            var inputBytes = Encoding.Default.GetBytes(input);
            var saltBytes = Encoding.Default.GetBytes(salt);
            var saltedValue = inputBytes.Concat(saltBytes).ToArray();

            return SHA256.Create().ComputeHash(saltedValue);
        }

        private async Task<string> GetM4DToken()
        {
            if(!_cache.TryGetValue<string>(TOKEN_CACHE_NAME, out string token))
            {
                token = await GetNewToken();
                var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1));
                _cache.Set(TOKEN_CACHE_NAME, token, cacheOptions);
            }

            return token;
        }

        private async Task<string> GetNewToken()
        {
            var baseToken = await ExecuteM4DLoginCertRequest<GetAuthTokenResponse>();

            var sign = await GetSign(baseToken.base64Token);

            var jwt = await ExecuteM4DLoginCertRequest<GetJwtResult>(baseToken.base64Token, sign);
            return jwt.token;
        }

        private async Task<string> GetSign(string base64Token)
        {

            return "123123";
        }

        private async Task<T> ExecuteM4DLoginCertRequest<T>(string token = "", string sign = "")
        {
            using var client = GetM4DClient();

            var content = new MultipartFormDataContent();
            if(!string.IsNullOrEmpty(token))
            {
                content.Add(new StringContent(sign), "sign");
                content.Add(new StringContent(token), "base64Token");
            }

            var response = await client.PostAsync("api/login/cert", content);
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<T>(body);
                return result;
            }

            throw new Exception($"Error in web ExecuteM4DLoginCertRequest. status {response.StatusCode}\n{body}");
        }

        private HttpClient GetM4DClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(_configuration.GetValue<string>("M4DBaseUrl"));

            return client;
        }
    }

}
