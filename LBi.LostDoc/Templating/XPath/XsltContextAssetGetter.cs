/*
 * Copyright 2014 DigitasLBi Netherlands B.V.
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

using System.Xml.XPath;
using System.Xml.Xsl;

namespace LBi.LostDoc.Templating.XPath
{
    public class XsltContextAssetGetter : IXsltContextFunction
    {
        private static readonly XPathResultType[] _ArgTypes;

        static XsltContextAssetGetter()
        {
            _ArgTypes = new[] { XPathResultType.String, XPathResultType.String };
        }

        public object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            return new AssetIdentifier(XPathServices.ResultToString(args[0]), XPathServices.ResultToString(args[1])).ToString();
        }

        public int Minargs
        {
            get { return 2; }
        }

        public int Maxargs
        {
            get { return 2; }
        }

        public XPathResultType ReturnType
        {
            get { return XPathResultType.String; }
        }

        public XPathResultType[] ArgTypes
        {
            get { return _ArgTypes; }
        }
    }
}