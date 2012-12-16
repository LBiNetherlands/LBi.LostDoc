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

using Company.Project.Library;

namespace Company.Project
{
    /// <summary>
    /// An extension method class
    /// </summary>
    public static class ExtensionMethodTest
    {
        /// <summary>
        /// A static extension method for <see cref="Foo"/>
        /// </summary>
        /// <param name="foo">
        /// the foo 
        /// </param>
        /// <param name="more">
        /// the more string 
        /// </param>
        /// <returns>
        /// an empty string 
        /// </returns>
        public static string FooExt(this Foo foo, string more)
        {
            return string.Empty;
        }


        /// <summary>
        /// A static extension method for <see cref="Foo"/>
        /// </summary>
        /// <param name="foo">
        /// the foo 
        /// </param>
        /// <param name="more">
        /// the more string 
        /// </param>
        /// <returns>
        /// an empty string 
        /// </returns>
        public static string RegularClassExt(this RegularClass foo, string more)
        {
            return string.Empty;
        }
    }
}
