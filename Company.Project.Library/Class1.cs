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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Company.Project.Library
{
    /// <summary>
    /// Class that inherits from RegularClass
    /// </summary>
    public class InheritedRegularClass : RegularClass
    {

    }


    /// <summary>
    /// Class that inherits GenericClass[string]
    /// </summary>
    public class InheritedGenericClass : GenericClass<string>
    {
        public InheritedGenericClass(string str)
            : base(str)
        {
        }
    }

    /// <summary>
    /// A regular class.
    /// </summary>
    public class RegularClass
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="RegularClass"/> class.
        /// </summary>
        public RegularClass()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegularClass"/> class.
        /// </summary>
        /// <param name="aString">A string.</param>
        /// <param name="readOnlyInt">The read only int.</param>
        public RegularClass(string aString, int readOnlyInt)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegularClass"/> class.
        /// </summary>
        /// <param name="aString">A string.</param>
        /// <param name="readOnlyInt">The read only (optional) int.</param>
        public RegularClass(string aString, int? readOnlyInt)
        {
        }


        /// <summary>
        /// Gets or sets the string property.
        /// </summary>
        /// <value>
        /// The string property.
        /// </value>
        public string StringProperty { get; set; }

        /// <summary>
        /// Gets the readonly int property.
        /// </summary>
        public int ReadonlyIntProperty { get; private set; }
    }

    /// <summary>
    /// A class with one generic parameter
    /// </summary>
    /// <typeparam name="T">The generic parameter</typeparam>
    public class GenericClass<T> : IEnumerable<T>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericClass&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="input">The input.</param>
        public GenericClass(T input) {
        }

        /// <summary>
        /// Returns the generic.
        /// </summary>
        /// <returns>The generic value</returns>
        public T ReturnsGeneric()
        {
            return default(T);
        }


        /// <summary>
        /// Anothers the generic.
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="inputOne">The input one.</param>
        /// <param name="inputTwo">The input two.</param>
        /// <param name="inputThree">The input three.</param>
        public void AnotherGeneric<M>(IEnumerable<M> inputOne, IEnumerable<T> inputTwo, IEnumerable<int> inputThree) {
        }

        /// <summary>
        /// Consumes the generic.
        /// </summary>
        /// <param name="input">The input.</param>
        public void ConsumesGeneric(T input)
        {
        }

        /// <summary>
        /// Consumeses the generic.
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="input">The input.</param>
        /// <param name="input2">The input2.</param>
        public void ConsumesGeneric<M>(T input, M input2)
        {
        }



        #region IEnumerable<T> Members

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>The enumerator!</returns>
        public IEnumerator<T> GetEnumerator()
        {
            yield break;
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

}
