using System.Collections;
using System.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace LBi.LostDoc.Core.Templating.XPath
{
    public class XsltContextAssetVersionGetter : IXsltContextFunction
    {
        private VersionComponent? _ignoredVersionComponent;

        public XsltContextAssetVersionGetter(VersionComponent? ignoredVersionComponent = null)
        {
            this._ignoredVersionComponent = ignoredVersionComponent;
        }

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

            if (this._ignoredVersionComponent.HasValue)
                return aid.Version.ToString((int)this._ignoredVersionComponent.Value);

            return aid.Version.ToString();
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