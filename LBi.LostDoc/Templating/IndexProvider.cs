using System;
using System.Collections.Generic;
using System.Xml.XPath;
using System.Xml.Xsl;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{
    public class IndexProvider : IIndexProvider
    {
        private class Definition
        {
            public string Name { get; set; }
            public int Ordinal { get; set; }
            public Uri InputUri { get; set; }
            public string MatchExpression { get; set; }
            public string KeyExpression { get; set; }
            public XsltContext Context { get; set; }
        }


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
            this._indices.Add(name, new OrdinalResolver<XPathNavigatorIndex>(this.CreateFallbackEvaluator(name)));
        }

        public XPathNodeIterator Get(string name, int ordinal, object value)
        {
            OrdinalResolver<XPathNavigatorIndex> resolver;
            if (this._indices.TryGetValue(name, out resolver))
            {
                var index = resolver.Resolve(ordinal).Value;
                index.Get()

            }
            else
                throw new KeyNotFoundException(string.Format("No index with name: '{0}'", name));
        }

        private Lazy<XPathNavigatorIndex> CreateFallbackEvaluator(string name)
        {
            throw new KeyNotFoundException("No index added");
        }

        private Lazy<XPathNavigatorIndex> CreateIndexEvaluator(string name, Uri inputUri, string matchExpression, string keyExpression, XsltContext xsltContext)
        {
            return new Lazy<XPathNavigatorIndex>(
                () =>
                {
                    
                });
        }
    }
}