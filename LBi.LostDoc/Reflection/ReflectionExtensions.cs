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
using System.Linq;
using System.Reflection;

namespace LBi.LostDoc.Reflection
{
    internal static class ReflectionExtensions
    {
        public static IEnumerable<Type> GetImplementedInterfaces(this Type type)
        {
            foreach (Type interfaceType in type.GetInterfaces())
            {
                InterfaceMapping mapping = type.GetInterfaceMap(interfaceType);

                if (mapping.TargetMethods.Any(mi => mi.DeclaringType == type))
                {
                    Type ifType = interfaceType.IsGenericType
                                      ? interfaceType.GetGenericTypeDefinition()
                                      : interfaceType;

                    yield return ifType;
                }
            }
        }
    }
}