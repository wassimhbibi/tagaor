using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Diag.Models
{
    public class foPas
    {
        public int id { get; set; }
        public string email { get; set; }
        public string resetToken { get; set; }
        public string ExpiryDate { get; set; }
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