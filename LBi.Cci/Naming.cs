using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Cci;

namespace LBi.Cci
{
    public static class Naming
    {
        public static string GetAssetId(string namespaceName)
        {
            return string.Format("N:{0}", namespaceName);
        }

        private static string GetTypeFullName(Type type)
        {
            if (type.IsNested)
            {
                return string.Format("{0}.{1}",
                                     GetTypeFullName(type.DeclaringType),
                                     type.Name);
            }

            if (type.IsGenericType)
                return string.Format("{0}.{1}", type.Namespace, type.Name);

            return type.FullName;
        }

        private static string GetTypeFullName(ITypeDefinition type)
        {
            Contract.Assert(!(type is Dummy));

            INestedTypeDefinition nestedType = type as INestedTypeDefinition;
            if (nestedType != null)
            {
                return string.Format("{0}.{1}",
                                     GetTypeFullName(nestedType.ContainingTypeDefinition),
                                     nestedType.Name);
            }

            INamespaceTypeDefinition namespaceTypeDef = type as INamespaceTypeDefinition;
            if (namespaceTypeDef != null)
            {
                return string.Format("{0}.{1}",
                                     GetNamespaceName(namespaceTypeDef.ContainingNamespace),
                                     namespaceTypeDef.Name.Value);
            }

            throw new InvalidOperationException("Unknowng ITypeDefinition: " + type.GetType().FullName);
        }

        private static string GetTypeName(INestedTypeDefinition type)
        {
            var ret = TypeHelper.GetTypeName(type, NameFormattingOptions.None);
            return ret;
            //if (type.MangleName)
            //    return type.MangledName;

            //return type.Name;
        }

        private static string GetNamespaceName(INamespaceDefinition namespaceDefinition)
        {
            INamespaceMember nestedNamespace = namespaceDefinition as INamespaceMember;

            if (nestedNamespace != null)
            {
                if (nestedNamespace.ContainingNamespace is IRootUnitNamespace)
                    return namespaceDefinition.Name.Value;

                return string.Format("{0}.{1}",
                                     GetNamespaceName(nestedNamespace.ContainingNamespace),
                                     namespaceDefinition.Name.Value);
            }

            return namespaceDefinition.Name.Value;
        }

        public static string GetAssetId(Type type)
        {
            return string.Format("T:{0}", GetTypeFullName(type));
        }

        public static string GetAssetId(ITypeDefinition type)
        {
            return string.Format("T:{0}", GetTypeFullName(type));
        }

        public static string GetAssetId(PropertyInfo propertyInfo)
        {
            ParameterInfo[] p = propertyInfo.GetIndexParameters();
            if (p.Length == 0)
                return string.Format("P:{0}.{1}", GetTypeFullName(propertyInfo.ReflectedType), propertyInfo.Name);

            return string.Format("P:{0}.{1}{2}",
                                 GetTypeFullName(propertyInfo.ReflectedType),
                                 propertyInfo.Name,
                                 CreateParameterSignature(propertyInfo, p.Select(pr => pr.ParameterType).ToArray()));
        }

        public static string GetAssetId(FieldInfo fieldInfo)
        {
            return string.Format("F:{0}.{1}", GetTypeFullName(fieldInfo.ReflectedType), fieldInfo.Name);
        }

        public static string GetAssetId(MethodInfo mInfo)
        {
            string ret = string.Format("M:{0}.{1}", GetTypeFullName(mInfo.ReflectedType), GetMethodName(mInfo));
            return ret;
        }

        public static string GetAssetId(ConstructorInfo mInfo)
        {
            return string.Format("M:{0}.{1}{2}",
                                 GetTypeFullName(mInfo.DeclaringType),
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

            if (!type.IsGenericParameter)
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
            else if (type.IsGenericParameter)
            {
                int ix = -1;
                if (type.DeclaringMethod != null)
                    ix = Array.IndexOf(type.DeclaringMethod.GetGenericArguments(), type);

                if (ix >= 0)
                    ret.Append("``").Append(ix);
                else
                    ret.Append('`').Append(Array.IndexOf(type.DeclaringType.GetGenericArguments(), type));
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
                    ret.Append("~").Append(CreateParameterSignature(null /* HACK: pass null to prevent stack overflow on infinite recursion */,
                                                                    new[] { declMethodBase.ReturnType },
                                                                    wrap: false /* don't wrap return var in parens */));
                }
            }

            return ret.ToString();
        }

        public static string GetAssetId(EventInfo eventInfo)
        {
            return string.Format("E:{0}.{1}", GetTypeFullName(eventInfo.ReflectedType), eventInfo.Name);
        }
    }
}