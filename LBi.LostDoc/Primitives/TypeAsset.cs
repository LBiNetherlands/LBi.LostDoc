/*
 * Copyright 2014 DigitasLBi Netherlands B.V.
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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LBi.LostDoc.Primitives
{
    public abstract class TypeAsset : Asset
    {
        protected TypeAsset(AssetIdentifier id, object target) : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Type, "Invalid AssetIdentifier for TypeAsset");
        }

    }

    public abstract class AssemblyAsset : Asset
    {
        protected AssemblyAsset(AssetIdentifier id, object target) : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Assembly, "Invalid AssetIdentifier for AssemblyAsset");

        }
    }

    public abstract class NamespaceAsset : Asset
    {
        protected NamespaceAsset(AssetIdentifier id, object target) : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Namespace, "Invalid AssetIdentifier for NamespaceAsset");

        }
    }

    public abstract class MemberAsset : Asset
    {
        protected MemberAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
        }
    }

    public abstract class MethodAsset : MemberAsset
    {
        protected MethodAsset(AssetIdentifier id, object target) : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Method, "Invalid AssetIdentifier for MethodAsset");
        }
    }

    public abstract class PropertyAsset : MemberAsset
    {
        protected PropertyAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Property, "Invalid AssetIdentifier for PropertyAsset");
        }
    }

    public abstract class EventAsset : MemberAsset
    {
        protected EventAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Event, "Invalid AssetIdentifier for EventAsset");
        }
    }

    public abstract class OperatorAsset : MemberAsset
    {
        protected OperatorAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Method, "Invalid AssetIdentifier for OperatorAsset");
        }
    }
}
