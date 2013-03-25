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
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.ReflectionModel;
using System.Linq;
using System.Web.Http.Dispatcher;

namespace LBi.LostDoc.Repository.Web.Extensibility
{
    public class AddInHttpControllerTypeResolver : DefaultHttpControllerTypeResolver
    {
        private readonly CompositionContainer _container;

        public AddInHttpControllerTypeResolver(CompositionContainer container)
        {
            this._container = container;
        }

        public override ICollection<Type> GetControllerTypes(IAssembliesResolver assembliesResolver)
        {
            var ret = base.GetControllerTypes(assembliesResolver);

            var parts = _container.Catalog.Parts.Where(
                partDef =>
                partDef.ExportDefinitions.Any(
                    p => string.Equals(p.ContractName, ContractNames.ApiController, StringComparison.Ordinal)));

            foreach (var composablePartDefinition in parts)
            {
                // TODO this isn't fantastic as it makes a lot of assumption about the CPD
                var lazyType = ReflectionModelServices.GetPartType(composablePartDefinition);
                ret.Add(lazyType.Value);
            }

            return ret;
        }
    }
}