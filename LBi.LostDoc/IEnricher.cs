/*
 * Copyright 2012-2014 DigitasLBi Netherlands B.V.
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
using System.Reflection;

namespace LBi.LostDoc
{
    public interface IEnricher
    {
        void EnrichType(IProcessingContext context, Asset typeAsset);

        void EnrichConstructor(IProcessingContext context, Asset ctorAsset);

        void EnrichAssembly(IProcessingContext context, Asset assemblyAsset);

        void RegisterNamespace(IProcessingContext context);

        void EnrichMethod(IProcessingContext context, Asset methodAsset);

        void EnrichField(IProcessingContext context, Asset fieldAsset);

        void EnrichProperty(IProcessingContext context, Asset propertyAsset);

        void EnrichReturnValue(IProcessingContext context, Asset methodAsset);

        void EnrichParameter(IProcessingContext context, Asset methodAsset, string parameterName);

        void EnrichTypeParameter(IProcessingContext context, Asset typeOrMethodAsset, string name);

        void EnrichNamespace(IProcessingContext context, Asset namespaceAsset);

        void EnrichEvent(IProcessingContext context, Asset eventAsset);
    }
}
