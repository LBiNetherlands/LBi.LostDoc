/*
 * Copyright 2013 LBi Netherlands B.V.
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

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Routing;

namespace LBi.LostDoc.Repository.Web.Extensibility.Http
{
    public class AddInHttpRouteRewriter : HttpRouteCollection
    {
        private readonly IAddInMetadata _metadata;
        private readonly HttpRouteCollection _innerCollection;

        public AddInHttpRouteRewriter(HttpRouteCollection routeCollection, IAddInMetadata metadata)
        {
            this._metadata = metadata;
            this._innerCollection = routeCollection;
        }

        protected virtual string NamePrefix
        {
         get { return this._metadata.PackageId + '/' + this._metadata.PackageVersion + '/'; }
        }

        public override void Add(string name, IHttpRoute route)
        {
            this._innerCollection.Add(this.NamePrefix + name, new AddInHttpRoute(this._metadata, route));
        }

        public override void Clear()
        {
            this._innerCollection.Clear();
        }

        public override bool Contains(IHttpRoute item)
        {
            return this._innerCollection.OfType<AddInHttpRoute>().Any(r => r.InnerRoute == item);
        }

        public override bool ContainsKey(string name)
        {
            return this._innerCollection.ContainsKey(this.NamePrefix + name);
        }

        public override void CopyTo(IHttpRoute[] array, int arrayIndex)
        {
            this._innerCollection.CopyTo(array, arrayIndex);
        }

        public override void CopyTo(KeyValuePair<string, IHttpRoute>[] array, int arrayIndex)
        {
            this._innerCollection.CopyTo(array, arrayIndex);
        }

        public override int Count
        {
            get { return this._innerCollection.Count; }
        }

        public override IHttpRoute CreateRoute(string routeTemplate,
                                               IDictionary<string, object> defaults,
                                               IDictionary<string, object> constraints,
                                               IDictionary<string, object> dataTokens,
                                               HttpMessageHandler handler)
        {
            return this._innerCollection.CreateRoute(routeTemplate, defaults, constraints, dataTokens, handler);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                this._innerCollection.Dispose();

            base.Dispose(disposing);
        }

        public override IEnumerator<IHttpRoute> GetEnumerator()
        {
            return this._innerCollection.GetEnumerator();
        }

        public override IHttpVirtualPathData GetVirtualPath(HttpRequestMessage request,
                                                            string name,
                                                            IDictionary<string, object> values)
        {
            return this._innerCollection.GetVirtualPath(request, this.NamePrefix + name, values);
        }

        public override void Insert(int index, string name, IHttpRoute value)
        {
            this._innerCollection.Insert(index, this.NamePrefix + name, new AddInHttpRoute(this._metadata, value));
        }

        public override bool IsReadOnly
        {
            get { return this._innerCollection.IsReadOnly; }
        }

        protected override System.Collections.IEnumerator OnGetEnumerator()
        {
            return this._innerCollection.GetEnumerator();
        }

        public override bool Remove(string name)
        {
            return this._innerCollection.Remove(this.NamePrefix + name);
        }

        public override IHttpRoute this[int index]
        {
            get { return this._innerCollection[index]; }
        }

        public override IHttpRoute this[string name]
        {
            get { return this._innerCollection[this.NamePrefix + name]; }
        }

        public override bool TryGetValue(string name, out IHttpRoute route)
        {
            return this._innerCollection.TryGetValue(this.NamePrefix + name, out route);
        }

        public override string VirtualPathRoot
        {
            get { return this._innerCollection.VirtualPathRoot; }
        }

        public override IHttpRouteData GetRouteData(HttpRequestMessage request)
        {
            return this._innerCollection.GetRouteData(request);
        }
    }
}