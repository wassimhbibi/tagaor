using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Diag.Models
{
    public class Account
    {

        public int id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public int phone { get; set; }
        public string role { get; set; }
        public string id_token { get; set; }

        public string key = "wassimheros";

        public string coverttoencrypt(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";
            password += key;
            var passwordByte = Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(passwordByte);
        }

    }
}