/*
 * Copyright 2012 DigitasLBi Netherlands B.V.
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

using System.Web;
using System.Web.Routing;

namespace LBi.LostDoc.Repository.Web
{
    public class RedirectRouteHandler : IRouteHandler, IHttpHandler
    {
        private readonly string _routeName;

        public RedirectRouteHandler(string routeName)
        {
            this._routeName = routeName;
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return this;
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.RedirectToRoute(this._routeName);
        }
    }
}