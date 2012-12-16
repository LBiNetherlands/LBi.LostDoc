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

namespace Company.Project.Library
{
    /// <summary>
    /// This is a simple generic class that inherits <see cref="SecondLevelSimpleGeneric{T4,T5}"/>
    /// </summary>
    /// <typeparam name="T6">
    /// </typeparam>
    public class ThirdLevelSimpleGeneric<T6> : SecondLevelSimpleGeneric<int, T6>
    {
        /// <summary>
        /// Override virtual test method
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
        public override void SimpleTest(string one, int two, T6 three)
        {
            base.SimpleTest(one, two, three);
        }
    }
}
