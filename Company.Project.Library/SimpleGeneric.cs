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

namespace Company.Project.Library
{
    /// <summary>
    /// This is a simple generic class
    /// </summary>
    /// <typeparam name="T1">
    /// First param 
    /// </typeparam>
    /// <typeparam name="T2">
    /// Second param 
    /// </typeparam>
    /// <typeparam name="T3">
    /// Third param 
    /// </typeparam>
    public class SimpleGeneric<T1, T2, T3>
    {
        /// <summary>
        /// Virtual Test method
        /// </summary>
        /// <param name="one">
        /// Param one 
        /// </param>
        /// <param name="two">
        /// Param two 
        /// </param>
        /// <param name="three">
        /// Param three 
        /// </param>
        public virtual void SimpleTest(T1 one, T2 two, T3 three)
        {
        }
    }
}
