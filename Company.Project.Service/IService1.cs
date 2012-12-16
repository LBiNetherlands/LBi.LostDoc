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