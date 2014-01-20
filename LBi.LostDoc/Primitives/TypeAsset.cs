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
using System.Diagnostics.Contracts;

namespace LBi.LostDoc.Primitives
{
    public abstract class TypeAsset : Asset
    {
        protected TypeAsset(AssetIdentifier id, object target) : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Type, "Invalid AssetIdentifier for TypeAsset");
        }

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitType(this);
        }
    }
}
