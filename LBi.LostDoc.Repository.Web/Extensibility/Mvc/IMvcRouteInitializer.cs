using System.ComponentModel.Composition;
using System.Web.Routing;

namespace LBi.LostDoc.Repository.Web.Extensibility.Mvc
{
    [InheritedExport]
    public interface IMvcRouteInitializer
    {
        void RegisterRoutes(RouteCollection routes);
    }
}