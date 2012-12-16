namespace Company.Project.Library
{
    public abstract class AbstractClassTest
    {
        public virtual string AStringProp { get; private set; }
        public abstract string AbstractStringPropProtSet { get; protected set; }
        public abstract string AbstractStringProp { get; set; }
        protected string ProtStringProp { get; set; }
    }
}