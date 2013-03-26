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
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;

namespace LBi.LostDoc.Packaging.Composition
{
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

        public event EventHandler<ComposablePartCatalogChangeEventArgs> Changed;

        public event EventHandler<ComposablePartCatalogChangeEventArgs> Changing;

        public string PackageId { get; protected set; }

        public string PackageVersion { get; protected set; }

        public override IQueryable<ComposablePartDefinition> Parts
        {
            get { return this.InnerCatalog.Parts.Select(this.InjectMetadata).AsQueryable(); }
        }

        protected ComposablePartCatalog InnerCatalog { get; set; }

        protected virtual ComposablePartDefinition InjectMetadata(ComposablePartDefinition arg)
        {
            return new AddInComposablePartDefinition(this, arg);
        }

        protected virtual void OnChanged(ComposablePartCatalogChangeEventArgs e)
        {
            EventHandler<ComposablePartCatalogChangeEventArgs> handler = this.Changed;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnChanging(ComposablePartCatalogChangeEventArgs e)
        {
            EventHandler<ComposablePartCatalogChangeEventArgs> handler = this.Changing;
            if (handler != null) handler(this, e);
        }
    }
}