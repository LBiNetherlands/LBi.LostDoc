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
    /// Class that inherits from RegularClass
    /// </summary>
    public class InheritedRegularClass : RegularClass
    {
        /// <summary>
        /// Inherits parent class.
        /// </summary>
        public class NestedClassThatInheritsParent : InheritedRegularClass 
        {
            public new virtual void Without(out int test)
            {
                test = 5;
            }
        }
        /// <summary>
        /// Overridden foo
        /// </summary>
        public override void Foo()
        {
            base.Foo();
        }

        /// <summary>
        /// I'm a sealed override
        /// </summary>
        public override sealed void Dispose()
        {
            base.Dispose();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        /// <summary>
        /// I'm a "new"
        /// </summary>
        /// <param name="test"></param>
        public new void WithOut(out int test)
        {
            test = 4;
        }
    }
}
