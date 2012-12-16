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

using System.Runtime.Serialization;

namespace Company.Project.Service.DataContracts
{
// Use a data contract as illustrated in the sample below to add composite types to service operations
    /// <summary>
    /// The composite data contract
    /// </summary>
    [DataContract]
    public class CompositeType
    {
        private bool boolValue = true;
        private string stringValue = "Hello ";

        /// <summary>
        ///   Gets or sets a value indicating whether true.
        /// </summary>
        /// <value> <c>true</c> if true; otherwise, <c>false</c> . </value>
        [DataMember]
        public bool BoolValue
        {
            get { return this.boolValue; }
            set { this.boolValue = value; }
        }

        /// <summary>
        ///   Gets or sets the string value.
        /// </summary>
        /// <value> The string value. </value>
        [DataMember]
        public string StringValue
        {
            get { return this.stringValue; }
            set { this.stringValue = value; }
        }
    }
}
