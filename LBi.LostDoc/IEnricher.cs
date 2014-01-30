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

using LBi.LostDoc.Primitives;

namespace LBi.LostDoc
{
    public interface IEnricher
    {
        void EnrichType(IProcessingContext context, IType typeAsset);

        void EnrichConstructor(IProcessingContext context, IConstructor ctorAsset);

        void EnrichAssembly(IProcessingContext context, IAssembly assemblyAsset);

        void RegisterNamespace(IProcessingContext context);

        void EnrichMethod(IProcessingContext context, IMethod methodAsset);

        void EnrichField(IProcessingContext context, IField fieldAsset);

        void EnrichProperty(IProcessingContext context, IProperty propertyAsset);

        void EnrichReturnValue(IProcessingContext context, IMethod methodAsset);

        void EnrichParameter(IProcessingContext context, Parameter parameter);

        void EnrichTypeParameter(IProcessingContext context, TypeParameter typeParameter);

        void EnrichNamespace(IProcessingContext context, INamespace namespaceAsset);

        void EnrichEvent(IProcessingContext context, IEvent eventAsset);
    }
}
