/* BSD License

 * Copyright (c) 2005, XMLMVP Project
 * Copyright (c) 2014, DigitasLBi Netherlands B.V.
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

namespace LBi.LostDoc.Templating.XPath
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
    // TODO clean this up a bit, the IndexManager isn't very nice
    public class XPathNavigatorIndex
    {
        #region Fields & Ctor

        private readonly XPathNavigator _nav;
        private readonly IndexManager _manager;

        /// <summary>
        /// Creates Index over specified XPathNavigator.
        /// </summary>
        /// <param name="navigator">Core XPathNavigator</param>
        public XPathNavigatorIndex(XPathNavigator navigator)
        {
            this._nav = navigator;
            this._manager = new IndexManager();
        }

        #endregion Fields & Ctor


        /// <summary>
        /// Builds indexes according to defined keys.
        /// </summary>
        public void BuildIndexes()
        {
            this._manager.BuildIndexes();
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
            KeyDef key = new KeyDef(this._nav, match, use);
            this._manager.AddKey(this._nav, keyName, key);
        }

        public virtual void AddKey(string keyName, string match, string use, XsltContext customXsltContext)
        {
            KeyDef key = new KeyDef(this._nav, match, use, customXsltContext);
            this._manager.AddKey(this._nav, keyName, key);
        }

        public XPathNodeIterator Get(string keyName, object value)
        {
            return this._manager.GetNodes(keyName, value);
        }


        #region KeyDef
        /// <summary>
        /// Compilable key definition.
        /// </summary>
        private class KeyDef
        {
            private XPathExpression _matchExpr;
            private XPathExpression _useExpr;
            private readonly XPathNavigator _nav;
            private readonly XsltContext _context;

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
                this._nav = nav;
                this.Match = match;
                this.Use = use;
                this._context = customContext;
            }

            /// <summary>
            /// XPath pattern, defining the nodes to 
            /// which this key is applicable.
            /// </summary>
            public string Match { get; private set; }

            /// <summary>
            /// XPath expression expression used to 
            /// determine the value of the key for each matching node.
            /// </summary>
            public string Use { get; private set; }

            /// <summary>
            /// Compiled XPath pattern, defining the nodes to 
            /// which this key is applicable.
            /// </summary>
            public XPathExpression MatchExpr
            {
                get
                {
                    if (this._matchExpr == null)
                    {
                        this._matchExpr = this._nav.Compile(this.Match);
                        if (this._context != null)
                            this._matchExpr.SetContext(this._context);
                    }
                    return this._matchExpr;
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
                    if (this._useExpr == null)
                    {
                        this._useExpr = this._nav.Compile(this.Use);
                        if (this._context != null)
                            this._useExpr.SetContext(this._context);
                    }
                    return this._useExpr;
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
            private readonly List<KeyDef> _keys;
            private readonly IDictionary<string, List<XPathNavigator>> _index;

            /// <summary>
            /// Creates index over specified XPathNavigator.
            /// </summary>
            public Index()
            {
                this._keys = new List<KeyDef>();
                this._index = new Dictionary<string, List<XPathNavigator>>();
            }

            /// <summary>
            /// Adds a key.
            /// </summary>
            /// <param name="key">Key definition</param>
            public void AddKey(KeyDef key)
            {
                this._keys.Add(key);
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

                        if (this._index.TryGetValue(nodes.Current.Value, out tmpIndexedNodes))
                        {
                            if (indexedNodes == null)
                                indexedNodes = new List<XPathNavigator>();
                            indexedNodes.AddRange(tmpIndexedNodes);
                        }
                    }
                }
                else
                {
                    this._index.TryGetValue(keyValue.ToString(), out indexedNodes);
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
                foreach (KeyDef keyDef in this._keys)
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
                if (!this._index.TryGetValue(key, out indexedNodes))
                {
                    indexedNodes = new List<XPathNavigator>();
                    this._index.Add(key, indexedNodes);
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
            private IDictionary<string, Index> _indexes;
            private XPathNavigator _nav;
            private bool _indexed;

            /// <summary>
            /// Adds new key to the named index.
            /// </summary>
            /// <param name="nav">XPathNavigator over XML document to be indexed</param>
            /// <param name="indexName">Index name</param>
            /// <param name="key">Key definition</param>
            public void AddKey(XPathNavigator nav, string indexName, KeyDef key)
            {
                this._indexed = false;
                this._nav = nav;
                //Named indexes are stored in a hashtable.
                if (this._indexes == null)
                    this._indexes = new Dictionary<string, Index>();
                Index index;
                if (!this._indexes.TryGetValue(indexName, out index))
                {
                    index = new Index();
                    this._indexes.Add(indexName, index);
                }
                index.AddKey(key);
            }

            /// <summary>
            /// Builds indexes.
            /// </summary>
            public void BuildIndexes()
            {
                // TODO hack to prevent null ref exception in case there is no index specifications
                if (this._nav == null)
                    return;

                XPathNavigator doc = this._nav.Clone();
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
                            foreach (Index index in this._indexes.Values)
                                index.MatchNode(tempNav);
                        }
                        //ni.Current.MoveToParent();

                        tempNav = ni.Current.Clone();
                        //process attributes
                        for (bool go = tempNav.MoveToFirstAttribute(); go; go = tempNav.MoveToNextAttribute())
                        {
                            foreach (Index index in this._indexes.Values)
                                index.MatchNode(tempNav);
                        }
                        //ni.Current.MoveToParent();
                    }

                    foreach (Index index in this._indexes.Values)
                        index.MatchNode(ni.Current);
                }
                this._indexed = true;
            }

            /// <summary>
            /// Get indexed nodes by index name and key value.
            /// </summary>    
            /// <param name="indexName">Index name</param>
            /// <param name="value">Key value</param>
            /// <returns>Indexed nodes</returns>
            public XPathNodeIterator GetNodes(string indexName, object value)
            {
                if (!this._indexed)
                    BuildIndexes();
                Index index;
                this._indexes.TryGetValue(indexName, out index);
                return index == null ? null : index.GetNodes(value);
            }
        }

        #endregion IndexManager


    }
}