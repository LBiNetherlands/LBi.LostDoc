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
using System.Web.Mvc;
using System.Web.Routing;

namespace LBi.LostDoc.Repository.Web.Composition
{
    public class ControllerFactory : DefaultControllerFactory
    {
        private readonly CompositionContainer _container;

        public ControllerFactory(CompositionContainer container)
        {
            this._container = container;
        }
        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
        {
            var contractName = AttributedModelServices.GetContractName(controllerType);
            ImportDefinition importDefinition =
                new ImportDefinition(ed => ExportPredicate(controllerType, ed),
                                     contractName,
                                     ImportCardinality.ExactlyOne,
                                     true,
                                     false);
            return (IController)_container.GetExports(importDefinition).Single().Value;
        }

        private static bool ExportPredicate(Type controllerType, ExportDefinition ed)
        {
            return (string)ed.Metadata[CompositionConstants.ExportTypeIdentityMetadataName] ==
                   AttributedModelServices.GetTypeIdentity(controllerType);
        }
    }
}