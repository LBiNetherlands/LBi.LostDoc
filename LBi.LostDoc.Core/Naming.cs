using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LBi.LostDoc.Core
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
                return string.Format("{0}.{1}", GetTypeName(type.DeclaringType), type.Name);

            if (type.IsGenericType)
                return string.Format("{0}.{1}", type.Namespace, type.Name);

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
            return string.Format("F:{0}.{1}", GetTypeName(fieldInfo.DeclaringType), fieldInfo.Name);
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
                                 mInfo.Name.Replace('.', '#'),
                                 CreateParameterSignature(mInfo,
                                                          mInfo.GetParameters().Select(p => p.ParameterType).ToArray()));
        }

        private static string GetMethodName(MethodBase mInfo)
        {
            string ret;
            if (mInfo.IsGenericMethod)
            {
                ret = string.Format("{0}{1}{2}",
                                    mInfo.Name.Replace('.', '#'),
                                    "``" + mInfo.GetGenericArguments().Length.ToString(),
                                    CreateParameterSignature(
                                                             mInfo,
                                                             mInfo.GetParameters().Select(p => p.ParameterType).ToArray()));
            }
            else
            {
                ret = string.Format("{0}{1}",
                                    mInfo.Name.Replace('.', '#'),
                                    CreateParameterSignature(
                                                             mInfo,
                                                             mInfo.GetParameters().Select(p => p.ParameterType).ToArray()));
            }

            return ret;
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

                if (t.IsGenericType)
                {
                    int pos = t.Name.LastIndexOf('`');
                    if (pos >= 0)
                        ret.AppendFormat("{0}.{1}", t.Namespace, t.Name.Substring(0, pos));
                    else // this is prob wrong
                        ret.AppendFormat("{0}.{1}", t.Namespace, t.Name);
                }
                else if (t.IsGenericParameter)
                {
                    int ix = -1;
                    if (t.DeclaringMethod != null)
                        ix = Array.IndexOf(t.DeclaringMethod.GetGenericArguments(), t);

                    if (ix >= 0)
                        ret.Append("``").Append(ix);
                    else
                        ret.Append('`').Append(Array.IndexOf(t.DeclaringType.GetGenericArguments(), t));
                }
                else
                    ret.Append(t.FullName);

                if (t.IsGenericType)
                {
                    ret.Append('{').Append(CreateParameterSignature(declaringMember, t.GetGenericArguments(),
                                                                    wrap: false)).Append('}');
                }

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
                                                                    new[] {declMethodBase.ReturnType},
                                                                    wrap: false /* don't wrap return var in parens */));
                }
            }

            return ret.ToString();
        }

        public static string GetAssetId(EventInfo eventInfo)
        {
            return string.Format("E:{0}.{1}", GetTypeName(eventInfo.DeclaringType), eventInfo.Name);
        }
    }
}