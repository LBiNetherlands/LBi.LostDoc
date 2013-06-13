using System;
using System.ComponentModel.Composition;
using System.Web;
using System.Web.Http;
using LBi.LostDoc.Repository.Web.Configuration;
using LBi.LostDoc.Repository.Web.Configuration.Composition;

namespace LBi.LostDoc.Repository.Web.Security
{
    public class ApiKeyAuthorizeAttribute : AuthorizeAttribute
    {
        [ImportSetting(Settings.ApiKey)]
        protected string ApiKey { get; set; }

        protected override bool IsAuthorized(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            var queryString = HttpUtility.ParseQueryString(actionContext.Request.RequestUri.Query);
            return StringComparer.OrdinalIgnoreCase.Equals(this.ApiKey, queryString["apiKey"]);
        }
    }
}