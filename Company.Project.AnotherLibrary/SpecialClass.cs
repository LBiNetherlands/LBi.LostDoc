﻿/*
 * Copyright 2012 DigitasLBi Netherlands B.V.
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
using Company.Project.Library;

namespace Company.Project.AnotherLibrary
{
    /// <summary>
    /// I'm an enum
    /// </summary>
    [Flags]
    public enum ThisIsAnEnum
    {
        /// <summary>
        ///   First field
        /// </summary>
        FirstField,

        /// <summary>
        ///   second field
        /// </summary>
        SecondField,

        /// <summary>
        ///   third field
        /// </summary>
        ThirdField
    }

    /// <summary>
    /// The special class in a dependant assembly
    /// </summary>
    public class SpecialClass : GenericClass<RegularClass>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpecialClass"/> class. Wraps <see cref="RegularClass"/> with a <see cref="SpecialClass"/> .
        /// </summary>
        /// <param name="input">
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if input is not a valid
        ///   <see cref="RegularClass"/>
        ///   .
        /// </exception>
        public SpecialClass(RegularClass input) : base(input)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SpecialClass" /> class. Default constructor that create a new instance of <see
        ///    cref="RegularClass" /> .
        /// </summary>
        public SpecialClass() : this(new RegularClass())
        {
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/> .
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/> ; otherwise, false. 
        /// </returns>
        /// <param name="obj">
        /// The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/> . 
        /// </param>
        /// <filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            return false;
        }

        #region Nested type: NestedClass

        /// <summary>
        /// A Nested class inside <see cref=" SpecialClass"/>
        /// </summary>
        /// <typeparam name="T">
        /// </typeparam>
        public class NestedClass<T>
        {
            /// <summary>
            /// This is a method with it's own generic parameter.
            /// </summary>
            /// <param name="t">
            /// </param>
            /// <param name="tkey">
            /// </param>
            /// <typeparam name="TKey">
            /// </typeparam>
            /// <returns>
            /// </returns>
            public static T MethodWithGeneric<TKey>(T t, TKey tkey)
            {
                return default(T);
            }
        }

        #endregion
    }
}
