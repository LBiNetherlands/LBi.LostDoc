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
    public class AddInControllerFactory : IControllerFactory
    {
        private static readonly object _metadataKey = new object();
        private readonly string _areaName;

        private readonly CompositionContainer _container;
        private readonly MetadataContractBuilder<IController, IControllerMetadata> _importBuilder;
        private readonly object _key;
        private readonly IControllerFactory _nestedFactory;

        public AddInControllerFactory(string areaName, 
                                      CompositionContainer container, 
                                      IControllerFactory controllerFactory)
        {
            this._areaName = areaName;
            this._key = new object();
            this._container = container;
            this._nestedFactory = controllerFactory;
            this._importBuilder =
                new MetadataContractBuilder<IController, IControllerMetadata>(ContractNames.AdminController, 
                                                                              ImportCardinality.ExactlyOne, 
                                                                              CreationPolicy.NonShared);

            this._importBuilder.Add(
                (contract, meta) => StringComparer.OrdinalIgnoreCase.Equals(meta.Name, contract.Name));
            this._importBuilder.Add(
                (contract, meta) => StringComparer.Ordinal.Equals(meta.PackageId, contract.PackageId));
            this._importBuilder.Add(
                (contract, meta) => StringComparer.Ordinal.Equals(meta.PackageVersion, contract.PackageVersion));
        }

        public static object MetadataKey
        {
            get { return _metadataKey; }
        }

        public IController CreateController(RequestContext requestContext, string controllerName)
        {
            IController ret = null;

            if (StringComparer.OrdinalIgnoreCase.Equals(requestContext.RouteData.DataTokens["area"], this._areaName))
            {
                ImportDefinition importDefinition = this._importBuilder
                                                        .WithValue(c => c.Name, controllerName)
                                                        .WithValue(c => c.PackageId, 
                                                                   requestContext.RouteData.Values["packageId"])
                                                        .WithValue(c => c.PackageVersion, 
                                                                   requestContext.RouteData.Values["packageVersion"]);

                Export export = this._container.GetExports(importDefinition).SingleOrDefault();
                if (export != null)
                {
                    ret = (IController)export.Value;
                    requestContext.HttpContext.Items[this._key] = this;

                    // store this for later as we already went throuh the trouble of resolving the Export
                    requestContext.HttpContext.Items[MetadataKey] =
                        AttributedModelServices.GetMetadataView<IControllerMetadata>(export.Metadata);

                    // how bad is this?
                    ((Controller)ret).ActionInvoker = new AddInActionInvoker();
                }
            }

            if (ret == null)
            {
                ret = this._nestedFactory.CreateController(requestContext, controllerName);
                if (ret != null)
                    requestContext.HttpContext.Items[this._key] = this._nestedFactory;
            }

            return ret;
        }

        public SessionStateBehavior GetControllerSessionBehavior(RequestContext requestContext, string controllerName)
        {
            return SessionStateBehavior.Default;
        }

        public void ReleaseController(IController controller)
        {
            IControllerFactory factory = HttpContext.Current.Items[this._key] as IControllerFactory;
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