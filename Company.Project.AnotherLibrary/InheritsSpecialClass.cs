/*
 * Copyright 2012 LBi Netherlands B.V.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */

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
        public override void AnotherGeneric<M>(IEnumerable<M> inputOne,
                                               IEnumerable<RegularClass> inputTwo,
                                               IEnumerable<int> inputThree)
        {
            base.AnotherGeneric<M>(inputOne, inputTwo, inputThree);
        }
    }
}
