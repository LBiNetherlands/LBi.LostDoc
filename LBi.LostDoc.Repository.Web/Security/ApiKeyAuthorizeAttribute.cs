using System;
using System.Web;
using System.Web.Http;
using LBi.LostDoc.Repository.Web.Configuration;

namespace LBi.LostDoc.Repository.Web.Security
{
    public class ApiKeyAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool IsAuthorized(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            var queryString = HttpUtility.ParseQueryString(actionContext.Request.RequestUri.Query);
            return StringComparer.OrdinalIgnoreCase.Equals(AppConfig.ApiKey, queryString["apiKey"]);
        }
    }
}