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
    /// Access modifier tests
    /// </summary>
    public class AccessModifierTests
    {
        public string AStringField;

        public string AStringProp { get; private set; }

        protected internal string ProtIntStringProp { get; internal set; }

        protected internal void ProtOrInt()
        {
        }

        protected internal virtual void ProtOrIntVirt()
        {
        }

        internal void Int()
        {
        }

        protected void Prot()
        {
        }

        public void Publ()
        {
        }

        private void Priv()
        {
        }

        public static void PublStatic()
        {
        }

        public virtual void PublVirt()
        {
        }
    }
}
