namespace Company.Project.Library
{
    /// <summary>
    /// Class that inherits from RegularClass
    /// </summary>
    public class InheritedRegularClass : RegularClass
    {
        /// <summary>
        /// Overridden foo
        /// </summary>
        public override void Foo()
        {
            base.Foo();
        }

        /// <summary>
        /// I'm a sealed override
        /// </summary>
        public override sealed void Dispose()
        {
            base.Dispose();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}