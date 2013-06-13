using System;
using System.Windows.Markup;

namespace LBi.LostDoc.Repository.Web.Configuration.Xaml
{
    public abstract class Entry
    {
        public string Key { get; set; }

        public abstract Type Type { get; }

        public abstract object GetValue();
    }

    [ContentProperty("Value")]
    public class Entry<T> : Entry
    {
        public T Value { get; set; }

        public override Type Type
        {
            get { return typeof(T); }
        }

        public override object GetValue()
        {
            return this.Value;
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1}", typeof(T).FullName, this.Value);
        }
    }
}