﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace LBi.LostDoc.Core.Enrichers
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
            using (
                Stream resource =
                    Assembly.GetExecutingAssembly().GetManifestResourceStream(
                                                                              "LBi.LostDoc.Core.Enrichers.enrich-doc-comments.xslt")
                )
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
                    this.RewriteXml(context, element, "typeparam");
            }
        }

        public void EnrichConstructor(IProcessingContext context, ConstructorInfo ctor)
        {
            XmlDocReader reader = this.GetDocReader(ctor.DeclaringType.Assembly);
            if (reader != null)
            {
                XElement element = reader.GetDocComments(ctor);
                if (element != null)
                    this.RewriteXml(context, element, "param", "typeparam");
            }
        }

        public void EnrichParameter(IProcessingContext context, ParameterInfo parameter)
        {
            XmlDocReader reader = this.GetDocReader(parameter.Member.DeclaringType.Assembly);
            if (reader != null)
            {
                XNamespace ns = "urn:doc";
                XElement element = reader.GetDocComments(parameter);
                if (element != null)
                    this.RewriteXmlContent(context, "summary", element);
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
                this.RewriteXml(context, element, "param", "typeparam", "filterpriority", "returns");
        }

        public void EnrichField(IProcessingContext context, FieldInfo fieldInfo)
        {
            XmlDocReader reader = this.GetDocReader(fieldInfo.DeclaringType.Assembly);
            if (reader != null)
            {
                XElement element = reader.GetDocComments(fieldInfo);
                if (element != null)
                    this.RewriteXml(context, element);
            }
        }

        public void EnrichProperty(IProcessingContext context, PropertyInfo propertyInfo)
        {
            XmlDocReader reader = this.GetDocReader(propertyInfo.DeclaringType.Assembly);
            if (reader != null)
            {
                XElement element = reader.GetDocComments(propertyInfo);
                if (element != null)
                    this.RewriteXml(context, element);
            }
        }

        public void EnrichReturnValue(IProcessingContext context, MethodInfo methodInfo)
        {
            XmlDocReader reader = this.GetDocReader(methodInfo.DeclaringType.Assembly);
            if (reader != null)
            {
                XNamespace ns = "urn:doc";
                XElement element = reader.GetDocCommentsReturnParameter(methodInfo.ReturnParameter);
                if (element != null)
                    this.RewriteXmlContent(context, "summary", element);
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

        public void EnrichEvent(IProcessingContext clone, EventInfo eventInfo)
        {
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

        private void RewriteXml
            (IProcessingContext context, XElement element, params string[] exclude)
        {
            element = this.EnrichXml(context, element);
            XNamespace ns = "urn:doc";
            foreach (XElement elem in element.Elements())
            {
                if (exclude.Contains(elem.Name.LocalName))
                    continue;

                context.Element.Add(new XElement(ns + elem.Name.LocalName, elem.Attributes(), elem.Nodes()));
            }
        }

        private void RewriteXmlContent(IProcessingContext context, string container, XElement element)
        {
            element = this.EnrichXml(context, element);
            XNamespace ns = "urn:doc";
            if (element.Nodes().Any())
                context.Element.Add(new XElement(ns + container, element.Attributes(), element.Nodes()));
        }

        private XElement EnrichXml(IProcessingContext context, XElement nodes)
        {
            XDocument ret = new XDocument();

            using (XmlReader nodeReader = nodes.CreateReader())
            using (XmlWriter nodeWriter = ret.CreateWriter())
            {
                XsltArgumentList argList = new XsltArgumentList();
                argList.AddExtensionObject("urn:lostdoc-core", new AssetVersionResolver(context));
                this._xslTransform.Transform(nodeReader, argList, nodeWriter);
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
            XmlDocReader reader = this.GetDocReader(methodInfo.DeclaringType.Assembly);
            if (reader != null)
            {
                XNamespace ns = "urn:doc";
                XElement element = reader.GetTypeParameterSummary(methodInfo, typeParameter);
                if (element != null)
                    this.RewriteXmlContent(context, "summary", element);
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
                    this.RewriteXmlContent(context, "summary", element);
            }
        }

        public void AddPath(string bclDocPath)
        {
            this._paths.Add(bclDocPath);
        }
    }
}