using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Routing;
using LBi.LostDoc.Repository.Web.Api.Controllers;
using LBi.LostDoc.Repository.Web.Extensibility;
using Tavis;

namespace LBi.LostDoc.Repository.Web.Api
{
    public class AddInInitializer : IHttpRouteInitializer
    {
        public void RegisterRoutes(HttpRouteCollection routes)
        {
            TreeRoute treeRoute = new TreeRoute("", new TreeRoute("library").To<LibraryController>());

            routes.Add("LibraryRoute", treeRoute);
        }
    }
}
