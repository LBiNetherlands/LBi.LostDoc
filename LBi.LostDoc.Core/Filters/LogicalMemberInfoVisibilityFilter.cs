using System;
using System.Reflection;

namespace LBi.LostDoc.Core.Filters
{
    public class LogicalMemberInfoVisibilityFilter : MemberInfoFilter
    {
        public override bool Filter(IFilterContext context, MemberInfo m)
        {
            bool isPublic;
            bool isProtected;

            switch (m.MemberType)
            {
                case MemberTypes.TypeInfo:
                    return false;
                case MemberTypes.Constructor:
                    ConstructorInfo ctor = (ConstructorInfo)m;
                    isPublic = ctor.IsPublic;
                    isProtected = ctor.IsFamily;
                    break;
                case MemberTypes.Event:
                    EventInfo eInfo = (EventInfo)m;
                    MethodInfo emInfo = eInfo.GetAddMethod();
                    isPublic = emInfo.IsPublic;
                    isProtected = emInfo.IsFamily;
                    break;
                case MemberTypes.Field:
                    FieldInfo fInfo = (FieldInfo)m;
                    isPublic = fInfo.IsPublic;
                    isProtected = fInfo.IsFamily;
                    break;
                case MemberTypes.Method:
                    MethodInfo mInfo = (MethodInfo)m;
                    isPublic = mInfo.IsPublic;
                    isProtected = mInfo.IsFamily;
                    break;
                case MemberTypes.Property:
                    PropertyInfo pInfo = (PropertyInfo)m;
                    isPublic = pInfo.CanRead && pInfo.GetGetMethod(true).IsPublic ||
                               pInfo.CanWrite && pInfo.GetSetMethod(true).IsPublic;
                    isProtected = pInfo.CanRead && pInfo.GetGetMethod(true).IsFamily ||
                                  pInfo.CanWrite && pInfo.GetSetMethod(true).IsFamily;

                    break;
                case MemberTypes.NestedType:
                    Type nestedType = (Type)m;
                    isPublic = nestedType.IsNestedPublic;
                    isProtected = nestedType.IsNestedFamily;
                    break;
                default:
                    throw new ArgumentException("Unknown member type: " + m.MemberType);
            }

            if (!isPublic && !isProtected)
            {
                Type ifType;
                isPublic = IsExplicitInterfaceImplementation(m, out ifType) && ifType.IsPublic;
            }

            if (m.ReflectedType.IsSealed && !isPublic)
                return true;
            if (!isPublic && !isProtected)
                return true;

            return false;
        }

        private static bool IsExplicitInterfaceImplementation(MemberInfo m, out Type interfaceType)
        {
            bool ret = false;
            interfaceType = null;
            Type declaringType = m.ReflectedType;
            do
            {
                // check for explicit method impl
                foreach (var ifType in declaringType.GetInterfaces())
                {
                    var ifMap = declaringType.GetInterfaceMap(ifType);
                    int ix = Array.IndexOf(ifMap.TargetMethods, m);
                    ret = (ix >= 0);

                    if (ret)
                    {
                        interfaceType = ifType;
                        break;
                    }
                }

                if (!ret)
                    declaringType = declaringType.BaseType;
            } while (!ret && declaringType != null);

            return ret;
        }
    }
}