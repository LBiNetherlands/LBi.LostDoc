using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Filters;

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