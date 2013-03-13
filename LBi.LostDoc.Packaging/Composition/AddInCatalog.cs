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
using System.Text;
using System.Threading.Tasks;

namespace LBi.LostDoc.Packaging.Composition
{
    public class AddInComposablePartDefinition : ComposablePartDefinition
    {
        private readonly ComposablePartDefinition _composablePartDefinition;
        //private readonly IDictionary<string, object> _metadata;
        private readonly ExportDefinition[] _exportDefinitions;

        public AddInComposablePartDefinition(AddInCatalog catalog, ComposablePartDefinition composablePartDefinition)
        {
            this._composablePartDefinition = composablePartDefinition;

            //// TODO is this actually the right place for this? NO, doesn't seem like it is
            //this._metadata = new Dictionary<string, object>(composablePartDefinition.Metadata);
            //this._metadata.Add(AddInCatalog.PackageIdMetadataName, catalog.PackageId);
            //this._metadata.Add(AddInCatalog.PackageVersionMetadataName, catalog.PackageVersion);

            // maybe here
            List<KeyValuePair<string, object>> injectedMetadata = new List<KeyValuePair<string, object>>();
            injectedMetadata.Add(new KeyValuePair<string, object>(AddInCatalog.PackageIdMetadataName, catalog.PackageId));
            injectedMetadata.Add(new KeyValuePair<string, object>(AddInCatalog.PackageVersionMetadataName, catalog.PackageVersion));

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

        public override ComposablePart CreatePart()
        {
            return this._composablePartDefinition.CreatePart();
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
    }


    public class AddInCatalog : ComposablePartCatalog, INotifyComposablePartCatalogChanged
    {
        public const string PackageIdMetadataName = "PackageId";
        public const string PackageVersionMetadataName = "PackageVersion";

        public AddInCatalog(ComposablePartCatalog innerCatalog, string packageId, string packageVersion) : base()
        {
            this.InnerCatalog = innerCatalog;
            this.PackageId = packageId;
            this.PackageVersion = packageVersion;

            INotifyComposablePartCatalogChanged notify = innerCatalog as INotifyComposablePartCatalogChanged;
            if (notify != null)
            {
                notify.Changed += (sender, args) => this.OnChanged(args);
                notify.Changing += (sender, args) => this.OnChanging(args);
            }

        }

        protected ComposablePartCatalog InnerCatalog { get; set; }

        public string PackageVersion { get; protected set; }

        public string PackageId { get; protected set; }


        public override IQueryable<ComposablePartDefinition> Parts
        {
            get
            {
                return this.InnerCatalog.Parts.Select(this.InjectMetadata).AsQueryable();
            }
        }

        protected virtual ComposablePartDefinition InjectMetadata(ComposablePartDefinition arg)
        {
            return new AddInComposablePartDefinition(this, arg);
        }

        public event EventHandler<ComposablePartCatalogChangeEventArgs> Changed;

        protected virtual void OnChanged(ComposablePartCatalogChangeEventArgs e)
        {
            EventHandler<ComposablePartCatalogChangeEventArgs> handler = this.Changed;
            if (handler != null) handler(this, e);
        }

        public event EventHandler<ComposablePartCatalogChangeEventArgs> Changing;

        protected virtual void OnChanging(ComposablePartCatalogChangeEventArgs e)
        {
            EventHandler<ComposablePartCatalogChangeEventArgs> handler = this.Changing;
            if (handler != null) handler(this, e);
        }


    }
}
