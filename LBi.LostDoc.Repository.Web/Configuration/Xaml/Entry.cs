using System.Windows.Markup;

namespace LBi.LostDoc.Repository.Web.Configuration.Xaml
{
    public abstract class Entry
    {
        public string Key { get; set; }

        public abstract object GetValue();
    }

    [ContentProperty("Value")]
    public class Entry<T> : Entry
    {
        public T Value { get; set; }

        public override object GetValue()
        {
            return this.Value;
        }
    }
}