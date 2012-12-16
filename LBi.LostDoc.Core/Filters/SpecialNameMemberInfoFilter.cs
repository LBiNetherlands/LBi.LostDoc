using System;
using System.Reflection;

namespace LBi.LostDoc.Core.Filters
{
    public class SpecialNameMemberInfoFilter : MemberInfoFilter
    {
        public override bool Filter(IFilterContext context, MemberInfo m)
        {
            if (m is MethodInfo)
                return ((MethodInfo)m).IsSpecialName && !m.Name.StartsWith("op_", StringComparison.Ordinal);

            return false;
        }
    }
}