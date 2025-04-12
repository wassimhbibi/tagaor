using Diag.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Web.Http;

namespace Diag.Controllers
{
    public class SignInController : ApiController
    {
        public IHttpActionResult Post(Account log)
        {
            string decryptedPassword = log.coverttoencrypt(log.password);

            string query = @"SELECT email, role FROM dbo.Account WHERE email = @Email AND password = @Password";
            DataTable table = new DataTable();

            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["diag"].ConnectionString))
            using (var cmd = new SqlCommand(query, con))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@Email", log.email);
                cmd.Parameters.AddWithValue("@Password", decryptedPassword);
                cmd.CommandType = CommandType.Text;
                da.Fill(table);
            }

            if (table.Rows.Count == 0)
            {
                return Content(HttpStatusCode.Unauthorized, new { Message = "Invalid email or password" });
            }




            var userRole = table.Rows[0]["role"].ToString();
            var email = log.email;
            var (token, tokenId) = GenerateJwtToken(email, userRole);
            StoreRefreshTokenInDatabase(email, tokenId);





            return Ok(new { Message = "Authentication successful", Token = token, TokenId = tokenId, Role = userRole });
        }



        private void StoreRefreshTokenInDatabase(string email, string idtoken)
        {
            string query = @"UPDATE dbo.Account
                     SET  id_token = @IdToken  
                     WHERE email = @Email";

            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["diag"].ConnectionString))
            using (var cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Email", email);

                cmd.Parameters.AddWithValue("@IdToken", idtoken);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }



        private (string Token, string TokenId) GenerateJwtToken(string email, string userRole)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["JwtKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var issuer = ConfigurationManager.AppSettings["JwtIssuer"];
            var audience = ConfigurationManager.AppSettings["JwtAudience"];
            var expireMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["JwtExpireMinutes"]);

            var expires = DateTime.Now.AddMinutes(expireMinutes);
            var tokenId = Guid.NewGuid().ToString();
            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Email, email),
         new Claim(JwtRegisteredClaimNames.Sub, userRole),
        new Claim(JwtRegisteredClaimNames.Jti, tokenId)
    };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
            claims: claims,
            expires: expires,
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return (Token: tokenString, TokenId: tokenId);
        }
        [HttpPost]
        [Route("api/SignIn/forgotpassword")]
        public HttpResponseMessage ForgotPassword([FromBody] foPas l)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["diag"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string resetToken = GeneratePasswordResetToken();

                string query = "INSERT INTO forgetPass (Email, resetToken, ExpiryDate) VALUES (@email, @resetToken, @ExpiryDate)";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@email", l.email);
                command.Parameters.AddWithValue("@resetToken", resetToken);

                DateTime expiryDate = DateTime.UtcNow.AddHours(1).AddMinutes(10);

                command.Parameters.AddWithValue("@ExpiryDate", expiryDate);

                command.ExecuteNonQuery();
                SendPasswordResetEmail(l.email, resetToken);

                return Request.CreateResponse(HttpStatusCode.OK, "Password reset instructions sent to your email.");
            }

        }






        // generete token
        private string GeneratePasswordResetToken()
        {
            // Generate a secure random token (you may need to customize this)
            var tokenLength = 32;
            var randomBytes = new byte[tokenLength];
            using (var rngCryptoServiceProvider = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                rngCryptoServiceProvider.GetBytes(randomBytes);
            }

            // Convert the random bytes to a base64-encoded string

            var token = Convert.ToBase64String(randomBytes);
            // Remove spaces from the token
            token = token.Replace(" ", string.Empty);
            return token;
        }


        [HttpPost]
        [Route("api/SignIn/UpdateUserPassword")]
        public HttpResponseMessage ResetPassword([FromBody] ResetPassword resetPasswordModel)
        {

            string connectionString = ConfigurationManager.ConnectionStrings["diag"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                DateTime DateNow = DateTime.UtcNow.AddHours(1);
                string receivedToken = resetPasswordModel.FoPasModel.resetToken.Replace(" ", "+");
                // Verify the reset token and check if it's still valid (not expired)
                string query = "SELECT email FROM forgetPass WHERE resetToken = @resetToken AND ExpiryDate > @DateNow";
                SqlCommand selectCommand = new SqlCommand(query, connection);
                selectCommand.Parameters.Add("@resetToken", SqlDbType.NVarChar, 255).Value = receivedToken;
                selectCommand.Parameters.AddWithValue("@DateNow", DateNow);

                var email = selectCommand.ExecuteScalar() as string;

                if (email != null)
                {

                    // Token is valid, update the user's password
                    string updateQuery = "UPDATE Account SET password = @NewPassword WHERE Email = @Email";
                    SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@NewPassword", resetPasswordModel.UsersModel.coverttoencrypt(resetPasswordModel.UsersModel.password));
                    updateCommand.Parameters.AddWithValue("@Email", email);

                    updateCommand.ExecuteNonQuery();

                    // Remove the used reset token from the database
                    string deleteQuery = "DELETE FROM forgetPass WHERE resetToken = @resetToken";
                    SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection);
                    deleteCommand.Parameters.AddWithValue("@resetToken", resetPasswordModel.FoPasModel.resetToken);
                    deleteCommand.ExecuteNonQuery();

                    return Request.CreateResponse(HttpStatusCode.OK, "Password reset successful.");
                }
                else
                {
                    string deleteQuery = "DELETE FROM forgetPass WHERE resetToken = @resetToken";
                    SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection);
                    deleteCommand.Parameters.AddWithValue("@resetToken", resetPasswordModel.FoPasModel.resetToken);
                    deleteCommand.ExecuteNonQuery();
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid or expired token.");
                }
            }
        }






        private void SendPasswordResetEmail(string email, string token)
        {
            string smtpServer = "smtp.gmail.com";
            int smtpPort = 587;
            string smtpUsername = "diagtaga@gmail.com";
            string smtpPassword = "b f m n b c j d m o a l o z t w";
            string senderEmail = email;


            using (SmtpClient smtpClient = new SmtpClient(smtpServer))
            {
                smtpClient.Port = smtpPort;
                smtpClient.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
                smtpClient.EnableSsl = true; // Enable SSL if required

                using (MailMessage mailMessage = new MailMessage(senderEmail, email))
                {

                    var frontendBaseUrl = ConfigurationManager.AppSettings["FrontendBaseUrl"];
                    var resetPasswordUrl = $"https://tagadiag.com/pages/reinitialiser_le_mot_de_passe?token={token}";
                    mailMessage.Subject = "Password Reset Request";
                    mailMessage.Body = $"Pour réinitialiser votre mot de passe, cliquez sur le lien suivant:{resetPasswordUrl}";
                    mailMessage.IsBodyHtml = true;

                    smtpClient.Send(mailMessage);
                }
            }
        }

    
}
}
