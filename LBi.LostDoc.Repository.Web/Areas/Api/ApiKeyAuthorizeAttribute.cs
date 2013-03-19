using System;
using System.Web;
using System.Web.Http;

namespace LBi.LostDoc.Repository.Web.Areas.Api
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