/* BSD License

Copyright (c) 2005, XMLMVP Project
All rights reserved.

Redistribution and use in source and binary forms, with or without 
modification, are permitted provided that the following conditions 
are met:

* Redistributions of source code must retain the above copyright 
notice, this list of conditions and the following disclaimer. 
* Redistributions in binary form must reproduce the above copyright 
notice, this list of conditions and the following disclaimer in 
the documentation and/or other materials provided with the 
distribution. 
* Neither the name of the XMLMVP Project nor the names of its 
contributors may be used to endorse or promote products derived
from this software without specific prior written permission. 

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS 
FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE 
COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, 
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, 
BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; 
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER 
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN 
ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
POSSIBILITY OF SUCH DAMAGE.
*/

#region using

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

#endregion using

namespace LBi.LostDoc.Core.Templating.XPath
{
    /// <summary>	
    /// <see cref="XPath.XPathNavigatorIndex"/> enables lazy or eager indexing of any XML store
    /// (<see cref="XmlDocument"/>, <see cref="XPathDocument"/> or any other <see cref="IXPathNavigable"/> XML store) thus
    /// providing an alternative way to select nodes using XSLT key() function directly from an index table 
    /// instead of searhing the XML tree. This allows drastically decrease selection time
    /// on preindexed selections.
    /// </summary>
    /// <remarks>
    /// <para>Author: Oleg Tkachenko, <a href="http://www.xmllab.net">http://www.xmllab.net</a>.</para>
    /// <para>Contributors: Daniel Cazzulino, <a href="http://clariusconsulting.net/kzu">blog</a></para>
    /// <para>See <a href="http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnxmlnet/html/XMLindexing.asp">"XML Indexing Part 1: XML IDs, XSLT Keys and Index"</a> article for more info.</para>
    /// </remarks>    
    public class XPathNavigatorIndex
    {
        #region Fields & Ctor

        private XPathNavigator nav;
        private IndexManager manager;

        /// <summary>
        /// Creates Index over specified XPathNavigator.
        /// </summary>
        /// <param name="navigator">Core XPathNavigator</param>
        public XPathNavigatorIndex(XPathNavigator navigator)
        {
            this.nav = navigator;
            manager = new IndexManager();
        }

        #endregion Fields & Ctor


        /// <summary>
        /// Builds indexes according to defined keys.
        /// </summary>
        public void BuildIndexes()
        {
            manager.BuildIndexes();
        }

        /// <summary>
        /// Adds named key for use with key() function.
        /// </summary>
        /// <param name="keyName">The name of the key</param>
        /// <param name="match">XPath pattern, defining the nodes to which 
        /// this key is applicable</param>
        /// <param name="use">XPath expression used to determine 
        /// the value of the key for each matching node</param>
        public virtual void AddKey(string keyName, string match, string use)
        {
            KeyDef key = new KeyDef(nav, match, use);
            manager.AddKey(nav, keyName, key);
        }

        public virtual void AddKey(string keyName, string match, string use, XsltContext customXsltContext)
        {
            KeyDef key = new KeyDef(nav, match, use, customXsltContext);
            manager.AddKey(nav, keyName, key);
        }

        public XPathNodeIterator Get(string keyName, object value)
        {
            return this.manager.GetNodes(keyName, value);
        }


        #region KeyDef

        /// <summary>
        /// Compilable key definition.
        /// </summary>
        private class KeyDef
        {
            private readonly string match;
            private string use;
            private XPathExpression matchExpr, useExpr;
            private XPathNavigator nav;
            private XsltContext context;

            /// <summary>
            /// Creates a key definition with specified 'match' and 'use' expressions.
            /// </summary>
            /// <param name="nav">XPathNavigator to compile XPath expressions</param>
            /// <param name="match">XPath pattern, defining the nodes to 
            /// which this key is applicable</param>
            /// <param name="use">XPath expression expression used to 
            /// determine the value of the key for each matching node.</param>
            /// <param name="customContext">Optional <see cref="XsltContext"/> implemenation.</param>
            public KeyDef(XPathNavigator nav, string match, string use, XsltContext customContext = null)
            {
                this.nav = nav;
                this.match = match;
                this.use = use;
                this.context = customContext;
            }

            /// <summary>
            /// XPath pattern, defining the nodes to 
            /// which this key is applicable.
            /// </summary>
            public string Match
            {
                get { return match; }
            }

            /// <summary>
            /// XPath expression expression used to 
            /// determine the value of the key for each matching node.
            /// </summary>
            public string Use
            {
                get { return use; }
            }

            /// <summary>
            /// Compiled XPath pattern, defining the nodes to 
            /// which this key is applicable.
            /// </summary>
            public XPathExpression MatchExpr
            {
                get
                {
                    if (matchExpr == null)
                    {
                        matchExpr = nav.Compile(match);
                        if (this.context != null)
                            matchExpr.SetContext(this.context);
                    }
                    return matchExpr;
                }
            }

            /// <summary>
            /// Compiled XPath expression expression used to 
            /// determine the value of the key for each matching node.
            /// </summary>
            public XPathExpression UseExpr
            {
                get
                {
                    if (useExpr == null)
                    {
                        useExpr = nav.Compile(use);
                        if (this.context != null)
                            useExpr.SetContext(this.context);
                    }
                    return useExpr;
                }
            }
        }

        #endregion KeyDef

        #region Index

        /// <summary>
        /// Index table for XPathNavigator.
        /// </summary>
        private class Index
        {
            private List<KeyDef> keys;
            private IDictionary<string, List<XPathNavigator>> index;

            /// <summary>
            /// Creates index over specified XPathNavigator.
            /// </summary>
            public Index()
            {
                keys = new List<KeyDef>();
                index = new Dictionary<string, List<XPathNavigator>>();
            }

            /// <summary>
            /// Adds a key.
            /// </summary>
            /// <param name="key">Key definition</param>
            public void AddKey(KeyDef key)
            {
                keys.Add(key);
            }

            /// <summary>
            /// Returns indexed nodes by a key value.
            /// </summary>
            /// <param name="keyValue">Key value</param>    
            public XPathNodeIterator GetNodes(object keyValue)
            {
                //As per XSLT spec:
                //When the second argument to the key function is of type node-set, 
                //then the result is the union of the result of applying the key function 
                //to the string value of each of the nodes in the argument node-set. 
                //When the second argument to key is of any other type, the argument is 
                //converted to a string as if by a call to the string function; it 
                //returns a node-set containing the nodes in the same document as 
                //the context node that have a value for the named key equal to this string.      
                List<XPathNavigator> indexedNodes = null, tmpIndexedNodes;
                if (keyValue is XPathNodeIterator)
                {
                    XPathNodeIterator nodes = keyValue as XPathNodeIterator;
                    while (nodes.MoveNext())
                    {

                        if (index.TryGetValue(nodes.Current.Value, out tmpIndexedNodes))
                        {
                            if (indexedNodes == null)
                                indexedNodes = new List<XPathNavigator>();
                            indexedNodes.AddRange(tmpIndexedNodes);
                        }
                    }
                }
                else
                {
                    index.TryGetValue(keyValue.ToString(), out indexedNodes);
                }
                if (indexedNodes == null)
                    indexedNodes = new List<XPathNavigator>(0);

                return new XPathNavigatorIterator(indexedNodes);
            }

            /// <summary>
            /// Matches given node against "match" pattern and adds it to 
            /// the index table if the matching succeeded.
            /// </summary>
            /// <param name="node">Node to match</param>
            public void MatchNode(XPathNavigator node)
            {
                foreach (KeyDef keyDef in keys)
                {
                    if (node.Matches(keyDef.MatchExpr))
                    {
                        //Ok, let's calculate key value(s). As per XSLT spec:
                        //If the result is a node-set, then for each node in the node-set, 
                        //the node that matches the pattern has a key of the specified name whose 
                        //value is the string-value of the node in the node-set; otherwise, the result 
                        //is converted to a string, and the node that matches the pattern has a 
                        //key of the specified name with value equal to that string.        
                        object key = node.Evaluate(keyDef.UseExpr);
                        if (key is XPathNodeIterator)
                        {
                            XPathNodeIterator ni = (XPathNodeIterator)key;
                            while (ni.MoveNext())
                                AddNodeToIndex(node, ni.Current.Value);
                        }
                        else
                        {
                            AddNodeToIndex(node, key.ToString());
                        }
                    }
                }
            }

            /// <summary>
            /// Adds node to the index slot according to key value.
            /// </summary>
            /// <param name="node">Node to add to index</param>
            /// <param name="key">String key value</param>
            private void AddNodeToIndex(XPathNavigator node, string key)
            {
                //Get slot
                List<XPathNavigator> indexedNodes;
                if (!index.TryGetValue(key, out indexedNodes))
                {
                    indexedNodes = new List<XPathNavigator>();
                    index.Add(key, indexedNodes);
                }
                indexedNodes.Add(node.Clone());
            }
        }

        #endregion Index

        #region IndexManager

        /// <summary>
        /// Index manager. Manages collection of named indexes.
        /// </summary>
        private class IndexManager
        {
            private IDictionary<string, Index> indexes;
            private XPathNavigator nav;
            private bool indexed;

            /// <summary>
            /// Adds new key to the named index.
            /// </summary>
            /// <param name="nav">XPathNavigator over XML document to be indexed</param>
            /// <param name="indexName">Index name</param>
            /// <param name="key">Key definition</param>
            public void AddKey(XPathNavigator nav, string indexName, KeyDef key)
            {
                this.indexed = false;
                this.nav = nav;
                //Named indexes are stored in a hashtable.
                if (indexes == null)
                    indexes = new Dictionary<string, Index>();
                Index index;
                if (!indexes.TryGetValue(indexName, out index))
                {
                    index = new Index();
                    indexes.Add(indexName, index);
                }
                index.AddKey(key);
            }

            /// <summary>
            /// Builds indexes.
            /// </summary>
            public void BuildIndexes()
            {
                XPathNavigator doc = nav.Clone();
                //Walk through the all document nodes adding each one matching 
                //'match' expression to the index.
                doc.MoveToRoot();
                //Select all nodes but namespaces and attributes
                XPathNodeIterator ni = doc.SelectDescendants(XPathNodeType.All, true);
                while (ni.MoveNext())
                {
                    if (ni.Current.NodeType == XPathNodeType.Element)
                    {
                        XPathNavigator tempNav = ni.Current.Clone();
                        //Processs namespace nodes
                        for (bool go = tempNav.MoveToFirstNamespace(); go; go = tempNav.MoveToNextNamespace())
                        {
                            foreach (Index index in indexes.Values)
                                index.MatchNode(tempNav);
                        }
                        //ni.Current.MoveToParent();

                        tempNav = ni.Current.Clone();
                        //process attributes
                        for (bool go = tempNav.MoveToFirstAttribute(); go; go = tempNav.MoveToNextAttribute())
                        {
                            foreach (Index index in indexes.Values)
                                index.MatchNode(tempNav);
                        }
                        //ni.Current.MoveToParent();
                    }

                    foreach (Index index in indexes.Values)
                        index.MatchNode(ni.Current);
                }
                indexed = true;
            }

            /// <summary>
            /// Get indexed nodes by index name and key value.
            /// </summary>    
            /// <param name="indexName">Index name</param>
            /// <param name="value">Key value</param>
            /// <returns>Indexed nodes</returns>
            public XPathNodeIterator GetNodes(string indexName, object value)
            {
                if (!indexed)
                    BuildIndexes();
                Index index;
                indexes.TryGetValue(indexName, out index);
                return index == null ? null : index.GetNodes(value);
            }
        }

        #endregion IndexManager


    }
}