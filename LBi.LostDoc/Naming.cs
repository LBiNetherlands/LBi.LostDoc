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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LBi.LostDoc
{
    public class Naming
    {
        public static string GetAssetId(string namespaceName)
        {
            return string.Format("N:{0}", namespaceName);
        }

        private static string GetTypeName(Type type)
        {
            if (type.IsNested)
            {
                return string.Format("{0}.{1}",
                                     GetTypeName(type.DeclaringType),
                                     type.Name);
            }

            if (type.IsGenericType)
            {
                if (type.Namespace != null)
                    return string.Format("{0}.{1}", type.Namespace, type.Name);
                
                return type.Name;
            }

            return type.FullName;
        }


        public static string GetAssetId(Type type)
        {
            if (type.IsGenericParameter) // TODO add extra guard here
                throw new InvalidOperationException("Type is generic parameter.");

            return string.Format("T:{0}", GetTypeName(type));
        }

        public static string GetAssetId(PropertyInfo propertyInfo)
        {
            ParameterInfo[] p = propertyInfo.GetIndexParameters();
            if (p.Length == 0)
                return string.Format("P:{0}.{1}", GetTypeName(propertyInfo.ReflectedType), propertyInfo.Name);

            return string.Format("P:{0}.{1}{2}",
                                 GetTypeName(propertyInfo.ReflectedType),
                                 propertyInfo.Name,
                                 CreateParameterSignature(propertyInfo, p.Select(pr => pr.ParameterType).ToArray()));
        }

        public static string GetAssetId(FieldInfo fieldInfo)
        {
            return string.Format("F:{0}.{1}", GetTypeName(fieldInfo.ReflectedType), fieldInfo.Name);
        }

        public static string GetAssetId(MethodInfo mInfo)
        {
            string ret = string.Format("M:{0}.{1}", GetTypeName(mInfo.ReflectedType), GetMethodName(mInfo));
            return ret;
        }

        public static string GetAssetId(ConstructorInfo mInfo)
        {
            return string.Format("M:{0}.{1}{2}",
                                 GetTypeName(mInfo.DeclaringType),
                                 GetExplicitName(mInfo.Name),
                                 CreateParameterSignature(mInfo,
                                                          mInfo.GetParameters().Select(p => p.ParameterType).ToArray()));
        }

        /// <summary>
        /// An explicitly implemented interface in a generic class can have a method name such as:
        /// <![CDATA[
        /// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Add
        /// ]]>
        /// csc generates the following:
        /// M:ConsoleApplication20.Test`2.System#Collections#Generic#ICollection{System#Collections#Generic#KeyValuePair{T@V}}#Add(System.Collections.Generic.KeyValuePair{`0,`1})
        /// </summary>
        /// <param name="explicitPart"></param>
        /// <returns></returns>
        private static string GetExplicitName(String explicitPart)
        {
            return explicitPart.Replace('<', '{')
                               .Replace('>', '}')
                               .Replace(',', '@')
                               .Replace('.', '#');
        }

        private static string GetMethodName(MethodBase mInfo)
        {
            string ret;
            if (mInfo.IsGenericMethod)
            {
                ret = string.Format("{0}{1}{2}",
                                    GetExplicitName(mInfo.Name), // mInfo.Name.Replace('.', '#'),
                                    "``" + mInfo.GetGenericArguments().Length.ToString(),
                                    CreateParameterSignature(
                                                             mInfo,
                                                             mInfo.GetParameters().Select(p => p.ParameterType).ToArray()));
            }
            else
            {
                ret = string.Format("{0}{1}",
                                    GetExplicitName(mInfo.Name), // mInfo.Name.Replace('.', '#'),
                                    CreateParameterSignature(
                                                             mInfo,
                                                             mInfo.GetParameters().Select(p => p.ParameterType).ToArray()));
            }

            return ret;
        }


        private static string CreateParameterTypeSignature(MemberInfo declaringMember, Type type)
        {
            StringBuilder ret = new StringBuilder();

            if (!type.IsGenericParameter && (!type.IsArray || !type.GetElementType().IsGenericParameter))
            {
                if (type.IsNested)
                {
                    ret.Append(CreateParameterTypeSignature(declaringMember, type.DeclaringType))
                       .Append('.');
                }
                else if (!string.IsNullOrEmpty(type.Namespace))
                {
                    ret.Append(type.Namespace)
                       .Append('.');
                }
            }

            if (type.IsGenericType)
            {
                int pos = type.Name.LastIndexOf('`');
                if (pos >= 0)
                    ret.Append(type.Name.Substring(0, pos));
                else
                    ret.Append(type.Name);
            }
            else if (type.IsGenericParameter || type.IsArray && type.GetElementType().IsGenericParameter)
            {
                Type realType = type.IsArray ? type.GetElementType() : type;
                int ix = -1;
                if (realType.DeclaringMethod != null)
                    ix = Array.IndexOf(realType.DeclaringMethod.GetGenericArguments(), realType);

                if (ix >= 0)
                    ret.Append("``").Append(ix);
                else
                    ret.Append('`').Append(Array.IndexOf(realType.DeclaringType.GetGenericArguments(), realType));

                if (type.IsArray)
                {
                    ret.Append('[');
                    if (type.GetArrayRank() > 1)
                    {
                        for (int rank = 1; rank <= type.GetArrayRank(); rank++)
                        {
                            if (rank > 1)
                                ret.Append(',');

                            // TODO figure out if we can actually get a lower bound and size for the array
                            ret.Append("0:");
                        }
                    }
                    ret.Append(']');
                }
            }
            else
                ret.Append(type.Name);


            if (type.IsGenericType)
            {
                Type[] genArgs = type.GetGenericArguments();

                if (type.IsNested)
                {
                    HashSet<Type> genericArguments = new HashSet<Type>(genArgs);

                    Type declaringType = type.DeclaringType;

                    while (declaringType != null)
                    {
                        if (declaringType.IsGenericType)
                        {
                            foreach (var genArg in declaringType.GetGenericArguments())
                                genericArguments.Remove(genArg);
                        }

                        declaringType = declaringType.DeclaringType;
                    }

                    genArgs = genericArguments.ToArray();
                }
                ret.Append('{')
                   .Append(CreateParameterSignature(declaringMember, genArgs, wrap: false))
                   .Append('}');
            }

            return ret.ToString();
        }

        private static string CreateParameterSignature(MemberInfo declaringMember, Type[] parameters, bool wrap = true)
        {
            StringBuilder ret = new StringBuilder();
            if (parameters.Length > 0 && wrap)
                ret.Append('(');
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                    ret.Append(',');

                Type t = parameters[i];

                if (t.IsByRef)
                    t = t.GetElementType();

                ret.Append(CreateParameterTypeSignature(declaringMember, t));

                if (parameters[i].IsByRef)
                    ret.Append('@');
            }


            if (parameters.Length > 0 && wrap)
            {
                ret.Append(')');
                MethodInfo declMethodBase = declaringMember as MethodInfo;
                if (declMethodBase != null &&
                    declMethodBase.IsSpecialName &&
                    declMethodBase.IsStatic &&
                    declMethodBase.IsPublic &&
                    (declMethodBase.Name == "op_Implicit" ||
                     declMethodBase.Name == "op_Explicit"))
                {
                    ret.Append("~").Append(CreateParameterSignature(
                                                                    null /* HACK: pass null to prevent stack overflow on infinite recursion */,
                                                                    new[] { declMethodBase.ReturnType },
                                                                    wrap: false /* don't wrap return var in parens */));
                }
            }

            return ret.ToString();
        }

        public static string GetAssetId(EventInfo eventInfo)
        {
            return string.Format("E:{0}.{1}", GetTypeName(eventInfo.ReflectedType), eventInfo.Name);
        }
    }
}
