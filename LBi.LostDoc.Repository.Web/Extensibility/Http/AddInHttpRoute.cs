/*
 * Copyright 2013 DigitasLBi Netherlands B.V.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Routing;
using System.Web.Routing;

namespace LBi.LostDoc.Repository.Web.Extensibility.Http
{
    public class AddInHttpRoute : IHttpRoute
    {
        public AddInHttpRoute(IAddInMetadata metadata, IHttpRoute addInRoute)
        {
            this.AddInMetadata = metadata; 
            this.InnerRoute = addInRoute;
        }

        public IAddInMetadata AddInMetadata { get; private set; }
        
        public IHttpRoute InnerRoute { get; private set; }

        protected string GetRoutePrefix()
        {
            return "api/" + this.AddInMetadata.PackageId + "/";
        }

        public IHttpRouteData GetRouteData(string virtualPathRoot, HttpRequestMessage request)
        {
            return this.InnerRoute.GetRouteData(virtualPathRoot + this.GetRoutePrefix(), request);
        }

        public IHttpVirtualPathData GetVirtualPath(HttpRequestMessage request, IDictionary<string, object> values)
        {
            // TODO fix this
            return this.InnerRoute.GetVirtualPath(request, values);
        }

        public string RouteTemplate { get { return this.GetRoutePrefix() + this.InnerRoute.RouteTemplate; } }
        public IDictionary<string, object> Defaults { get { return this.InnerRoute.Defaults; } }
        public IDictionary<string, object> Constraints { get { return this.InnerRoute.Constraints; } }
        public IDictionary<string, object> DataTokens { get { return this.InnerRoute.DataTokens; } }
        public HttpMessageHandler Handler { get { return this.InnerRoute.Handler; } }

    }
}
