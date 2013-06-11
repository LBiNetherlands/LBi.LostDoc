using System.Web.Mvc;

namespace LBi.LostDoc.Repository.Web.Areas.Api
{
    public class ApiAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Api";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            //context.MapRoute(
            //    "Api",
            //    "api/{packageId}/{packageVersion}/{controller}/{action}/{id}",
            //    new {action = "Index", id = UrlParameter.Optional});
        }
    }
}
