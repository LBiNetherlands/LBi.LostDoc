namespace Company.Project.Library
{
    /// <summary>
    /// Class that inherits GenericClass[string]
    /// </summary>
    public class InheritedGenericClass : GenericClass<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InheritedGenericClass"/> class. Creates anew instance given a <see cref="string"/>
        /// </summary>
        /// <param name="str">
        /// </param>
        public InheritedGenericClass(string str)
            : base(str)
        {
        }
    }
}