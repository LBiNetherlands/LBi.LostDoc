namespace Company.Project.Library
{
    /// <summary>
    /// Access modifier tests
    /// </summary>
    public class AccessModifierTests
    {
        public string AStringField;

        public string AStringProp { get; private set; }

        protected internal string ProtIntStringProp { get; internal set; }

        protected internal void ProtOrInt()
        {
        }

        protected internal virtual void ProtOrIntVirt()
        {
        }

        internal void Int()
        {
        }

        protected void Prot()
        {
        }

        public void Publ()
        {
        }

        private void Priv()
        {
        }

        public static void PublStatic()
        {
        }

        public virtual void PublVirt()
        {
        }
    }
}