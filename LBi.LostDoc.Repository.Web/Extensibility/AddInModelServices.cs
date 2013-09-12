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
using System.ComponentModel.Composition.Hosting;
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

                yield return new Lazy<Type, TMetadata>(() => ReflectionModelServices.GetPartType(partDefinition).Value, controllerMetadata);
            }
        }

        public static bool TryGetExport<TContract, TExport>(this CompositionContainer container, out TExport export)
        {
            return container.TryGetExport<TExport>(typeof(TContract), typeof(TExport), out export);
        }

        public static bool TryGetExport<TExport>(this CompositionContainer container, out TExport export)
        {
            return container.TryGetExport<TExport, TExport>(out export);
        }

        public static bool TryGetExport<TExportBase>(this CompositionContainer container, Type contracType, Type exportType, out TExportBase export)
        {
            var contractName = AttributedModelServices.GetContractName(contracType);
            ImportDefinition importDefinition =
                new ImportDefinition(ed => TypeIdentityPredicate(exportType, ed),
                                     contractName,
                                     ImportCardinality.ExactlyOne,
                                     true,
                                     false);

            IEnumerable<Export> exports;
            bool ret = container.TryGetExports(importDefinition, new AtomicComposition(), out exports);
            if (ret)
                export = (TExportBase)exports.First().Value;
            else
                export = default(TExportBase);

            return ret;
        }

        public static bool TryGetExport<TExportBase>(this CompositionContainer container, Type exportType, out TExportBase export)
        {
            return container.TryGetExport<TExportBase>(exportType, exportType, out export);
        }

        private static bool TypeIdentityPredicate(Type explicitType, ExportDefinition ed)
        {
            return (string)ed.Metadata[CompositionConstants.ExportTypeIdentityMetadataName] ==
                   AttributedModelServices.GetTypeIdentity(explicitType);
        }
    }
}