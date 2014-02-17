using System;
using System.Collections.Generic;
using System.Xml.XPath;
using System.Xml.Xsl;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{
    public class IndexProvider : IIndexProvider
    {
        private readonly IDependencyProvider _dependencyProvider;
        private readonly Dictionary<string, OrdinalResolver<XPathNavigatorIndex>> _indices;

        public IndexProvider(IDependencyProvider dependencyProvider)
        {
            this._dependencyProvider = dependencyProvider;
            this._indices = new Dictionary<string, OrdinalResolver<XPathNavigatorIndex>>(StringComparer.Ordinal);
        }

        public void Add(string name,
                        int ordinal,
                        Uri inputUri,
                        string matchExpression,
                        string keyExpression,
                        XsltContext xsltContext = null)
        {
            throw new NotImplementedException();
        }

        public XPathNodeIterator Get(string name, int ordinal, object value)
        {
            throw new System.NotImplementedException();
        }
    }
}