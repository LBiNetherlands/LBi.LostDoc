using System;

namespace LBi.LostDoc.Core.Filters
{
    public class PublicTypeFilter : TypeFilter
    {
        protected override bool Filter(IFilterContext context, Type t)
        {
            return !t.IsPublic && (!t.IsNested || !t.IsNestedPublic);
        }
    }
}