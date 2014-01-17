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

namespace LBi.LostDoc
{
    public class Asset : IEquatable<Asset>
    {
        public Asset(AssetIdentifier id, object target)
        {
            this.Id = id;
            this.Target = target;
        }
        public virtual string Name { get { return null; } }

        public object Target { get; private set; }

        public AssetIdentifier Id { get; private set; }

        public AssetType Type { get { return this.Id.Type; } }

        public void Visit(IAssetVisitor visitor)
        {
            switch (this.Type)
            {
                case AssetType.Unknown:
                    visitor.VisitUnknown(this);
                    break;
                case AssetType.Namespace:
                    visitor.VisitNamespace(this);
                    break;
                case AssetType.Type:
                    visitor.VisitType(this);
                    break;
                case AssetType.Method:
                    visitor.VisitMethod(this);
                    break;
                case AssetType.Field:
                    visitor.VisitField(this);
                    break;
                case AssetType.Event:
                    visitor.VisitEvent(this);
                    break;
                case AssetType.Property:
                    visitor.VisitProperty(this);
                    break;
                case AssetType.Assembly:
                    visitor.VisitAssembly(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool Equals(Asset other)
        {
            return other != null && other.Id.Equals(this.Id);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Asset);
        }

        public override string ToString()
        {
            return this.Id.ToString(includeVersion: true);
        }
    }
}