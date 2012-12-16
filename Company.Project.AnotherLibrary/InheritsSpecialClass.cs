using System.Collections.Generic;
using Company.Project.Library;

namespace Company.Project.AnotherLibrary
{
    /// <summary>
    /// This class inherits the special class.
    /// </summary>
    public class InheritsSpecialClass : SpecialClass
    {
        /// <summary>
        /// This inherits the <see cref="GenericClass{T}.AnotherGeneric{M}"/> method
        /// </summary>
        /// <typeparam name="M">
        /// This is the M param 
        /// </typeparam>
        /// <param name="inputOne">
        /// First 
        /// </param>
        /// <param name="inputTwo">
        /// Second 
        /// </param>
        /// <param name="inputThree">
        /// Third 
        /// </param>
        public override void AnotherGeneric<M>(IEnumerable<M> inputOne, IEnumerable<RegularClass> inputTwo,
                                               IEnumerable<int> inputThree)
        {
            base.AnotherGeneric<M>(inputOne, inputTwo, inputThree);
        }
    }
}