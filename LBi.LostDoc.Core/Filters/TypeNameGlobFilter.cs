using System;
using System.Text.RegularExpressions;

namespace LBi.LostDoc.Core.Filters
{
    public class TypeNameGlobFilter : TypeFilter
    {
        private string _filter;
        private Regex _regex;

        public string Include
        {
            get { return this._filter; }
            set
            {
                this._filter = value;
                string pattern = this._filter.Replace("**", @"[^\.]+(\.[^\.]+)*");
                pattern = pattern.Replace("*", @"[^\.]+");
                this._regex = new Regex(pattern, RegexOptions.Compiled);
            }
        }

        protected override bool Filter(IFilterContext context, Type type)
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
                return false;

            return !this._regex.IsMatch(type.FullName);
        }
    }
}