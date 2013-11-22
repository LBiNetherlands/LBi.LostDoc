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
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Web.Http.Dependencies;

namespace LBi.LostDoc.Repository.Web.Extensibility.Http
{
    public class MefDependencyResolver : IDependencyResolver
    {
        private readonly CompositionContainer _container;
        private readonly IDependencyResolver _innerResolver;

        public MefDependencyResolver(CompositionContainer container, IDependencyResolver innerResolver)
        {
            this._container = container;
            this._innerResolver = innerResolver;
        }

        public IDependencyScope BeginScope()
        {
            return this;
        }

        public object GetService(Type serviceType)
        {
            var export = this._container.GetExports(serviceType, null, null).SingleOrDefault();

            return null != export ? export.Value : this._innerResolver.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            IEnumerable<Lazy<object, object>> exports = _container.GetExports(serviceType, null, null);
            return exports.Select(v => v.Value).Concat(this._innerResolver.GetServices(serviceType));
        }

        public void Dispose()
        {
            this._innerResolver.Dispose();
        }
    }
}