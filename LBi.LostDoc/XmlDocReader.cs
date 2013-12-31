/*
 * Copyright 2012 DigitasLBi Netherlands B.V.
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
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
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

        public XElement GetDocComments(MethodInfo methodInfo)
        {
            string sig = Naming.GetAssetId(methodInfo);
            return this.GetMemberElement(sig);
        }

        public XElement GetDocComments(Type type)
        {
            return this.GetMemberElement(Naming.GetAssetId(type));
        }

        public XElement GetDocComments(ConstructorInfo ctor)
        {
            string sig = Naming.GetAssetId(ctor);
            return this.GetMemberElement(sig);
        }

        internal XElement GetDocComments(ParameterInfo parameter)
        {
            string sig;
            if (parameter.Member is ConstructorInfo)
                sig = Naming.GetAssetId((ConstructorInfo)parameter.Member);
            else if (parameter.Member is PropertyInfo)
                sig = Naming.GetAssetId((PropertyInfo)parameter.Member);
            else
                sig = Naming.GetAssetId((MethodInfo)parameter.Member);

            XElement elem = this.GetMemberElement(sig);
            if (elem != null)
                return elem.XPathSelectElement(string.Format("param[@name='{0}']", parameter.Name));
            return null;
        }

        internal XElement GetDocCommentsReturnParameter(ParameterInfo parameter)
        {
            string sig = Naming.GetAssetId((MethodInfo)parameter.Member);

            XElement elem = this.GetMemberElement(sig);
            if (elem != null)
                return elem.XPathSelectElement("returns");
            return null;
        }

        internal XElement GetDocComments(FieldInfo fieldInfo)
        {
            string sig = Naming.GetAssetId(fieldInfo);
            return this.GetMemberElement(sig);
        }

        internal XElement GetTypeParameterSummary(Type type, Type typeParameter)
        {
            string sig = Naming.GetAssetId(type);

            XElement elem = this.GetMemberElement(sig);
            if (elem != null)
                return elem.XPathSelectElement(string.Format("typeparam[@name='{0}']", typeParameter.Name));
            return null;
        }

        internal XElement GetTypeParameterSummary(MethodInfo methodInfo, Type typeParameter)
        {
            string sig = Naming.GetAssetId(methodInfo);

            XElement elem = this.GetMemberElement(sig);
            if (elem != null)
                return elem.XPathSelectElement(string.Format("typeparam[@name='{0}']", typeParameter.Name));
            return null;
        }

        private XElement GetMemberElement(string signature)
        {
            XElement ret;
            if (!this._members.TryGetValue(signature, out ret))
                ret = null;

            return ret;
        }

        public XElement GetDocComments(PropertyInfo propertyInfo)
        {
            string sig = Naming.GetAssetId(propertyInfo);
            return this.GetMemberElement(sig);
        }

        public XElement GetDocComments(EventInfo eventInfo)
        {
            string sig = Naming.GetAssetId(eventInfo);
            return this.GetMemberElement(sig);
        }
    }
}
