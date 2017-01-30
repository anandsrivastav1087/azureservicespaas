
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace WebRole1.Controllers
{
    public class AzureSQLController : Controller
    {
        // GET: AzureSQL
        public ActionResult sql()
        {
            return View();
        }

        //Retrieve the access token for the current user
        //private AuthenticationResult GetAccessToken()
        //{
        //    AuthenticationContext context = new AuthenticationContext("https://login.windows.net/domainname");
        //    AuthenticationResult result = context.AcquireToken("https://management.azure.com/"/* the Azure Resource Management endpoint */,
        //        ConfigurationManager.AppSettings["ClientID"].ToString(),new Uri(ConfigurationManager.AppSettings["PostLogoutRedirectUri"].ToString()) /* redirect URI */,
        //        PromptBehavior.Auto /* with Auto user will not be prompted if an unexpired token is cached */);

        //    return result;
        //}
       
    }
}