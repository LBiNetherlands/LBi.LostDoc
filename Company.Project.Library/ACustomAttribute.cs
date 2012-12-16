using System;

namespace Company.Project.Library
{
    /// <summary>
    /// A custom attribute class
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class ACustomAttribute : Attribute
    {
        /// <summary>
        ///   An integer field.
        /// </summary>
        public int AField;

        /// <summary>
        ///   Initializes a new instance of the <see cref="ACustomAttribute" /> class. The a custom attribute.
        /// </summary>
        public ACustomAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ACustomAttribute"/> class. The a custom attribute.
        /// </summary>
        /// <param name="aFieldValue">
        /// </param>
        public ACustomAttribute(int aFieldValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ACustomAttribute"/> class. The a custom attribute.
        /// </summary>
        /// <param name="aFieldValue">
        /// </param>
        /// <param name="aPropValue">
        /// </param>
        public ACustomAttribute(int aFieldValue, object aPropValue)
        {
        }

        /// <summary>
        ///   An object property
        /// </summary>
        public object AProp { get; set; }


        /// <summary>
        ///   An array property
        /// </summary>
        public object[] ArrayProp { get; set; }


        /// <summary>
        /// Finalizes an instance of the <see cref="ACustomAttribute"/> class. A Destructor
        /// </summary>
        ~ACustomAttribute()
        {
// booya!
        }
    }
}