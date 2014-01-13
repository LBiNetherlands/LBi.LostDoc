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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Templating.AssetResolvers;
using LBi.LostDoc.Templating.FileProviders;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{

    // TODO figure out how to untangle this mess of a class, extract parsing to TemplateParser
    // TODO fix error handling, a bad template.xml file will just throw random exceptions, xml schema validation?
    public class TemplateParser
    {
        public const string TemplateDefinitionFileName = "template.xml";

        //private readonly ObjectCache _cache;
        private readonly CompositionContainer _container;
        private readonly FileResolver _fileResolver;
        private readonly List<IAssetUriResolver> _resolvers;
        private readonly IUniqueUriFactory _uriFactory;
        private string _basePath;
        private XDocument _templateDefinition;
        private string _templateSourcePath;
        private TemplateInfo _templateInfo;
        private readonly DependencyProvider _dependencyProvider;
        private CancellationTokenSource _cancellationTokenSource;

        public TemplateParser(CompositionContainer container)
        {
            this._cache = new MemoryCache("TemplateCache");
            this._fileResolver = new FileResolver();
            this._resolvers = new List<IAssetUriResolver>();
            this._resolvers.Add(this._fileResolver);
            this._resolvers.Add(new MsdnResolver());
            this._uriFactory = new DefaultUniqueUriFactory();
            this._container = container;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._dependencyProvider = new DependencyProvider(this._cancellationTokenSource.Token);
        }

        #region LoadFrom Template

        public virtual void Load(TemplateInfo templateInfo)
        {
            this._templateInfo = templateInfo;
            this._templateSourcePath = null;
            using (Stream str = this._templateInfo.Source.OpenFile(templateInfo.Path, FileMode.Open))
                _templateDefinition = XDocument.Load(str, LoadOptions.SetLineInfo);

            this._basePath = Path.GetDirectoryName(templateInfo.Path);
        }

        protected virtual IFileProvider GetScopedFileProvider()
        {
            return new ScopedFileProvider(this._templateInfo.Source, this._basePath);
        }

        private IEnumerable<AssetRegistration> ParseAssetRegistration(IEnumerable<XElement> elements)
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

        private IEnumerable<SectionRegistration> ParseSectionRegistration(IEnumerable<XElement> elements)
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

        private IEnumerable<XPathVariable> ParseVariables(XElement element)
        {
            return element.Attributes()
                          .Where(a => a.Name.NamespaceName == Namespaces.Variable)
                          .Select(a => new ExpressionXPathVariable(a.Name.LocalName, a.Value));
        }

        private IEnumerable<XPathVariable> ParseParams(IEnumerable<XElement> elements)
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




        #endregion

        public virtual Template PrepareTemplate(TemplateSettings settings)
        {
            Stack<IFileProvider> providers = new Stack<IFileProvider>();
            providers.Push(new HttpFileProvider());
            providers.Push(new DirectoryFileProvider());
           
            return this.PrepareTemplate(settings, providers);
        }

        protected virtual Template PrepareTemplate(TemplateSettings xsettings, Stack<IFileProvider> providers)
        {
            // set up temp file container
            TempFileCollection tempFiles = new TempFileCollection(settings.TemporaryFilesPath,
                                                                  settings.KeepTemporaryFiles);

            if (!Directory.Exists(tempFiles.TempDir))
                Directory.CreateDirectory(tempFiles.TempDir);

            // clone orig doc
            XDocument workingDoc;

            // this is required to preserve the line information 
            using (var xmlReader = this._templateDefinition.CreateReader())
                workingDoc = XDocument.Load(xmlReader, LoadOptions.SetLineInfo);

            // template inheritence
            if (this._templateInfo.Inherits != null)
            {

                int depth = providers.Count + 1;
                TemplateParser inheritedTemplateParser = _templateInfo.Inherits.Load(this._container);
                Template template = inheritedTemplateParser.PrepareTemplate(settings, providers);

                providers.Push(inheritedTemplateParser.GetScopedFileProvider());

                // a little hacky but it should work with the Reverse()/AddFirst()
                foreach (XElement elem in template.Source.Root.Elements().Reverse())
                {
                    workingDoc.Root.AddFirst(new XElement(elem));
                }

                // create and register temp file (this can be overriden later if there are meta-template directives
                // in the template
                this._templateSourcePath = this.SaveTempFile(tempFiles, workingDoc, "inherited." + depth + '.' + _templateInfo.Name);
            }

            // add our file provider to the top of the stack
            providers.Push(this.GetScopedFileProvider());

            // create stacked provider
            IFileProvider provider = new StackedFileProvider(providers);

            // start by loading any parameters as they are needed for meta-template evaluation
            CustomXsltContext customContext = CustomXsltContext.Create(settings.IgnoredVersionComponent);

            XElement[] paramNodes = workingDoc.Root.Elements("parameter").ToArray();
            List<XPathVariable> globalParams = new List<XPathVariable>();

            foreach (XElement paramNode in paramNodes)
            {
                string name = paramNode.GetAttributeValue("name");
                object argValue;
                if (settings.Arguments.TryGetValue(name, out argValue))
                    globalParams.Add(new ConstantXPathVariable(name, argValue));
                else
                {
                    string expr = paramNode.GetAttributeValueOrDefault("select");
                    globalParams.Add(new ExpressionXPathVariable(name, expr));
                }
            }

            customContext.PushVariableScope(workingDoc, globalParams.ToArray());

            var arguments = settings.Arguments
                                        .Select(argument => new ConstantXPathVariable(argument.Key, argument.Value))
                                        .ToArray();

            customContext.PushVariableScope(workingDoc, arguments);

            // expand any meta-template directives
            workingDoc = ApplyMetaTransforms(workingDoc, customContext, provider, tempFiles);

            // there was neither inheretance, nor any meta-template directives
            if (this._templateSourcePath == null)
            {
                // save current template to disk
                this._templateSourcePath = this.SaveTempFile(tempFiles, workingDoc);
            }

            // loading template
            List<StylesheetDirective> stylesheets = new List<StylesheetDirective>();
            List<ResourceDirective> resources = new List<ResourceDirective>();
            List<IndexDirective> indices = new List<IndexDirective>();
            foreach (XElement elem in workingDoc.Root.Elements())
            {
                // we alread processed the parameters
                if (elem.Name.LocalName == "parameter")
                    continue;

                if (elem.Name.LocalName == "apply-stylesheet")
                {
                    stylesheets.Add(this.ParseStylesheet(provider, elem));
                }
                else if (elem.Name.LocalName == "index")
                {
                    indices.Add(this.ParseIndexDefinition(elem));
                }
                else if (elem.Name.LocalName == "include-resource")
                {
                    resources.Add(ParseResouceDefinition(provider, elem));
                }
                else
                {
                    throw new Exception("Unknown element: " + elem.Name.LocalName);
                }
            }

            return new Template
                       {
                           Parameters = globalParams.ToArray(),
                           Source = workingDoc,
                           ResourceDirectives = resources.ToArray(),
                           StylesheetsDirectives = stylesheets.ToArray(),
                           IndexDirectives = indices.ToArray(),
                           TemporaryFiles = tempFiles,
                           // add provider here?!
                       };
        }

        private string SaveTempFile(TempFileCollection tempFiles, XDocument workingDoc, string suffix = null)
        {
            var tempFileName = Path.Combine(tempFiles.TempDir,
                                            Path.GetDirectoryName(this._basePath)
                                            + TemplateDefinitionFileName
                                            + (suffix != null ? "." + suffix : ""));
            workingDoc.Save(tempFileName, SaveOptions.OmitDuplicateNamespaces);
            tempFiles.AddFile(tempFileName, tempFiles.KeepFiles);
            return tempFileName;
        }

        private ResourceDirective ParseResouceDefinition(IFileProvider provider, XElement elem)
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

        protected virtual XDocument ApplyMetaTransforms(XDocument workingDoc, CustomXsltContext customContext, IFileProvider provider, TempFileCollection tempFiles)
        {
            // check for meta-template directives and expand
            int metaCount = 0;
            XElement metaNode = workingDoc.Root.Elements("meta-template").FirstOrDefault();
            while (metaNode != null)
            {
                if (metaNode.EvaluateCondition(metaNode.GetAttributeValueOrDefault("condition"), customContext))
                {
                    XslCompiledTransform metaTransform = new XslCompiledTransform(true);
                    using (Stream str = provider.OpenFile(metaNode.GetAttributeValue("stylesheet"), FileMode.Open))
                    {
                        XmlReader reader = XmlReader.Create(str, new XmlReaderSettings { CloseInput = true, });
                        XsltSettings settings = new XsltSettings(true, true);
                        XmlResolver resolver = new XmlFileProviderResolver(provider);
                        metaTransform.Load(reader, settings, resolver);
                    }

                    XsltArgumentList xsltArgList = new XsltArgumentList();

                    // TODO this is a quick fix/hack
                    xsltArgList.AddExtensionObject(Namespaces.Template, new TemplateXsltExtensions(null, null));

                    var metaParamNodes = metaNode.Elements("with-param");

                    foreach (XElement paramNode in metaParamNodes)
                    {
                        string pName = paramNode.GetAttributeValue("name");
                        string pExpr = paramNode.GetAttributeValue("select");

                        try
                        {
                            xsltArgList.AddParam(pName,
                                                 string.Empty,
                                                 workingDoc.XPathEvaluate(pExpr, customContext));
                        }
                        catch (XPathException ex)
                        {
                            throw new TemplateException(this._templateSourcePath,
                                                        paramNode.Attribute("select"),
                                                        string.Format(
                                                            "Unable to process XPath expression: '{0}'",
                                                            pExpr),
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
                                                new XmlFileProviderResolver(provider));

                        outputWriter.Close();

                        // rewind stream
                        tempStream.Seek(0, SeekOrigin.Begin);
                        outputDoc = XDocument.Load(tempStream, LoadOptions.SetLineInfo);

                        // create and register temp file
                        // this will override the value set in PrepareTemplate in case of template inhertence
                        // TODO this is a bit hacky, maybe add Template.Name {get;} instead of this._basePath (which could be anything)
                        string filename = this._basePath + ".meta." + (++metaCount).ToString(CultureInfo.InvariantCulture);
                        this._templateSourcePath = this.SaveTempFile(tempFiles, outputDoc, filename);
                    }


                    TraceSources.TemplateSource.TraceVerbose("Template after transformation by {0}",
                                                             metaNode.GetAttributeValue("stylesheet"));

                    TraceSources.TemplateSource.TraceData(TraceEventType.Verbose, 1, outputDoc.CreateNavigator());

                    workingDoc = outputDoc;
                }
                else
                {
                    // didn't process, so remove it
                    metaNode.Remove();
                }

                // select next template
                metaNode = workingDoc.Root.Elements("meta-template").FirstOrDefault();
            }
            return workingDoc;
        }

        protected virtual IndexDirective ParseIndexDefinition(XElement elem)
        {
            return new IndexDirective(elem.GetAttributeValue("name"),
                                      elem.GetAttributeValue("match"),
                                      elem.GetAttributeValue("key"));
        }



        private StylesheetDirective ParseStylesheet(IFileProvider provider, XElement elem)
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
    }
}
