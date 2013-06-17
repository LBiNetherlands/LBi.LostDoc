using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Routing;

namespace LBi.LostDoc.Repository.Web.Extensibility
{
    [InheritedExport]
    public interface IHttpRouteInitializer
    {
        void RegisterRoutes(HttpRouteCollection routes);
    }

    [InheritedExport]
    public interface IMvcRouteInitializer
    {
        void RegisterRoutes(RouteCollection routes);
    }
}
