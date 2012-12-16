using Company.Project.Library;

namespace Company.Project
{
    /// <summary>
    /// An extension method class
    /// </summary>
    public static class ExtensionMethodTest
    {
        /// <summary>
        /// A static extension method for <see cref="Foo"/>
        /// </summary>
        /// <param name="foo">
        /// the foo 
        /// </param>
        /// <param name="more">
        /// the more string 
        /// </param>
        /// <returns>
        /// an empty string 
        /// </returns>
        public static string FooExt(this Foo foo, string more)
        {
            return string.Empty;
        }


        /// <summary>
        /// A static extension method for <see cref="Foo"/>
        /// </summary>
        /// <param name="foo">
        /// the foo 
        /// </param>
        /// <param name="more">
        /// the more string 
        /// </param>
        /// <returns>
        /// an empty string 
        /// </returns>
        public static string RegularClassExt(this RegularClass foo, string more)
        {
            return string.Empty;
        }
    }
}