using System.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace LBi.LostDoc.Core.Templating.XPath
{
    public class XsltContextJoinFunction : IXsltContextFunction
    {
        #region IXsltContextFunction Members

        /// <summary>
        /// Provides the method to invoke the function with the given arguments in the given context.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Object"/> representing the return value of the function. 
        /// </returns>
        /// <param name="xsltContext">
        /// The XSLT context for the function call. 
        /// </param>
        /// <param name="args">
        /// The arguments of the function call. Each argument is an element in the array. 
        /// </param>
        /// <param name="docContext">
        /// The context node for the function call. 
        /// </param>
        public object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            string sep = (string)args[1];
            XPathNodeIterator nodeIter = (XPathNodeIterator)args[0];
            string ret = string.Join(sep, nodeIter.Cast<XPathNavigator>().Select(n => n.ToString()));
            return ret;
        }

        /// <summary>
        ///   Gets the minimum number of arguments for the function. This enables the user to differentiate between overloaded functions.
        /// </summary>
        /// <returns> The minimum number of arguments for the function. </returns>
        public int Minargs
        {
            get { return 2; }
        }

        /// <summary>
        ///   Gets the maximum number of arguments for the function. This enables the user to differentiate between overloaded functions.
        /// </summary>
        /// <returns> The maximum number of arguments for the function. </returns>
        public int Maxargs
        {
            get { return 2; }
        }

        /// <summary>
        ///   Gets the <see cref="T:System.Xml.XPath.XPathResultType" /> representing the XPath type returned by the function.
        /// </summary>
        /// <returns> An <see cref="T:System.Xml.XPath.XPathResultType" /> representing the XPath type returned by the function </returns>
        public XPathResultType ReturnType
        {
            get { return XPathResultType.String; }
        }

        /// <summary>
        ///   Gets the supplied XML Path Language (XPath) types for the function's argument list. This information can be used to discover the signature of the function which allows you to differentiate between overloaded functions.
        /// </summary>
        /// <returns> An array of <see cref="T:System.Xml.XPath.XPathResultType" /> representing the types for the function's argument list. </returns>
        public XPathResultType[] ArgTypes
        {
            get { return new[] {XPathResultType.NodeSet, XPathResultType.String}; }
        }

        #endregion
    }
}