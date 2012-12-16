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

using System;
using System.Collections;
using System.Collections.Generic;

namespace Company.Project.Library
{
    /// <summary>
    /// A class with one generic parameter
    /// </summary>
    /// <typeparam name="T">
    /// The generic parameter 
    /// </typeparam>
    public class GenericClass<T> : IEnumerable<T> where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericClass{T}"/> class. Initializes a new instance of the <see cref="GenericClass&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="input">
        /// The input. 
        /// </param>
        public GenericClass(T input)
        {
        }

        #region IEnumerable<T> Members

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>
        /// The enumerator! 
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            yield break;
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection. 
        /// </returns>
        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>
        /// Returns the generic.
        /// </summary>
        /// <returns>
        /// The generic value 
        /// </returns>
        public T ReturnsGeneric()
        {
            return default(T);
        }


        /// <summary>
        /// Anothers the generic.
        /// </summary>
        /// <typeparam name="M">
        /// The M TypeParam 
        /// </typeparam>
        /// <param name="inputOne">
        /// The input one. 
        /// </param>
        /// <param name="inputTwo">
        /// The input two. 
        /// </param>
        /// <param name="inputThree">
        /// The input three. 
        /// </param>
        public virtual void AnotherGeneric<M>(IEnumerable<M> inputOne, IEnumerable<T> inputTwo,
                                              IEnumerable<int> inputThree) where M : T
        {
        }

        /// <summary>
        /// Consumes the generic (XPA).
        /// </summary>
        /// <param name="input">
        /// The input. 
        /// </param>
        public void ConsumesGeneric(T input)
        {
        }

        /// <summary>
        /// Consumeses the generic.
        /// </summary>
        /// <typeparam name="M">
        /// </typeparam>
        /// <param name="input">
        /// The input. 
        /// </param>
        /// <param name="input2">
        /// The input2. 
        /// </param>
        public void ConsumesGeneric<M>(T input, M input2)
        {
        }

        #region Nested type: NestedGeneric

        public class NestedGeneric<P>
        {
            public void ConsumeP(P input)
            {
            }

            /// <summary>
            /// I'm special
            /// </summary>
            /// <typeparam name="U">
            /// </typeparam>
            /// <param name="input">
            /// </param>
            public void ConsumeU<U>(U input)
            {
            }
        }

        #endregion

        #region Nested type: NestedGeneric2

        public class NestedGeneric2<T> where T : struct
        {
            public void Consume(T input)
            {
            }
        }

        #endregion
    }
}
