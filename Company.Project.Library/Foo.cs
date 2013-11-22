/*
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
