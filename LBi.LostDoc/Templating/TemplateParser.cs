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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Templating.FileProviders;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{
    // TODO fix error handling, a bad template.xml file will just throw random exceptions, xml schema validation?
    public class TemplateParser
    {
        public const string TemplateDefinitionFileName = "template.xml";

        public TemplateParser()
        {
        }

        #region Preprocessing

        protected virtual XDocument ApplyMetaTransforms(TemplateInfo templateInfo,
                                                        XDocument workingDoc,
                                                        IFileProvider templateProvider,
                                                        IFileProvider tempFileProvider)
        {
            CustomXsltContext xsltContext = CustomXsltContext.Create(null);

            // check for meta-template directives and expand
            int metaCount = 0;
            XElement metaNode = workingDoc.Root.Elements("meta-template").FirstOrDefault();
            while (metaNode != null)
            {
                XslCompiledTransform metaTransform = new XslCompiledTransform(true);
                using (Stream str = templateProvider.OpenFile(metaNode.GetAttributeValue("stylesheet"), FileMode.Open))
                {
                    XmlReader reader = XmlReader.Create(str, new XmlReaderSettings { CloseInput = true, });
                    XsltSettings settings = new XsltSettings(true, true);
                    XmlResolver resolver = new XmlFileProviderResolver(templateProvider);
                    metaTransform.Load(reader, settings, resolver);
                }

                XsltArgumentList xsltArgList = new XsltArgumentList();

                // TODO this is a quick fix/hack
                xsltArgList.AddExtensionObject(Namespaces.Template, new TemplateXsltExtensions(null, null));

                var metaParamNodes = metaNode.Elements("with-param");

                foreach (XElement paramNode in metaParamNodes)
                {
                    string pName = paramNode.GetAttributeValue("name");
                    string pExpr = paramNode.GetAttributeValue("value");

                    try
                    {
                        xsltArgList.AddParam(pName,
                                             string.Empty,
                                             workingDoc.EvaluateValue(pExpr, xsltContext));
                    }
                    catch (XPathException ex)
                    {
                        throw new TemplateException(new FileReference(0, templateInfo.Source, templateInfo.Path),
                                                    paramNode.Attribute("select"),
                                                    string.Format("Unable to process XPath expression: '{0}'. {1}", pExpr, ex.Message),
                                                    ex);
                    }
                }

                // this isn't very nice, but I can't figure out another way to get LineInfo included in the transformed document
                XDocument outputDoc;
                using (MemoryStream tempStream = new MemoryStream())
                using (XmlWriter outputWriter = XmlWriter.Create(tempStream, new XmlWriterSettings { Indent = true }))
                {

                    metaTransform.Transform(workingDoc.CreateNavigator(),
                                            xsltArgList,
                                            outputWriter,
                                            new XmlFileProviderResolver(templateProvider));

                    outputWriter.Close();

                    // rewind stream
                    tempStream.Seek(0, SeekOrigin.Begin);
                    outputDoc = XDocument.Load(tempStream, LoadOptions.SetLineInfo);

                    // create and register temp file
                    string filename = templateInfo.Name + ".meta." +
                                      (++metaCount).ToString(CultureInfo.InvariantCulture);
                    this.SaveTempFile(tempFileProvider, outputDoc, filename);
                }

                TraceSources.TemplateSource.TraceVerbose("Template after transformation by {0}",
                                                         metaNode.GetAttributeValue("stylesheet"));

                TraceSources.TemplateSource.TraceData(TraceEventType.Verbose, 1, outputDoc.CreateNavigator());

                workingDoc = outputDoc;

                // select next template
                metaNode = workingDoc.Root.Elements("meta-template").FirstOrDefault();
            }
            return workingDoc;
        }

        protected virtual XDocument PreProcess(TemplateInfo templateInfo,
                                               Stack<IFileProvider> providerStack,
                                               IFileProvider tempFileProvider)
        {
            XDocument templateDefinition;

            using (Stream str = templateInfo.Source.OpenFile(templateInfo.Path, FileMode.Open))
                templateDefinition = XDocument.Load(str, LoadOptions.SetLineInfo);

            // template inheritence
            if (templateInfo.Inherits != null)
            {
                int depth = providerStack.Count + 1;

                XDocument inheritedSource = this.PreProcess(templateInfo.Inherits,
                                                            providerStack,
                                                            tempFileProvider);

                // a little hacky but it should work with the Reverse()/AddFirst()
                foreach (XElement elem in inheritedSource.Root.Elements().Reverse())
                    templateDefinition.Root.AddFirst(new XElement(elem));

                // create and register temp file
                this.SaveTempFile(tempFileProvider, templateDefinition, "inherited." + depth + '.' + templateInfo.Name);
            }

            // push template provider onto provider stack
            providerStack.Push(new ScopedFileProvider(templateInfo.Source, Path.GetDirectoryName(templateInfo.Path)));

            return this.ApplyMetaTransforms(templateInfo,
                                            templateDefinition,
                                            new StackedFileProvider(providerStack),
                                            tempFileProvider);
        }

        #endregion

        #region Parsing

        public virtual Template ParseTemplate(TemplateInfo templateInfo, IFileProvider tempFileProvider)
        {
            Stack<IFileProvider> providers = new Stack<IFileProvider>();
            providers.Push(new HttpFileProvider());
            providers.Push(new DirectoryFileProvider());

            XDocument workingDoc = this.PreProcess(templateInfo, providers, tempFileProvider);

            // save real template definition as temp file
            string tempFilename = this.SaveTempFile(tempFileProvider, workingDoc, "final");
            FileReference templateSource = new FileReference(0, tempFileProvider, tempFilename);

            // create stacked provider
            IFileProvider provider = new StackedFileProvider(providers);

            // loading template
            List<StylesheetDirective> stylesheets = new List<StylesheetDirective>();
            List<ResourceDirective> resources = new List<ResourceDirective>();
            List<IndexDirective> indices = new List<IndexDirective>();
            foreach (XElement elem in workingDoc.Root.Elements())
            {
                // we alread processed the parameters
                if (elem.Name.LocalName == "parameter")
                    continue;

                switch (elem.Name.LocalName)
                {
                    case "apply-stylesheet":
                        stylesheets.Add(this.ParseStylesheet(provider, elem));
                        break;
                    case "index":
                        indices.Add(this.ParseIndexDefinition(elem));
                        break;
                    case "include-resource":
                        resources.Add(this.ParseResouceDefinition(provider, elem));
                        break;
                    default:
                        throw new Exception("Unknown element: " + elem.Name.LocalName);
                }
            }

            return new Template(templateSource, provider, templateInfo.Parameters, resources, stylesheets, indices);
        }

        protected virtual IEnumerable<AssetRegistration> ParseAssetRegistration(IEnumerable<XElement> elements)
        {
            foreach (XElement elem in elements)
            {
                yield return new AssetRegistration
                             {
                                 Variables = this.ParseVariables(elem).ToArray(),
                                 SelectExpression = elem.GetAttributeValueOrDefault("select", "."),
                                 AssetIdExpression = elem.GetAttributeValue("assetId"),
                                 VersionExpression = elem.GetAttributeValue("version"),
                             };
            }
        }

        protected virtual IEnumerable<SectionRegistration> ParseSectionRegistration(IEnumerable<XElement> elements)
        {
            foreach (XElement elem in elements)
            {
                yield return new SectionRegistration
                             {
                                 SelectExpression = elem.GetAttributeValue("select"),
                                 NameExpression = elem.GetAttributeValue("name"),
                                 AssetIdExpression = elem.GetAttributeValue("assetId"),
                                 VersionExpression = elem.GetAttributeValue("version"),
                                 ConditionExpression = elem.GetAttributeValueOrDefault("condition"),
                                 Variables = this.ParseVariables(elem).ToArray(),
                             };
            }
        }

        protected virtual IEnumerable<XPathVariable> ParseVariables(XElement element)
        {
            return element.Attributes()
                          .Where(a => a.Name.NamespaceName == Namespaces.Variable)
                          .Select(a => new ExpressionXPathVariable(a.Name.LocalName, a.Value));
        }

        protected virtual IEnumerable<XPathVariable> ParseParams(IEnumerable<XElement> elements)
        {
            foreach (XElement elem in elements)
            {
                if (elem.Name.LocalName == "with-param")
                {
                    yield return new ExpressionXPathVariable(elem.GetAttributeValue("name"),
                                                             elem.GetAttributeValue("select"));
                }
                else
                {
                    throw new Exception("Unknown element:" + elem.Name.LocalName);
                }
            }
        }

        protected virtual IndexDirective ParseIndexDefinition(XElement elem)
        {
            return new IndexDirective(elem.GetAttributeValue("name"),
                                      elem.GetAttributeValue("match"),
                                      elem.GetAttributeValue("key"));
        }

        protected virtual StylesheetDirective ParseStylesheet(IFileProvider provider, XElement elem)
        {

            var nameAttr = elem.Attribute("name");
            var src = elem.GetAttributeValue("stylesheet");
            string name;
            if (nameAttr != null)
            {
                name = nameAttr.Value;
                TraceSources.TemplateSource.TraceInformation("Loading stylesheet: {0} ({1})", name, src);
            }
            else
            {
                name = Path.GetFileNameWithoutExtension(src);
                TraceSources.TemplateSource.TraceInformation("Loading stylesheet: {0}", name);
            }

            var ret = new StylesheetDirective
            {
                Stylesheet = new FileReference(0, provider, src),
                SelectExpression = elem.GetAttributeValueOrDefault("select", "/"),
                InputExpression = elem.GetAttributeValueOrDefault("input"),
                OutputExpression = elem.GetAttributeValueOrDefault("output"),
                XsltParams = this.ParseParams(elem.Elements("with-param")).ToArray(),
                Variables = this.ParseVariables(elem).ToArray(),
                Name = name,
                Sections = this.ParseSectionRegistration(elem.Elements("register-section")).ToArray(),
                AssetRegistrations = this.ParseAssetRegistration(elem.Elements("register-asset")).ToArray(),
                ConditionExpression = elem.GetAttributeValueOrDefault("condition"),
            };

            return ret;
        }

        protected virtual ResourceDirective ParseResouceDefinition(IFileProvider provider, XElement elem)
        {
            string source = elem.GetAttributeValue("path");

            XAttribute outputAttr = elem.Attribute("output");

            string output;
            if (outputAttr != null)
                output = outputAttr.Value;
            else
                output = '\'' + Path.GetFileName(source) + '\'';

            List<ResourceTransform> transforms = new List<ResourceTransform>();
            foreach (XElement transform in elem.Elements("transform"))
            {
                string transformName = transform.GetAttributeValue("name");

                var transformParams = transform.Elements("with-param")
                                               .Select(p => new ExpressionXPathVariable(p.GetAttributeValue("name"),
                                                                                        p.GetAttributeValue("select")));

                transforms.Add(new ResourceTransform(transformName, transformParams));
            }

            var resource = new ResourceDirective(elem.GetAttributeValueOrDefault("condition"),
                                        this.ParseVariables(elem).ToArray(),
                                        provider,
                                        source,
                                        output,
                                        transforms.ToArray());
            return resource;
        }

        #endregion

        private string SaveTempFile(IFileProvider tempFiles, XDocument workingDoc, string suffix = null)
        {
            string tempFileName = TemplateDefinitionFileName + (suffix != null ? "." + suffix : "");

            using (var stream = tempFiles.OpenFile(tempFileName, FileMode.Create))
                workingDoc.Save(stream, SaveOptions.OmitDuplicateNamespaces);

            return tempFileName;
        }
    }
}
