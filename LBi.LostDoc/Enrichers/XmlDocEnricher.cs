/*
 * Copyright 2012 LBi Netherlands B.V.
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
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace LBi.LostDoc.Enrichers
{
    public class XmlDocEnricher : IEnricher
    {
        private Dictionary<Assembly, XmlDocReader> _docReaders;
        private List<string> _paths;
        private XslCompiledTransform _xslTransform;

        public XmlDocEnricher()
        {
            this._docReaders = new Dictionary<Assembly, XmlDocReader>();
            this._paths = new List<string>();
            this._xslTransform = new XslCompiledTransform();
            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("LBi.LostDoc.Enrichers.enrich-doc-comments.xslt"))
            {
                XmlReader reader = XmlReader.Create(resource);
                this._xslTransform.Load(reader);
            }
        }

        #region IEnricher Members

        public void EnrichType(IProcessingContext context, Type type)
        {
            XmlDocReader reader = this.GetDocReader(type.Assembly);
            if (reader != null)
            {
                XElement element = reader.GetDocComments(type);
                if (element != null)
                    this.RewriteXml(context, type.Assembly, element, "typeparam");
            }
        }

        public void EnrichConstructor(IProcessingContext context, ConstructorInfo ctor)
        {
            XmlDocReader reader = this.GetDocReader(ctor.DeclaringType.Assembly);
            if (reader != null)
            {
                XElement element = reader.GetDocComments(ctor);
                if (element != null)
                    this.RewriteXml(context, ctor.ReflectedType.Assembly, element, "param", "typeparam");
            }
        }

        public void EnrichParameter(IProcessingContext context, ParameterInfo parameter)
        {
            XmlDocReader reader = this.GetDocReader(parameter.Member.ReflectedType.Assembly);
            if (reader != null)
            {
                XNamespace ns = "urn:doc";
                XElement element = reader.GetDocComments(parameter);
                if (element != null)
                    this.RewriteXmlContent(context, parameter.Member.ReflectedType.Assembly, "summary", element);
            }
        }

        public void EnrichAssembly(IProcessingContext context, Assembly asm)
        {
        }

        public void RegisterNamespace(IProcessingContext context)
        {
            context.Element.Add(new XAttribute(XNamespace.Xmlns + "doc", "urn:doc"));
        }

        public void EnrichMethod(IProcessingContext context, MethodInfo mInfo)
        {
            XElement element = this.GetMethodDocComments(mInfo);
            if (element == null)
            {
                HashSet<MethodInfo> seen = new HashSet<MethodInfo>();
                seen.Add(mInfo);

                MethodInfo baseMethod = mInfo.GetBaseDefinition();
                while (seen.Add(baseMethod))
                {
                    element = this.GetMethodDocComments(baseMethod);
                    if (element != null)
                        break;
                }

                if (element == null && !mInfo.DeclaringType.IsInterface)
                    element = this.GetMethodDocComments(baseMethod);
            }

            if (element != null)
            {
                this.RewriteXml(context,
                                mInfo.ReflectedType.Assembly,
                                element,
                                "param",
                                "typeparam",
                                "filterpriority",
                                "returns");
            }
        }

        public void EnrichField(IProcessingContext context, FieldInfo fieldInfo)
        {
            XmlDocReader reader = this.GetDocReader(fieldInfo.ReflectedType.Assembly);
            if (reader != null)
            {
                XElement element = reader.GetDocComments(fieldInfo);
                if (element != null)
                    this.RewriteXml(context, fieldInfo.ReflectedType.Assembly, element);
            }
        }

        public void EnrichProperty(IProcessingContext context, PropertyInfo propertyInfo)
        {
            XmlDocReader reader = this.GetDocReader(propertyInfo.ReflectedType.Assembly);
            if (reader != null)
            {
                XElement element = reader.GetDocComments(propertyInfo);
                if (element != null)
                    this.RewriteXml(context, propertyInfo.ReflectedType.Assembly, element);
            }
        }

        public void EnrichReturnValue(IProcessingContext context, MethodInfo methodInfo)
        {
            XmlDocReader reader = this.GetDocReader(methodInfo.ReflectedType.Assembly);
            if (reader != null)
            {
                XNamespace ns = "urn:doc";
                XElement element = reader.GetDocCommentsReturnParameter(methodInfo.ReturnParameter);
                if (element != null)
                    this.RewriteXmlContent(context, methodInfo.ReflectedType.Assembly, "summary", element);
            }
        }

        public void EnrichTypeParameter(IProcessingContext context, Type typeParameter)
        {
            if (typeParameter.DeclaringMethod != null)
                this.EnrichTypeParameter(context, (MethodInfo)typeParameter.DeclaringMethod, typeParameter);
            else
                EnrichTypeParameter(context, typeParameter.DeclaringType, typeParameter);
        }

        public void EnrichNamespace(IProcessingContext context, string ns)
        {
        }

        public void EnrichEvent(IProcessingContext context, EventInfo eventInfo)
        {
            XmlDocReader reader = this.GetDocReader(eventInfo.ReflectedType.Assembly);
            if (reader != null)
            {
                XElement element = reader.GetDocComments(eventInfo);
                if (element != null)
                    this.RewriteXml(context, eventInfo.ReflectedType.Assembly, element);
            }
        }

        #endregion

        private XmlDocReader GetDocReader(Assembly assembly)
        {
            XmlDocReader reader;

            if (!this._docReaders.TryGetValue(assembly, out reader))
            {
                string path = Path.Combine(Path.GetDirectoryName(assembly.Location),
                                           Path.GetFileNameWithoutExtension(assembly.Location) + ".xml");

                if (!File.Exists(path))
                {
                    // check alt paths
                    foreach (string dir in this._paths)
                    {
                        path = Path.Combine(dir, Path.GetFileNameWithoutExtension(assembly.Location) + ".xml");
                        if (File.Exists(path))
                            break;
                    }
                }

                if (File.Exists(path))
                {
                    this._docReaders.Add(assembly, reader = new XmlDocReader());

                    using (XmlReader xreader = XmlReader.Create(path))
                        reader.Load(xreader);
                }
                else
                    reader = null;
            }

            return reader;
        }

        private void RewriteXml(IProcessingContext context, Assembly hintAssembly, XElement element, params string[] exclude)
        {
            element = this.EnrichXml(context, hintAssembly, element);
            XNamespace ns = "urn:doc";
            foreach (XElement elem in element.Elements())
            {
                if (exclude.Contains(elem.Name.LocalName))
                    continue;

                context.Element.Add(new XElement(ns + elem.Name.LocalName, elem.Attributes(), elem.Nodes()));
            }
        }

        private void RewriteXmlContent(IProcessingContext context, Assembly hintAssembly, string container, XElement element)
        {
            element = this.EnrichXml(context, hintAssembly, element);
            XNamespace ns = "urn:doc";
            if (element.Nodes().Any())
                context.Element.Add(new XElement(ns + container, element.Attributes(), element.Nodes()));
        }

        private XElement EnrichXml(IProcessingContext context, Assembly hintAssembly, XElement nodes)
        {
            XDocument ret = new XDocument();

            using (XmlWriter nodeWriter = ret.CreateWriter())
            {
                XsltArgumentList argList = new XsltArgumentList();
                argList.AddExtensionObject("urn:lostdoc-core", new AssetVersionResolver(context, hintAssembly));

                this._xslTransform.Transform(nodes.CreateNavigator(), argList, nodeWriter);
                nodeWriter.Close();
            }

            return ret.Root;
        }

        private XElement GetMethodDocComments(MethodInfo mInfo)
        {
            XmlDocReader reader = this.GetDocReader(mInfo.DeclaringType.Assembly);
            if (reader != null)
                return reader.GetDocComments(mInfo);
            return null;
        }

        public void EnrichTypeParameter(IProcessingContext context, MethodInfo methodInfo, Type typeParameter)
        {
            XmlDocReader reader = this.GetDocReader(methodInfo.ReflectedType.Assembly);
            if (reader != null)
            {
                XNamespace ns = "urn:doc";
                XElement element = reader.GetTypeParameterSummary(methodInfo, typeParameter);
                if (element != null)
                    this.RewriteXmlContent(context, methodInfo.ReflectedType.Assembly, "summary", element);
            }
        }

        public void EnrichTypeParameter(IProcessingContext context, Type type, Type typeParameter)
        {
            XmlDocReader reader = this.GetDocReader(type.Assembly);
            if (reader != null)
            {
                XNamespace ns = "urn:doc";
                XElement element = reader.GetTypeParameterSummary(type, typeParameter);
                if (element != null)
                    this.RewriteXmlContent(context, typeParameter.Assembly, "summary", element);
            }
        }

        public void AddPath(string bclDocPath)
        {
            this._paths.Add(bclDocPath);
        }
    }
}
