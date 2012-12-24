/*
 * Copyright 2012 LBi Netherlands B.V.
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

namespace LBi.LostDoc.Core
{
    public interface IEnricher
    {
        void EnrichType(IProcessingContext context, Type type);

        void EnrichConstructor(IProcessingContext context, ConstructorInfo ctor);

        void EnrichAssembly(IProcessingContext context, Assembly asm);

        void RegisterNamespace(IProcessingContext context);

        void EnrichMethod(IProcessingContext context, MethodInfo mInfo);

        void EnrichField(IProcessingContext context, FieldInfo fieldInfo);

        void EnrichProperty(IProcessingContext context, PropertyInfo propertyInfo);

        void EnrichReturnValue(IProcessingContext context, MethodInfo methodInfo);

        void EnrichParameter(IProcessingContext context, ParameterInfo item);

        void EnrichTypeParameter(IProcessingContext context, Type typeParameter);

        void EnrichNamespace(IProcessingContext context, string ns);

        void EnrichEvent(IProcessingContext context, EventInfo eventInfo);
    }
}
