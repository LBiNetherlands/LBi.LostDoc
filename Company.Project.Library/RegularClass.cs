using System;
using System.Collections.Generic;

namespace Company.Project.Library
{
    /// <summary>
    /// A regular class.
    /// </summary>
    public class RegularClass : IDisposable
    {
        #region NestedEnum enum

        /// <summary>
        /// I'm a nested enum
        /// </summary>
        public enum NestedEnum
        {
            Aone,
            Atwo
        }

        #endregion

        /// <summary>
        ///   Initializes a new instance of the <see cref="RegularClass" /> class.
        /// </summary>
        public RegularClass()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegularClass"/> class.
        /// </summary>
        /// <param name="aString">
        /// A string. 
        /// </param>
        /// <param name="readOnlyInt">
        /// The read only int. 
        /// </param>
        public RegularClass(string aString, int readOnlyInt)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegularClass"/> class.
        /// </summary>
        /// <param name="aString">
        /// A string. 
        /// </param>
        /// <param name="readOnlyInt">
        /// The read only (optional) int. 
        /// </param>
        [ACustom(42, 5, ArrayProp = new object[] {typeof(RegularClass), 5, "test"})]
        public RegularClass(string aString, int? readOnlyInt)
        {
        }

        /// <summary>
        ///   Gets or sets the string property.
        /// </summary>
        /// <returns>The string</returns>
        public string StringProperty { get; set; }

        /// <summary>
        ///   Gets the readonly int property.
        /// </summary>
        /// <returns>The readonly int</returns>
        public int ReadonlyIntProperty { get; private set; }

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. (I'm a virtual!)
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>
        /// A Virtual FOo Method
        /// </summary>
        [ACustom(ArrayProp = new object[] {typeof(List<>), typeof(List<string>)})]
        [ACustom(ArrayProp = new object[] {typeof(RegularClass), 5, "test"})]
        public virtual void Foo()
        {
        }

        /// <summary>
        /// This foos the int.
        /// </summary>
        /// <param name="foo">
        /// </param>
        public virtual void Foo(int foo)
        {
        }

        /// <summary>
        /// The foo.
        /// </summary>
        /// <param name="foo">
        /// foo param 
        /// </param>
        public virtual void Foo(string foo)
        {
        }

        /// <summary>
        /// The foo.
        /// </summary>
        /// <param name="foo">
        /// float foo param 
        /// </param>
        public virtual void Foo(float foo)
        {
        }

        /// <summary>
        /// The foo.
        /// </summary>
        /// <param name="listOfFoo">
        /// a list of foo <see cref="string"/> 
        /// </param>
        public virtual void Foo(List<string> listOfFoo)
        {
        }

        /// <summary>
        ///   Implicit to String
        /// </summary>
        /// <param name="rc"> </param>
        /// <returns> </returns>
        public static implicit operator String(RegularClass rc)
        {
            return null;
        }

        /// <summary>
        ///   Implicit to int
        /// </summary>
        /// <param name="rc"> </param>
        /// <returns> </returns>
        public static implicit operator int(RegularClass rc)
        {
            return 0;
        }

        /// <summary>
        ///   Explicit to long
        /// </summary>
        /// <param name="rc"> </param>
        /// <returns> </returns>
        public static explicit operator long(RegularClass rc)
        {
            return 0;
        }

        /// <summary>
        ///   Explicit from int
        /// </summary>
        /// <param name="rc"> </param>
        /// <returns> </returns>
        public static explicit operator RegularClass(int rc)
        {
            return null;
        }

        /// <summary>
        ///   Equals
        /// </summary>
        /// <param name="rc"> </param>
        /// <param name="rc2"> </param>
        /// <returns> </returns>
        public static bool operator ==(RegularClass rc, RegularClass rc2)
        {
            return false;
        }

        /// <summary>
        ///   Not Equals
        /// </summary>
        /// <param name="rc"> </param>
        /// <param name="rc2"> </param>
        /// <returns> </returns>
        public static bool operator !=(RegularClass rc, RegularClass rc2)
        {
            return true;
        }

        /// <summary>
        ///   Add
        /// </summary>
        /// <param name="rc"> </param>
        /// <param name="rc2"> </param>
        /// <returns> </returns>
        public static RegularClass operator +(RegularClass rc, RegularClass rc2)
        {
            return null;
        }

        /// <summary>
        ///   Subtract
        /// </summary>
        /// <param name="rc"> </param>
        /// <param name="rc2"> </param>
        /// <returns> </returns>
        public static RegularClass operator -(RegularClass rc, RegularClass rc2)
        {
            return null;
        }

        /// <summary>
        ///   Add int
        /// </summary>
        /// <param name="rc"> </param>
        /// <param name="bla"> </param>
        /// <returns> </returns>
        public static RegularClass operator /(RegularClass rc, int bla)
        {
            return null;
        }


        /// <summary>
        /// With out-param
        /// </summary>
        /// <param name="test">the out parma</param>
        public void WithOut(out int test)
        {
            test = 0;
        }

        /// <summary>
        /// With ref-param
        /// </summary>
        /// <param name="test">The ref param</param>
        public void WithRefObj(ref object test)
        {
            test = null;
        }

        /// <summary>
        /// With out-param
        /// </summary>
        /// <param name="test">the out parma</param>
        public void WithOutObj(out object test)
        {
            test = 0;
        }


        /// <summary>
        /// With ref-param
        /// </summary>
        /// <param name="test">The ref param</param>
        public void WithRef(ref int test)
        {
            test = 0;
        }

        /// <summary>
        /// With intptr-param
        /// </summary>
        /// <param name="ptr">the IntPtr param</param>
        public void WithIntPtr(IntPtr ptr)
        {
            
        }


        #region Nested type: RegularNesteedClass

        /// <summary>
        /// I'm a nested class!
        /// </summary>
        public class RegularNesteedClass
        {
        }

        #endregion
    }
}