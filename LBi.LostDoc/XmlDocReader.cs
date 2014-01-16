/*
 * Copyright 2012-2014 DigitasLBi Netherlands B.V.
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
using System.Xml;
using System.Xml.Linq;
using LBi.LostDoc.Diagnostics;

namespace LBi.LostDoc
{
    public class XmlDocReader
    {
        private XDocument _doc;
        private Dictionary<string, XElement> _members;

        public void Load(XmlReader reader)
        {
            this._doc = XDocument.Load(reader, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
            this._members = new Dictionary<string, XElement>(StringComparer.Ordinal);
            foreach (XElement member in _doc.Element("doc").Element("members").Elements("member"))
            {
                try
                {
                    this._members.Add(member.Attribute("name").Value, member);
                }
                catch (ArgumentException)
                {
                    TraceSources.GeneratorSource.TraceWarning("Duplicate member in xml documentation file: " + member.Attribute("name").Value);
                }
            }
        }

        public XElement GetDocComments(Asset asset)
        {
            return this.GetMemberElement(asset.Id.AssetId);
        }


        private XElement GetMemberElement(string signature)
        {
            XElement ret;
            if (!this._members.TryGetValue(signature, out ret))
                ret = null;

            return ret;
        }
    }
}
