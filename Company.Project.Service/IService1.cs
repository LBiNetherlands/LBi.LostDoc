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

using System.ServiceModel;
using Company.Project.Service.DataContracts;

namespace Company.Project.Service
{
// NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    /// <summary>
    /// This is a test service
    /// </summary>
    [ServiceContract]
    public interface IService1
    {
        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="value">
        /// The value. 
        /// </param>
        /// <returns>
        /// The string value 
        /// </returns>
        [OperationContract]
        string GetData(int value);

        /// <summary>
        /// Gets the data using data contract.
        /// </summary>
        /// <param name="composite">
        /// The composite. 
        /// </param>
        /// <returns>
        /// Another Composite type 
        /// </returns>
        [OperationContract]
        CompositeType GetDataUsingDataContract(CompositeType composite);

        // TODO: Add your service operations here
    }
}
