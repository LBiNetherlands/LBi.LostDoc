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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace LBi.LostDoc.Repository.Web.Extensibility.Http
{
    [Export(typeof(IHttpControllerActivator))]
    public class MefHttpControllerActivator : IHttpControllerActivator
    {
        private readonly CompositionContainer _container;

        [ImportingConstructor]
        public MefHttpControllerActivator(CompositionContainer container)
        {
            this._container = container;
        }

        public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
        {
            Lazy<object, object> controller = this._container.GetExports(controllerType, null, null).FirstOrDefault();
            
            if (controller == null)
                return null;

            return controller.Value as IHttpController;
        }
    }
}