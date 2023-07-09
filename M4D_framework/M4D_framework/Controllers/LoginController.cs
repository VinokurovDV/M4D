using M4D_framework.Helpers;
using M4D_framework.Models.Login;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace ApiAckomNet.Controllers.M4D
{
    public class LoginController : ApiController
    {
        private readonly object log_lock = new object();

        public LoginController()
        {

        }

        [HttpGet]
        [Route("api/login")]
        public async Task<IHttpActionResult> Login()
        {
            var headerTokens = Request.Headers.GetValues("ApiKey");
            var headerToken = headerTokens.ToList().FirstOrDefault();

            if(string.IsNullOrEmpty(headerToken))
            {
                return BadRequest("Не предоставлен ApiKey заголовок");
            }

            WriteLog($"Income request Login : {headerToken}");
            try
            {
                CustomConfigSection configSection = (CustomConfigSection)ConfigurationManager.GetSection("CustomConfigSection");

                var incomeClientName = string.Empty;
                foreach(CustomConfigSectionElement item in configSection.Clients)
                {
                    if(item.ApiKey == headerToken)
                    {
                        incomeClientName = item.Name;
                    }
                }

                if (string.IsNullOrEmpty(incomeClientName))
                {
                    WriteLog($"User Not Found by token: {headerToken}");
                    return BadRequest("Incorrect api key");
                }

                var token = await GetM4DToken();
                return Json<Answer>(new Answer(token));
            }
            catch (Exception ex)
            {
                var errorGuid = Guid.NewGuid().ToString();
                WriteLog($"Exception in Login #{errorGuid}: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    WriteLog($"INNER #{errorGuid}: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                }
                return BadRequest($"{errorGuid} - {ex.Message}\n{ex.StackTrace}");
            }
        }

        //[HttpGet("RefreshToken")]
        //public async Task<IActionResult> RefreshToken()
        //{
        //    try
        //    {
        //        var token = await GetNewToken();
        //        var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1));
        //        _cache.Set(TOKEN_CACHE_NAME, token, cacheOptions);
        //        return Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        var errorGuid = Guid.NewGuid().ToString();
        //        _logger.LogError($"Exception in RefreshToken #{errorGuid}: {ex.Message}\n{ex.StackTrace}");
        //        return StatusCode(500, errorGuid);
        //    }
        //}


        private async Task<string> GetM4DToken()
        {
            //if (!_cache.TryGetValue<string>(TOKEN_CACHE_NAME, out string token))
            //{
            var token = await GetNewToken();
                //var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1));
                //_cache.Set(TOKEN_CACHE_NAME, token, cacheOptions);
            //}

            return token;
        }

        private async Task<string> GetNewToken()
        {
            var baseToken = await ExecuteM4DLoginCertRequest<GetAuthTokenResponse>("get");

            var sign = await GetSign(baseToken.base64Token);
            WriteLog($"Sign = {sign}");
            var jwt = await ExecuteM4DLoginCertRequest<GetJwtResult>("post", baseToken.base64Token, sign);
            return jwt.token;
        }

        private async Task<string> GetSign(string base64Token)
        {
            var encodedToken = Encoding.Default.GetString(Convert.FromBase64String(base64Token));
            WriteLog($"Encodod token = {encodedToken}");

            var cms = new SignedCms(new ContentInfo(Encoding.Default.GetBytes(encodedToken)), true);

            var _store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            _store.Open(OpenFlags.ReadOnly);

            var Thumbprint = "CF7B64BB91953DDEACFCF6BCCF287DA27CA41BC7";
            var _cert = _store.Certificates.Find(X509FindType.FindByThumbprint, Thumbprint, false);

            if (_cert.Count == 0)
                throw new Exception($"К сожалению, не удалось найти сертификат с отпечатком {Thumbprint} в хранилище \"Личные\" компьютера, пожалуйста, произведите установку сертификата в хранилище");

            var a = new CmsSigner(_cert[0]);

            cms.ComputeSignature(a);

            var _licenseText = cms.Encode();
            return (Convert.ToBase64String(_licenseText));
        }

        private async Task<T> ExecuteM4DLoginCertRequest<T>(string method, string token = "", string sign = "")
        {
            using (var client = GetM4DClient())
            {
                var content = new MultipartFormDataContent();
                if (!string.IsNullOrEmpty(token))
                {
                    content.Add(new StringContent(sign), "sign");
                    content.Add(new StringContent(token), "base64Token");
                }
                HttpResponseMessage response;
                if (method == "get")
                {
                    response = await client.GetAsync("api/login/cert");
                }
                else
                {
                    response = await client.PostAsync("api/login/cert", content);
                }

                var body = await response.Content.ReadAsStringAsync();
                WriteLog($"It com response on {method}: status {response.StatusCode}, {body}");
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<T>(body);
                    return result;
                }

                throw new Exception($"Error in web ExecuteM4DLoginCertRequest. status {response.StatusCode}\n{body}");
            }
        }

        void WriteLog(string text)
        {
            var pathRoot = ConfigurationManager.AppSettings["LogPath"];
            lock (log_lock)
            {
                var date = DateTime.Now.Date.ToString("dd_MM_yyyy");

                var path = $"{pathRoot}{date}.txt";

                File.AppendAllText(path, $"\n{DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss")}: {text}", Encoding.UTF8);
            }
        }

        private HttpClient GetM4DClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("https://m4d.uc-itcom.ru");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            return client;
        }
    }
}