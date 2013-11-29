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
using System.Collections.Generic;

namespace LBi.LostDoc.Reflection
{
    public class ReflectionExplorer : IAssetExplorer, IAssetVisitor
    {
        public IEnumerable<Asset> Discover(Asset root)
        {
            return null;
        }

        void IAssetVisitor.VisitAssembly(Asset asset)
        {
            throw new NotImplementedException();
        }

        void IAssetVisitor.VisitNamespace(Asset asset)
        {
            throw new NotImplementedException();
        }

        void IAssetVisitor.VisitType(Asset asset)
        {
            throw new NotImplementedException();
        }

        void IAssetVisitor.VisitField(Asset asset)
        {
            throw new NotImplementedException();
        }

        void IAssetVisitor.VisitEvent(Asset asset)
        {
            throw new NotImplementedException();
        }

        void IAssetVisitor.VisitProperty(Asset asset)
        {
            throw new NotImplementedException();
        }

        void IAssetVisitor.VisitUnknown(Asset asset)
        {
            throw new NotImplementedException();
        }

        void IAssetVisitor.VisitMethod(Asset asset)
        {
            throw new NotImplementedException();
        }
    }
}