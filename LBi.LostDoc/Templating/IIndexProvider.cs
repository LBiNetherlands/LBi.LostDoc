using System;
using System.Diagnostics.Contracts;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace LBi.LostDoc.Templating
{
    [ContractClass(typeof(IIndexProviderContract))]
    public interface IIndexProvider
    {
        void Add(string name,
                 int ordinal,
                 Uri inputUri,
                 string matchExpression,
                 string keyExpression,
                 XsltContext xsltContext = null);

        XPathNodeIterator Get(string name, int ordinal, object value);
    }

    [ContractClassFor(typeof(IIndexProvider))]
    // ReSharper disable InconsistentNaming
    internal class IIndexProviderContract : IIndexProvider
    // ReSharper restore InconsistentNaming
    {
        public void Add(string name, int ordinal, Uri inputUri, string matchExpression, string keyExpression, XsltContext xsltContext)
        {
            Contract.Requires<ArgumentNullException>(name != null, "name cannot be null.");
            Contract.Requires<ArgumentOutOfRangeException>(ordinal >= 0, "ordinal cannot be negative.");
            Contract.Requires<ArgumentNullException>(inputUri != null, "inputUri cannot be null.");
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(matchExpression), "matchExpression cannot be null or empty.");
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(keyExpression), "keyExpression cannot be null or empty.");
        }

        public XPathNodeIterator Get(string name, int ordinal, object value)
        {
            Contract.Requires<ArgumentNullException>(name != null, "name cannot be null.");
            Contract.Requires<ArgumentOutOfRangeException>(ordinal >= 0, "ordinal cannot be negative.");

            return default(XPathNodeIterator);
        }
    }
}