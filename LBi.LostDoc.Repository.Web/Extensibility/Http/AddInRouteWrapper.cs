using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Routing;

namespace LBi.LostDoc.Repository.Web.Extensibility.Http
{
    public class AddInRouteWrapper : IHttpRoute
    {
        private readonly IHttpRoute _inner;

        public AddInRouteWrapper(IHttpRoute addInRoute)
        {
            this._inner = addInRoute;
        }

        public IHttpRouteData GetRouteData(string virtualPathRoot, HttpRequestMessage request)
        {
            throw new NotImplementedException();
        }

        public IHttpVirtualPathData GetVirtualPath(HttpRequestMessage request, IDictionary<string, object> values)
        {
            throw new NotImplementedException();
        }

        public string RouteTemplate { get; private set; }
        public IDictionary<string, object> Defaults { get; private set; }
        public IDictionary<string, object> Constraints { get; private set; }
        public IDictionary<string, object> DataTokens { get; private set; }
        public HttpMessageHandler Handler { get; private set; }
    }
}
