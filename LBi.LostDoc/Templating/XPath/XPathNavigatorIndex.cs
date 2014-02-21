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
using System.Xml.XPath;
using System.Xml.Xsl;

namespace LBi.LostDoc.Templating.XPath
{
    public enum MergeMode
    {
        Merge,
        Replace
    }

    public class XPathNavigatorIndex
    {
        private class ResultIterator : XPathNodeIterator
        {
            private readonly List<XPathNavigator> _results;
            private int _position;

            public ResultIterator(List<XPathNavigator> results)
            {
                this._position = -1;
                this._results = results;
            }

            private ResultIterator(List<XPathNavigator> results, int position)
            {
                this._results = results;
                this._position = position;
            }

            public override XPathNodeIterator Clone()
            {
                return new ResultIterator(this._results, this._position);
            }

            public override bool MoveNext()
            {
                return ++this._position < this._results.Count;
            }

            public override XPathNavigator Current
            {
                get
                {
                    if (this._position < 0 || this._position >= this._results.Count)
                        throw new InvalidOperationException();

                    return this._results[this._position].Clone();
                }
            }

            public override int CurrentPosition
            {
                get { return this._position + 1; }
            }
        }

        private readonly Dictionary<string, List<XPathNavigator>> _index;

        public XPathNavigatorIndex()
        {
            this._index = new Dictionary<string, List<XPathNavigator>>(StringComparer.Ordinal);
        }

        private XPathNavigatorIndex(Dictionary<string, List<XPathNavigator>> index)
        {
            this._index = new Dictionary<string, List<XPathNavigator>>(index);
        }

        public XPathNavigatorIndex Merge(XPathNavigatorIndex index, MergeMode mode)
        {
            XPathNavigatorIndex ret = new XPathNavigatorIndex(this._index);
            foreach (var pair in index._index)
            {
                List<XPathNavigator> values;
                if (ret._index.TryGetValue(pair.Key, out values))
                {
                    if (mode == MergeMode.Merge)
                        values.AddRange(pair.Value);
                    else if (mode == MergeMode.Replace)
                        ret._index[pair.Key] = pair.Value;
                    else
                        throw new ArgumentOutOfRangeException("mode");
                }
                else
                    ret._index.Add(pair.Key, pair.Value);
            }
            return ret;
        }

        public static XPathNavigatorIndex Create(XPathNavigator navigator, string matchExpression, string keyExpression, string selectExpression, XsltContext context)
        {
            XPathNavigatorIndex ret = new XPathNavigatorIndex();

            XPathExpression matchExpr = navigator.Compile(matchExpression);
            XPathExpression keyExpr = navigator.Compile(keyExpression);
            XPathExpression selectExpr = navigator.Compile(selectExpression);
            if (context != null)
            {
                matchExpr.SetContext(context);
                keyExpr.SetContext(context);
                selectExpr.SetContext(context);
            }

            XPathNodeIterator nodeIterator = navigator.SelectDescendants(XPathNodeType.All, true);
            while (nodeIterator.MoveNext())
            {
                XPathNavigator currentNode = nodeIterator.Current;

                if (currentNode.NodeType == XPathNodeType.Element)
                {
                    XPathNavigator temp = currentNode.Clone();
                    for (bool hasMore = temp.MoveToFirstNamespace(); hasMore; hasMore = temp.MoveToNextNamespace())
                        IndexNode(ret, temp, matchExpr, keyExpr, selectExpr);

                    temp = currentNode.Clone();
                    for (bool hasMore = temp.MoveToFirstAttribute(); hasMore; hasMore = temp.MoveToNextAttribute())
                        IndexNode(ret, temp, matchExpr, keyExpr, selectExpr);
                }

                IndexNode(ret, currentNode, matchExpr, keyExpr, selectExpr);
            }

            return ret;
        }

        private static void IndexNode(XPathNavigatorIndex ret, XPathNavigator currentNode, XPathExpression matchExpr, XPathExpression keyExpr, XPathExpression selectExpr)
        {
            if (currentNode.Matches(matchExpr))
            {
                object key = currentNode.Evaluate(keyExpr);
                XPathNodeIterator keyIterator = key as XPathNodeIterator;
                if (keyIterator != null)
                {
                    while (keyIterator.MoveNext())
                    {
                        XPathNodeIterator nodeIterator = currentNode.Select(selectExpr);
                        while (nodeIterator.MoveNext())
                            ret.RegisterNode(keyIterator.Current.Value, nodeIterator.Current.Clone());
                    }
                }
                else if (key != null)
                {
                    XPathNodeIterator nodeIterator = currentNode.Select(selectExpr);
                    while (nodeIterator.MoveNext())
                        ret.RegisterNode(key.ToString(), nodeIterator.Current.Clone());
                }
            }
        }

        private void RegisterNode(string keyValue, XPathNavigator node)
        {
            List<XPathNavigator> values;
            if (!this._index.TryGetValue(keyValue, out values))
                this._index.Add(keyValue, values = new List<XPathNavigator>());

            values.Add(node);
        }

        public XPathNodeIterator Get(object key)
        {
            // TODO this could prob be optmized slightly by shifting this logic to the ResultIterator
            List<XPathNavigator> result = new List<XPathNavigator>();

            List<XPathNavigator> values;
            XPathNodeIterator keyIterator = key as XPathNodeIterator;
            if (keyIterator != null)
            {
                while (keyIterator.MoveNext())
                {
                    if (this._index.TryGetValue(keyIterator.Current.Value, out values))
                        result.AddRange(values);
                }
            }
            else if (key != null)
            {
                if (this._index.TryGetValue(key.ToString(), out values))
                    result.AddRange(values);
            }

            return new ResultIterator(result);
        }
    }
}
