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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.XPath;
using System.Xml.Xsl;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{
    public class IndexProvider : IIndexProvider
    {
        private class Definition
        {
            public Definition(string name, int ordinal, Uri inputUri, string matchExpression, string keyExpression, string selectExpression, XsltContext xsltContext)
            {
                this.Name = name;
                this.Ordinal = ordinal;
                this.InputUri = inputUri;
                this.MatchExpression = matchExpression;
                this.KeyExpression = keyExpression;
                this.SelectExpression = selectExpression;
                this.Context = xsltContext;
            }

            public string Name { get; set; }
            public int Ordinal { get; set; }
            public Uri InputUri { get; set; }
            public string MatchExpression { get; set; }
            public string KeyExpression { get; set; }
            public string SelectExpression { get; set; }
            public XsltContext Context { get; set; }

            public XPathNavigatorIndex Evaluate(IDependencyProvider dependencyProvider)
            {
                // TODO should we try to get a cached version of the navigator?
                using (Stream stream = dependencyProvider.GetDependency(this.InputUri, this.Ordinal))
                {
                    XPathDocument doc = new XPathDocument(stream);
                    XPathNavigator navigator = doc.CreateNavigator();
                    return XPathNavigatorIndex.Create(navigator, this.MatchExpression, this.KeyExpression, this.SelectExpression, this.Context);
                }
            }
        }

        private readonly IDependencyProvider _dependencyProvider;

        private readonly Dictionary<string, List<Definition>> _definitions;
        private readonly Dictionary<string, OrdinalResolver<XPathNavigatorIndex>> _indices;

        public IndexProvider(IDependencyProvider dependencyProvider)
        {
            this._dependencyProvider = dependencyProvider;
            this._indices = new Dictionary<string, OrdinalResolver<XPathNavigatorIndex>>(StringComparer.Ordinal);
            this._definitions = new Dictionary<string, List<Definition>>();
        }

        public void Add(string name,
                        int ordinal,
                        Uri inputUri,
                        string matchExpression,
                        string keyExpression,
                        string selectExpression,
                        XsltContext xsltContext = null)
        {
            List<Definition> definitions;
            OrdinalResolver<XPathNavigatorIndex> resolver;
            if (!this._definitions.TryGetValue(name, out definitions))
            {
                this._definitions.Add(name, definitions = new List<Definition>());
                this._indices.Add(name, resolver = new OrdinalResolver<XPathNavigatorIndex>(this.CreateFallbackEvaluator(name)));
            }
            else
                resolver = this._indices[name];

            definitions.Add(new Definition(name, ordinal, inputUri, matchExpression, keyExpression, selectExpression, xsltContext));

            resolver.Add(ordinal, this.CreateIndexEvaluator(definitions));

        }

        public XPathNodeIterator Get(string name, int ordinal, object value)
        {
            OrdinalResolver<XPathNavigatorIndex> resolver;
            if (this._indices.TryGetValue(name, out resolver))
            {
                var index = resolver.Resolve(ordinal).Value;
                return index.Get(value);
            }
            
            throw new KeyNotFoundException(string.Format("No index with name: '{0}'", name));
        }

        private Lazy<XPathNavigatorIndex> CreateFallbackEvaluator(string name)
        {
            return new Lazy<XPathNavigatorIndex>(
                () =>
                {
                    throw new KeyNotFoundException(string.Format("No index with name: '{0}'", name));
                },
                isThreadSafe: true);
        }

        private Lazy<XPathNavigatorIndex> CreateIndexEvaluator(IEnumerable<Definition> definitions)
        {
            Definition[] indexDefinitions = definitions.ToArray();
            return new Lazy<XPathNavigatorIndex>(
                () =>
                {
                    XPathNavigatorIndex index = new XPathNavigatorIndex();

                    for (int i = 0; i < indexDefinitions.Length; i++)
                    {
                        XPathNavigatorIndex tmp = indexDefinitions[i].Evaluate(this._dependencyProvider);
                        index = index.Merge(tmp, MergeMode.Replace);
                    }

                    return index;
                },
                LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }
}