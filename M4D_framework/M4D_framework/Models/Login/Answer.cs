using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace M4D_framework.Models.Login
{
    public class Answer
    {
        public string Token { get; set; }

        public Answer(string token)
        {
            Token = token;
        }
    }
}