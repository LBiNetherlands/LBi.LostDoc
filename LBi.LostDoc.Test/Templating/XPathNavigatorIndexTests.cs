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

using System.Xml.Linq;
using System.Xml.XPath;
using LBi.LostDoc.Templating.XPath;
using Xunit;

namespace LBi.LostDoc.Test.Templating
{
    public class XPathNavigatorIndexTests
    {
        public class Create
        {
            [Fact]
            public void MatchElementSingleValue()
            {
                XDocument doc = new XDocument(new XElement("root",
                                                           new XElement("data", new XAttribute("id", 0)),
                                                           new XElement("data", new XAttribute("id", 1)),
                                                           new XElement("data", new XAttribute("id", 2)),
                                                           new XElement("data", new XAttribute("id", 3))));

                XPathNavigatorIndex index = XPathNavigatorIndex.Create(doc.CreateNavigator(), "*[@id]", "@id", null);

                for (int i = 0; i < 4; i++)
                {
                    XPathNodeIterator result = index.Get(i);
                    Assert.True(result.MoveNext());
                    Assert.Equal(i, int.Parse(result.Current.GetAttribute("id", string.Empty)));
                    Assert.False(result.MoveNext());
                }
            }

            [Fact]
            public void MatchAttributeSingleValue()
            {
                XDocument doc = new XDocument(new XElement("root",
                                                           new XElement("data", new XAttribute("id", 0)),
                                                           new XElement("data", new XAttribute("id", 1)),
                                                           new XElement("data", new XAttribute("id", 2)),
                                                           new XElement("data", new XAttribute("id", 3))));

                XPathNavigatorIndex index = XPathNavigatorIndex.Create(doc.CreateNavigator(), "@id", ".", null);

                for (int i = 0; i < 4; i++)
                {
                    XPathNodeIterator result = index.Get(i);
                    Assert.True(result.MoveNext());
                    Assert.Equal(i, result.Current.ValueAsInt);
                    Assert.False(result.MoveNext());
                }
            }

            [Fact]
            public void MatchElementMultipleValue()
            {
                XDocument doc = new XDocument(new XElement("root",
                                                           new XElement("data", new XAttribute("id", 0), 0),
                                                           new XElement("data", new XAttribute("id", 0), 1),
                                                           new XElement("data", new XAttribute("id", 0), 2),
                                                           new XElement("data", new XAttribute("id", 0), 3)));

                XPathNavigatorIndex index = XPathNavigatorIndex.Create(doc.CreateNavigator(), "*[@id]", "@id", null);

                XPathNodeIterator result = index.Get(0);
                for (int i = 0; i < 4; i++)
                {
                    Assert.True(result.MoveNext());
                    Assert.Equal(i, result.Current.ValueAsInt);
                }

                Assert.False(result.MoveNext());
            }
        }


        public class Get
        {
            [Fact]
            public void EnsureResultIsThreadSafe()
            {
                XDocument doc = new XDocument(new XElement("root",
                                                           new XElement("data", new XAttribute("id", 0), 0),
                                                           new XElement("data", new XAttribute("id", 0), 1),
                                                           new XElement("data", new XAttribute("id", 0), 2),
                                                           new XElement("data", new XAttribute("id", 0), 3)));

                XPathNavigatorIndex index = XPathNavigatorIndex.Create(doc.CreateNavigator(), "*[@id]", "@id", null);

                XPathNodeIterator result = index.Get(0);
                XPathNodeIterator result2 = index.Get(0);

                for (int i = 0; i < 4; i++)
                {
                    Assert.True(result.MoveNext());
                    Assert.Equal(i, result.Current.ValueAsInt);

                    Assert.True(result2.MoveNext());
                    Assert.Equal(i, result2.Current.ValueAsInt);
                }

                Assert.False(result.MoveNext());
                Assert.False(result2.MoveNext());                
            }

            [Fact]
            public void EnsureResultCloneable()
            {
                XDocument doc = new XDocument(new XElement("root",
                                                           new XElement("data", new XAttribute("id", 0), 0),
                                                           new XElement("data", new XAttribute("id", 0), 1),
                                                           new XElement("data", new XAttribute("id", 0), 2),
                                                           new XElement("data", new XAttribute("id", 0), 3)));

                XPathNavigatorIndex index = XPathNavigatorIndex.Create(doc.CreateNavigator(), "*[@id]", "@id", null);

                XPathNodeIterator result = index.Get(0);
                XPathNodeIterator result2 = result.Clone();

                for (int i = 0; i < 4; i++)
                {
                    Assert.True(result.MoveNext());
                    Assert.Equal(i, result.Current.ValueAsInt);

                    Assert.True(result2.MoveNext());
                    Assert.Equal(i, result2.Current.ValueAsInt);
                }

                Assert.False(result.MoveNext());
                Assert.False(result2.MoveNext());
            }
        }
    }
}
