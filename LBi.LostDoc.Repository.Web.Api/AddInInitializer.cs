using System.Web.Http;
using LBi.LostDoc.Repository.Web.Api.Controllers;
using LBi.LostDoc.Repository.Web.Extensibility.Http;

namespace LBi.LostDoc.Repository.Web.Api
{
    public class AddInInitializer : IHttpRouteInitializer
    {
        public void RegisterRoutes(HttpRouteCollection routes)
        {
            routes.MapHttpRoute("library", "library", new {controller = "Library"});
            routes.MapHttpRoute("site", "site", new { controller = "Site" });
            routes.MapHttpRoute("repository", "repository", new { controller = "Repository" });
        }
    }
}
