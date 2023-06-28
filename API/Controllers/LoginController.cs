using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Collections.Specialized;
using System.Net;
using System.Text.Json;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {

        [HttpGet]
        public IActionResult Login()
        {
            string loginKey = Request.Headers["ApiKey"];
            NameValueCollection allowedKeys = ConfigurationManager.AppSettings["ApiKey"];
            

            //foreach (var key in allowedKeys.AllKeys)
            //{

            //    if (loginKey == key)
            //    {
            //        Answer ans = new Answer();
            //        ans.Token = "klvhsdfjkbfdk";
            //        return Ok(JsonSerializer.Serialize(ans));
            //    }

            //}
            return Unauthorized();

            
        }
    
    
    
               
        

    //public bool GetAllowedApiKeys()
    //{
    //    NameValueCollection allowedKeys = ConfigurationManager.AppSettings["ApiKeys"];


    //    foreach (var key in allowedKeys.AllKeys)
    //    {
    //    // string[] allowedApiKeys.add(Key);
    //    if (apiKey == key) return true;
    //    }


    //    return false;
    }

}
