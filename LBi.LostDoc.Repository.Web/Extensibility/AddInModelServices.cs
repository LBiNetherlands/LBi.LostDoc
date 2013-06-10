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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Linq;
using LBi.LostDoc.Packaging.Composition;

namespace LBi.LostDoc.Repository.Web.Extensibility
{
    public static class AddInModelServices
    {
        public static IEnumerable<Lazy<Type, TMetadata>> FindExports<TMetadata>(this ComposablePartCatalog catalog, string contractName)
        {
            var allParts = catalog.Parts;
            var controllerParts = allParts.Where(part => part.ExportDefinitions.Any(ed => ed.ContractName == contractName));

            foreach (var partDefinition in controllerParts)
            {
                var exportDefinition = partDefinition.ExportDefinitions.Single(ed => ed.ContractName == contractName);
                TMetadata controllerMetadata = AttributedModelServices.GetMetadataView<TMetadata>(exportDefinition.Metadata);

                yield return new Lazy<Type, TMetadata>(() => AddInModelServices.GetPartType(partDefinition).Value, controllerMetadata);
            }
        }

        public static Lazy<Type> GetPartType(ComposablePartDefinition partDefinition)
        {
            AddInComposablePartDefinition addinPartDef = partDefinition as AddInComposablePartDefinition;

            if (addinPartDef == null)
                throw new ArgumentException("Must be of type AddInComposablePartDefinition", "partDefinition");

            return ReflectionModelServices.GetPartType(addinPartDef.Definition);
        }
    }
}