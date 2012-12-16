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

using System.Collections;
using System.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace LBi.LostDoc.Core.Templating.XPath
{
    public class XsltContextAssetIdGetter : IXsltContextFunction
    {
        #region IXsltContextFunction Members

        public object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            string rawAid = string.Empty;

            if (args[0] is string)
                rawAid = (string)args[0];
            else if (args[0] is IEnumerable)
            {
                XPathNavigator nav = (XPathNavigator)((IEnumerable)args[0]).Cast<object>().First();
                rawAid = nav.Value;
            }

            AssetIdentifier aid = AssetIdentifier.Parse(rawAid);
            return aid.AssetId;
        }

        public int Minargs
        {
            get { return 1; }
        }

        public int Maxargs
        {
            get { return 1; }
        }

        public XPathResultType ReturnType
        {
            get { return XPathResultType.String; }
        }

        public XPathResultType[] ArgTypes
        {
            get { return new[] {XPathResultType.String}; }
        }

        #endregion
    }
}
