/*
 * Copyright 2013 DigitasLBi Netherlands B.V.
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

using System;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace LBi.LostDoc.Templating.XPath
{
    public class XsltContextSubstringAfterLastFunction : IXsltContextFunction
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
            //return args.FirstOrDefault(t => !string.IsNullOrEmpty(t as string));
            string s1 = Template.ResultToString(args[0]);
            string s2 = Template.ResultToString(args[1]);
            int ix = s1.LastIndexOf(s2, StringComparison.Ordinal);
            if (ix >= 0)
                return s1.Substring(ix + 1);

            return string.Empty;
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
            get { return new[] { XPathResultType.String, XPathResultType.String, }; }
        }

        #endregion
    }
}