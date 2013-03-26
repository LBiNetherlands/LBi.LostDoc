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
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Linq;

namespace LBi.LostDoc.Packaging.Composition
{
    public class AddInComposablePartDefinition : ComposablePartDefinition
    {
        private readonly ComposablePartDefinition _composablePartDefinition;

        private readonly ExportDefinition[] _exportDefinitions;

        public AddInComposablePartDefinition(AddInCatalog catalog, ComposablePartDefinition composablePartDefinition)
        {
            this._composablePartDefinition = composablePartDefinition;

            List<KeyValuePair<string, object>> injectedMetadata = new List<KeyValuePair<string, object>>();
            injectedMetadata.Add(new KeyValuePair<string, object>(AddInCatalog.PackageIdMetadataName, catalog.PackageId));
            injectedMetadata.Add(new KeyValuePair<string, object>(AddInCatalog.PackageVersionMetadataName, 
                                                                  catalog.PackageVersion));

            List<ExportDefinition> interceptedExports = new List<ExportDefinition>();

            foreach (ExportDefinition export in composablePartDefinition.ExportDefinitions)
            {
                ICompositionElement compositionElement = export as ICompositionElement;
                if (compositionElement == null)
                    throw new InvalidOperationException("ExportDefinition doesn't implement ICompositionElement");

                Dictionary<string, object> metadata = injectedMetadata.Concat(export.Metadata)
                                                                      .ToDictionary(kvp => kvp.Key, 
                                                                                    kvp => kvp.Value);

                // TODO this will fail if export isn't a ReflectionMemberExportDefinition (Internal, so I can't check)
                LazyMemberInfo lazyMember = ReflectionModelServices.GetExportingMember(export);

                ExportDefinition interceptedExport =
                    ReflectionModelServices.CreateExportDefinition(lazyMember, 
                                                                   export.ContractName, 
                                                                   new Lazy<IDictionary<string, object>>(() => metadata), 
                                                                   compositionElement.Origin);
                interceptedExports.Add(interceptedExport);
            }

            this._exportDefinitions = interceptedExports.ToArray();
        }

        public ComposablePartDefinition Definition
        {
            get { return this._composablePartDefinition; }
        }

        public override IEnumerable<ExportDefinition> ExportDefinitions
        {
            get { return this._exportDefinitions; }
        }

        public override IEnumerable<ImportDefinition> ImportDefinitions
        {
            get { return this._composablePartDefinition.ImportDefinitions; }
        }

        public override IDictionary<string, object> Metadata
        {
            get { return this._composablePartDefinition.Metadata; }
        }

        public override ComposablePart CreatePart()
        {
            return this._composablePartDefinition.CreatePart();
        }
    }
}