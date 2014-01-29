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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using LBi.LostDoc.Reflection;

namespace LBi.LostDoc.Enrichers
{
    //public class XmlDocEnricher : IEnricher
    //{
    //    private readonly Dictionary<Assembly, XmlDocReader> _docReaders;
    //    private readonly List<string> _paths;
    //    private readonly XslCompiledTransform _xslTransform;

    //    public XmlDocEnricher()
    //    {
    //        this._docReaders = new Dictionary<Assembly, XmlDocReader>();
    //        this._paths = new List<string>();
    //        this._xslTransform = new XslCompiledTransform();
    //        using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("LBi.LostDoc.Enrichers.enrich-doc-comments.xslt"))
    //        using (XmlReader reader = XmlReader.Create(resource, new XmlReaderSettings { CloseInput = false }))
    //        {
    //            this._xslTransform.Load(reader, new XsltSettings(false, true), null);
    //        }
    //    }

    //    #region IEnricher Members

    //    public void EnrichType(IProcessingContext context, Asset typeAsset)
    //    {
    //        XmlDocReader reader = this.GetDocReader(context, ReflectionServices.GetAssembly(typeAsset));
    //        if (reader != null)
    //        {
    //            XElement element = reader.GetDocComments(typeAsset);
    //            if (element != null)
    //                this.RewriteXml(context, typeAsset, element, "typeparam");
    //        }
    //    }

    //    public void EnrichConstructor(IProcessingContext context, Asset ctorAsset)
    //    {
    //        XmlDocReader reader = this.GetDocReader(context, ReflectionServices.GetAssembly(ctorAsset));
    //        if (reader != null)
    //        {
    //            XElement element = reader.GetDocComments(ctorAsset);
    //            if (element != null)
    //                this.RewriteXml(context, ctorAsset, element, "param", "typeparam");
    //        }
    //    }

    //    public void EnrichParameter(IProcessingContext context, Asset methodAsset, string parameterName)
    //    {
    //        XmlDocReader reader = this.GetDocReader(context, ReflectionServices.GetAssembly(methodAsset));
    //        if (reader != null)
    //        {
    //            XElement element = reader.GetDocComments(methodAsset);
    //            if (element != null)
    //            {
    //                element = element.XPathSelectElement(string.Format("param[@name='{0}']", parameterName));

    //                if (element != null)
    //                    this.RewriteXmlContent(context, methodAsset, "summary", element);
    //            }
    //        }
    //    }

    //    public void EnrichAssembly(IProcessingContext context, Asset assemblyAsset)
    //    {
    //    }

    //    public void RegisterNamespace(IProcessingContext context)
    //    {
    //        context.Element.Add(new XAttribute(XNamespace.Xmlns + "xdc", Namespaces.XmlDocComment));
    //    }

    //    public void EnrichMethod(IProcessingContext context, Asset methodAsset)
    //    {
    //        XmlDocReader reader = this.GetDocReader(context, ReflectionServices.GetAssembly(methodAsset));
    //        if (reader != null)
    //        {
    //            XElement element = reader.GetDocComments(methodAsset);


    //            if (element != null)
    //            {
    //                this.RewriteXml(context,
    //                                methodAsset,
    //                                element,
    //                                "param",
    //                                "typeparam",
    //                                "filterpriority",
    //                                "returns");
    //            }
    //        }
    //    }

    //    public void EnrichField(IProcessingContext context, Asset fieldAsset)
    //    {
    //        XmlDocReader reader = this.GetDocReader(context, ReflectionServices.GetAssembly(fieldAsset));
    //        if (reader != null)
    //        {
    //            XElement element = reader.GetDocComments(fieldAsset);
    //            if (element != null)
    //                this.RewriteXml(context, fieldAsset, element);
    //        }
    //    }

    //    public void EnrichProperty(IProcessingContext context, Asset propertyAsset)
    //    {
    //        XmlDocReader reader = this.GetDocReader(context, ReflectionServices.GetAssembly(propertyAsset));
    //        if (reader != null)
    //        {
    //            XElement element = reader.GetDocComments(propertyAsset);
    //            if (element != null)
    //                this.RewriteXml(context, propertyAsset, element);
    //        }
    //    }

    //    public void EnrichReturnValue(IProcessingContext context, Asset methodAsset)
    //    {
    //        XmlDocReader reader = this.GetDocReader(context, ReflectionServices.GetAssembly(methodAsset));
    //        if (reader != null)
    //        {
    //            XElement element = reader.GetDocComments(methodAsset);
    //            if (element != null)
    //            {
    //                element = element.XPathSelectElement("returns");

    //                if (element != null)
    //                    this.RewriteXmlContent(context, methodAsset, "summary", element);
    //            }
    //        }
    //    }

    //    public void EnrichNamespace(IProcessingContext context, Asset namespaceAsset)
    //    {
    //    }

    //    public void EnrichEvent(IProcessingContext context, Asset eventAsset)
    //    {
    //        XmlDocReader reader = this.GetDocReader(context, ReflectionServices.GetAssembly(eventAsset));
    //        if (reader != null)
    //        {
    //            XElement element = reader.GetDocComments(eventAsset);
    //            if (element != null)
    //                this.RewriteXml(context, eventAsset, element);
    //        }
    //    }

    //    #endregion

    //    private XmlDocReader GetDocReader(IProcessingContext context, Assembly assembly)
    //    {
    //        XmlDocReader reader;

    //        if (!this._docReaders.TryGetValue(assembly, out reader))
    //        {
    //            string path = Path.Combine(Path.GetDirectoryName(assembly.Location),
    //                                       Path.GetFileNameWithoutExtension(assembly.Location) + ".xml");

    //            if (!File.Exists(path))
    //            {
    //                // check alt paths
    //                foreach (string dir in this._paths)
    //                {
    //                    path = Path.Combine(dir, Path.GetFileNameWithoutExtension(assembly.Location) + ".xml");
    //                    if (File.Exists(path))
    //                        break;
    //                }
    //            }

    //            if (File.Exists(path))
    //            {
    //                this._docReaders.Add(assembly, reader = new XmlDocReader());
    //                using (XmlReader xreader = XmlReader.Create(path, new XmlReaderSettings { IgnoreWhitespace = false }))
    //                    reader.Load(xreader);
    //            }

    //            else
    //                reader = null;
    //        }

    //        return reader;
    //    }

    //    private void RewriteXml(IProcessingContext context, Asset asset, XElement element, params string[] exclude)
    //    {
    //        element = this.EnrichXml(context, asset, element);
    //        XNamespace ns = Namespaces.XmlDocComment;
    //        foreach (XElement elem in element.Elements())
    //        {
    //            if (exclude.Contains(elem.Name.LocalName))
    //                continue;

    //            context.Element.Add(new XElement(ns + elem.Name.LocalName, elem.Attributes(), elem.Nodes()));
    //        }
    //    }

    //    private void RewriteXmlContent(IProcessingContext context, Asset asset, string container, XElement element)
    //    {
    //        element = this.EnrichXml(context, asset, element);
    //        XNamespace ns = Namespaces.XmlDocComment;
    //        if (element.Nodes().Any())
    //            context.Element.Add(new XElement(ns + container, element.Attributes(), element.Nodes()));
    //    }

    //    private XElement EnrichXml(IProcessingContext context, Asset asset, XElement nodes)
    //    {
    //        XDocument ret = new XDocument();

    //        using (XmlWriter nodeWriter = ret.CreateWriter())
    //        {
    //            XsltArgumentList argList = new XsltArgumentList();
    //            argList.AddExtensionObject(Namespaces.Template, new AssetVersionResolver(context, asset));

    //            this._xslTransform.Transform(nodes.CreateNavigator(), argList, nodeWriter);
    //            nodeWriter.Close();
    //        }

    //        return ret.Root;
    //    }

    //    public void EnrichTypeParameter(IProcessingContext context, Asset methodOrTypeaAsset, string parameterName)
    //    {
    //        XmlDocReader reader = this.GetDocReader(context, ReflectionServices.GetAssembly(methodOrTypeaAsset));
    //        if (reader != null)
    //        {
    //            XElement element = reader.GetDocComments(methodOrTypeaAsset);
    //            if (element != null)
    //                element = element.XPathSelectElement(string.Format("typeparam[@name='{0}']", parameterName));

    //            if (element != null)
    //                this.RewriteXmlContent(context, methodOrTypeaAsset, "summary", element);
    //        }
    //    }

    //    public void AddPath(string bclDocPath)
    //    {
    //        this._paths.Add(bclDocPath);
    //    }
    //}
}
