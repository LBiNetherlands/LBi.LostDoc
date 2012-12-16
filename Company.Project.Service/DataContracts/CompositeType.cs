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