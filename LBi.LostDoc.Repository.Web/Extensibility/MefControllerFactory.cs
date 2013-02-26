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

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.SessionState;
using LBi.LostDoc.Composition;

namespace LBi.LostDoc.Repository.Web.Extensibility
{

    public class MefControllerFactory : IControllerFactory
    {
        private readonly CompositionContainer _container;
        private readonly IControllerFactory _nestedFactory;

        public MefControllerFactory(CompositionContainer container, IControllerFactory controllerFactory)
        {
            this._container = container;
            this._nestedFactory = controllerFactory;
        }

        public IController CreateController(RequestContext requestContext, string controllerName)
        {
            IController ret;

            var importBuilder = new MetadataContractBuilder<IController, IControllerMetadata>(ImportCardinality.ExactlyOne, CreationPolicy.NonShared);
            importBuilder.Add(m => StringComparer.OrdinalIgnoreCase.Equals(m.Name, controllerName));
            ImportDefinition importDefinition = importBuilder.GetImportDefinition(); 

            Export export = this._container.GetExports(importDefinition).SingleOrDefault();
            if (export != null)
            {
                ret = (IController) export.Value;
                requestContext.HttpContext.Items[this] = this;
            }
            else
            {
                ret = this._nestedFactory.CreateController(requestContext, controllerName);
                if (ret != null)
                    requestContext.HttpContext.Items[this] = this._nestedFactory;
            }



            return ret;
        }

        public SessionStateBehavior GetControllerSessionBehavior(RequestContext requestContext, string controllerName)
        {
            return SessionStateBehavior.Default;
        }

        public void ReleaseController(IController controller)
        {
            IControllerFactory factory = HttpContext.Current.Items[this] as IControllerFactory;
            if (factory == this)
            {
                IDisposable disposable = controller as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }
            else if (factory != null)
                factory.ReleaseController(controller);
        }
    }
}