using System.Collections.Generic;

namespace Company.Project
{
    /// <summary>
    /// Monkey summary
    /// </summary>
    /// <typeparam name="T">
    /// MYT 
    /// </typeparam>
    public class Bar<T>
    {
        public void AddRange(IEnumerable<T> moop)
        {
        }

        public void ConsumeOpen(IEnumerable<T> enu)
        {
        }
    }


    /// <summary>
    /// A specialized list of <see cref="string"/>
    /// </summary>
    public class Foo : Bar<string>
    {
        public void ConsumeClosed(IEnumerable<string> enu)
        {
        }

        /// <summary>
        ///   This operator checks equality
        /// </summary>
        /// <param name="one"> </param>
        /// <param name="two"> </param>
        /// <returns> </returns>
        public static bool operator ==(Foo one, Foo two)
        {
            return false;
        }

        /// <summary>
        ///   this operator checks for inequality
        /// </summary>
        /// <param name="one"> </param>
        /// <param name="two"> </param>
        /// <returns> </returns>
        public static bool operator !=(Foo one, Foo two)
        {
            return !(one == two);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}