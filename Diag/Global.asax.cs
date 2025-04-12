using Microsoft.IdentityModel.Tokens;
using System;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Http.Cors;


namespace Diag
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            ConfigureAuth();
            ConfigureCors();
        }
    

       

        private void ConfigureCors()
        {
            var cors = new EnableCorsAttribute("http://localhost:4200", "*", "*");
            GlobalConfiguration.Configuration.EnableCors(cors);
        }

        public void ConfigureAuth()
        {
            GlobalConfiguration.Configuration.MessageHandlers.Add(new JwtAuthenticationHandler());
        }
    }

    public class JwtAuthenticationHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            if (!request.Headers.Contains("Authorization"))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var tokenString = request.Headers.GetValues("Authorization").FirstOrDefault()?.Replace("Bearer ", "");
            if (tokenString != null)
            {
                try
                {
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["JwtKey"]));
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var validationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = ConfigurationManager.AppSettings["JwtIssuer"],
                        ValidAudience = ConfigurationManager.AppSettings["JwtAudience"],
                        IssuerSigningKey = key,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    SecurityToken validatedToken;
                    var principal = tokenHandler.ValidateToken(tokenString, validationParameters, out validatedToken);
                    HttpContext.Current.User = principal;
                }
                catch (Exception)
                {
                    // Token validation failed
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
