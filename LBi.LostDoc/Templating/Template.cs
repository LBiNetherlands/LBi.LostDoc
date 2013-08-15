/*
 * Copyright 2012-2013 LBi Netherlands B.V.
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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using LBi.LostDoc.Composition;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Templating.AssetResolvers;
using LBi.LostDoc.Templating.FileProviders;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{
    // TODO fix error handling, a bad template.xml file will just throw random exceptions, xml schema validation?
    public class Template
    {
        public const string TemplateDefinitionFileName = "template.xml";

        private readonly ObjectCache _cache;
        private readonly CompositionContainer _container;
        private readonly FileResolver _fileResolver;
        private readonly List<IAssetUriResolver> _resolvers;
        private string _basePath;
        private XDocument _templateDefinition;
        private string _templateSourcePath;
        private TemplateInfo _templateInfo;

        public event EventHandler<ProgressArgs> Progress;

        protected virtual void OnProgress(int percent)
        {
            EventHandler<ProgressArgs> handler = this.Progress;
            if (handler != null)
                handler(this, new ProgressArgs(percent));
        }

        public Template(CompositionContainer container)
        {
            this._cache = new MemoryCache("TemplateCache");
            this._fileResolver = new FileResolver();
            this._resolvers = new List<IAssetUriResolver>();
            this._resolvers.Add(this._fileResolver);
            this._resolvers.Add(new MsdnResolver());
            this._container = container;
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

        private IEnumerable<AliasRegistration> ParseAliasRegistration(IEnumerable<XElement> elements)
        {
            foreach (XElement elem in elements)
            {
                yield return
                    new AliasRegistration
                        {
                            Variables = this.ParseVariables(elem).ToArray(),
                            SelectExpression = this.GetAttributeValueOrDefault(elem, "select", "."),
                            AssetIdExpression = this.GetAttributeValue(elem, "assetId"),
                            VersionExpression = this.GetAttributeValue(elem, "version"),
                        };
            }
        }

        private IEnumerable<SectionRegistration> ParseSectionRegistration(IEnumerable<XElement> elements)
        {
            foreach (XElement elem in elements)
            {
                yield return
                    new SectionRegistration
                        {
                            SelectExpression = this.GetAttributeValue(elem, "select"),
                            NameExpression = this.GetAttributeValue(elem, "name"),
                            AssetIdExpression = this.GetAttributeValue(elem, "assetId"),
                            VersionExpression = this.GetAttributeValue(elem, "version"),
                            ConditionExpression = GetAttributeValueOrDefault(elem, "condition"),
                            Variables = this.ParseVariables(elem).ToArray(),
                        };
            }
        }

        private IEnumerable<XPathVariable> ParseVariables(XElement element)
        {
            return
                element.Attributes()
                       .Where(a => a.Name.NamespaceName == Namespaces.TemplateVariable)
                       .Select(a => new ExpressionXPathVariable(a.Name.LocalName, a.Value));
        }

        private IEnumerable<XPathVariable> ParseParams(IEnumerable<XElement> elements)
        {
            foreach (XElement elem in elements)
            {
                if (elem.Name.LocalName == "with-param")
                {
                    yield return new ExpressionXPathVariable(this.GetAttributeValue(elem, "name"),
                                                             this.GetAttributeValue(elem, "select"));
                }
                else
                {
                    throw new Exception("Unknown element:" + elem.Name.LocalName);
                }
            }
        }

        /// <summary>
        /// The load stylesheet.
        /// </summary>
        /// <param name="resourceProvider"></param>
        /// <param name="name">
        /// </param>
        /// <returns>
        /// </returns>
        private XslCompiledTransform LoadStylesheet(Stack<IFileProvider> resourceProvider, string name)
        {
            XslCompiledTransform ret = new XslCompiledTransform(true);

            foreach (var provider in resourceProvider)
            {
                if (provider.FileExists(name))
                {
                    using (Stream str = provider.OpenFile(name, FileMode.Open))
                    {
                        XmlReader reader = XmlReader.Create(str, new XmlReaderSettings {CloseInput = true,});
                        XsltSettings settings = new XsltSettings(false, true);
                        XmlResolver resolver = new XmlFileProviderResolver(resourceProvider);
                        ret.Load(reader, settings, resolver);
                    }
                    break;
                }
            }

            return ret;
        }

        #endregion

        protected virtual IEnumerable<StylesheetApplication> DiscoverWork(TemplateData templateData, XPathVariable[] parameters, Stylesheet stylesheet)
        {
            TraceSources.TemplateSource.TraceInformation("Processing stylesheet instructions: {0}",
                                                         (object)stylesheet.Name);

            CustomXsltContext xpathContext = CreateCustomXsltContext(templateData.IgnoredVersionComponent);

            xpathContext.PushVariableScope(templateData.Document.Root, parameters); // 1

            XElement[] inputElements =
                templateData.Document.XPathSelectElements(stylesheet.SelectExpression, xpathContext).ToArray();

            foreach (XElement inputElement in inputElements)
            {
                xpathContext.PushVariableScope(inputElement, stylesheet.Variables); // 2

                string saveAs = ResultToString(inputElement.XPathEvaluate(stylesheet.OutputExpression, xpathContext));
                string version = ResultToString(inputElement.XPathEvaluate(stylesheet.VersionExpression, xpathContext));
                string assetId = ResultToString(inputElement.XPathEvaluate(stylesheet.AssetIdExpression, xpathContext));
                List<AssetIdentifier> aliases = new List<AssetIdentifier>();
                List<AssetSection> sections = new List<AssetSection>();

                // eval condition, shortcut and log instead of wrapping entire loop in if
                if (!EvalCondition(xpathContext, inputElement, stylesheet.ConditionExpression))
                {
                    TraceSources.TemplateSource.TraceVerbose("{0}, {1} => Condition not met", assetId, version);
                    xpathContext.PopVariableScope(); // 2
                    continue;
                }

                Uri newUri = new Uri(saveAs, UriKind.RelativeOrAbsolute);

                // register url
                this._fileResolver.Add(assetId, new Version(version), ref newUri);

                TraceSources.TemplateSource.TraceVerbose("{0}, {1} => {2}", assetId, version, newUri.ToString());

                // aliases
                foreach (AliasRegistration alias in stylesheet.AssetAliases)
                {
                    XElement[] aliasInputElements =
                        inputElement.XPathSelectElements(alias.SelectExpression, xpathContext).ToArray();

                    foreach (XElement aliasInputElement in aliasInputElements)
                    {
                        xpathContext.PushVariableScope(aliasInputElement, alias.Variables); // 3

                        string aliasVersion =
                            ResultToString(aliasInputElement.XPathEvaluate(alias.VersionExpression, xpathContext));
                        string aliasAssetId =
                            ResultToString(aliasInputElement.XPathEvaluate(alias.AssetIdExpression, xpathContext));

                        // eval condition
                        if (EvalCondition(xpathContext, aliasInputElement, alias.ConditionExpression))
                        {
                            this._fileResolver.Add(aliasAssetId, new Version(aliasVersion), newUri);
                            aliases.Add(AssetIdentifier.Parse(aliasAssetId));
                            TraceSources.TemplateSource.TraceVerbose("{0}, {1} (Alias) => {2}", aliasAssetId,
                                                                     aliasVersion,
                                                                     newUri.ToString());
                        }
                        else
                        {
                            TraceSources.TemplateSource.TraceVerbose("{0}, {1} (Alias) => Condition not met",
                                                                     assetId,
                                                                     version);
                        }
                        xpathContext.PopVariableScope(); // 3
                    }
                }

                // sections
                foreach (SectionRegistration section in stylesheet.Sections)
                {
                    XElement[] sectionInputElements = inputElement.XPathSelectElements(section.SelectExpression, xpathContext).ToArray();

                    foreach (XElement sectionInputElement in sectionInputElements)
                    {
                        xpathContext.PushVariableScope(sectionInputElement, section.Variables); // 4

                        string sectionName =
                            ResultToString(sectionInputElement.XPathEvaluate(section.NameExpression, xpathContext));
                        string sectionVersion =
                            ResultToString(sectionInputElement.XPathEvaluate(section.VersionExpression, xpathContext));
                        string sectionAssetId =
                            ResultToString(sectionInputElement.XPathEvaluate(section.AssetIdExpression, xpathContext));

                        // eval condition
                        if (EvalCondition(xpathContext, sectionInputElement, section.ConditionExpression))
                        {
                            Uri sectionUri = new Uri(newUri + "#" + sectionName, UriKind.Relative);
                            this._fileResolver.Add(sectionAssetId, new Version(sectionVersion), sectionUri);
                            TraceSources.TemplateSource.TraceVerbose("{0}, {1}, (Section: {2}) => {3}",
                                                                     sectionAssetId,
                                                                     sectionVersion,
                                                                     sectionName,
                                                                     sectionUri.ToString());

                            sections.Add(new AssetSection(AssetIdentifier.Parse(sectionAssetId), sectionName, sectionUri));
                        }
                        else
                        {
                            TraceSources.TemplateSource.TraceVerbose("{0}, {1}, (Section: {2}) => Condition not met",
                                                                       sectionAssetId,
                                                                       sectionVersion,
                                                                       sectionName);
                        }

                        xpathContext.PopVariableScope(); // 4
                    }
                }

                var xsltParams = ResolveXsltParams(stylesheet.XsltParams, inputElement, xpathContext).ToArray();

                xpathContext.PopVariableScope(); // 2

                yield return new StylesheetApplication
                                 {
                                     StylesheetName = stylesheet.Name,
                                     Asset = new AssetIdentifier(assetId, new Version(version)),
                                     Aliases = aliases, /* list of AssetIdentifiers */
                                     Sections = sections, /* list of AssetSection */
                                     SaveAs = newUri.ToString(),
                                     Transform = stylesheet.Transform,
                                     InputElement = inputElement,
                                     XsltParams = xsltParams
                                 };                
            }

            xpathContext.PopVariableScope(); // 1
        }

        private static IEnumerable<KeyValuePair<string, object>> ResolveXsltParams(IEnumerable<XPathVariable> xsltParams,
                                                                                   XElement contextElement,
                                                                                   XsltContext xpathContext)
        {
            foreach (var param in xsltParams)
            {
                var contextVariable = param.Evaluate(contextElement, xpathContext);
                object val = contextVariable.Evaluate(xpathContext);
                yield return new KeyValuePair<string, object>(param.Name, val);
            }
        }

        protected virtual ParsedTemplate PrepareTemplate(TemplateData templateData, Stack<IFileProvider> providers = null)
        {
            // set up temp file container
            TempFileCollection tempFiles = new TempFileCollection(templateData.TemporaryFilesPath,
                                                                  templateData.KeepTemporaryFiles);

            if (!Directory.Exists(tempFiles.TempDir))
                Directory.CreateDirectory(tempFiles.TempDir);

            if (providers == null)
                providers = new Stack<IFileProvider>();

            // clone orig doc
            XDocument workingDoc;

            // this is required to preserve the line information 
            using (var xmlReader = this._templateDefinition.CreateReader())
                workingDoc = XDocument.Load(xmlReader, LoadOptions.SetLineInfo);
            
            // template inheritence
            //XAttribute templateInheritsAttr = workingDoc.Root.Attribute("inherits");
            if (this._templateInfo.Inherits != null)
            {
               
                int depth = providers.Count + 1;
                Template inheritedTemplate = _templateInfo.Inherits.Load(this._container);
                ParsedTemplate parsedTemplate = inheritedTemplate.PrepareTemplate(templateData, providers);

                providers.Push(inheritedTemplate.GetScopedFileProvider());

                // a little hacky but it should work with the Reverse()/AddFirst()
                foreach (XElement elem in parsedTemplate.Source.Root.Elements().Reverse())
                {
                    workingDoc.Root.AddFirst(new XElement(elem));
                }

                // create and register temp file (this can be overriden later if there are meta-template directives
                // in the template
                this._templateSourcePath = this.SaveTempFile(tempFiles, workingDoc, "inherited." + depth);
            }

            // add our file provider to the top of the stack
            providers.Push(this.GetScopedFileProvider());

            // start by loading any parameters as they are needed for meta-template evaluation
            CustomXsltContext customContext = CreateCustomXsltContext(templateData.IgnoredVersionComponent);

            XElement[] paramNodes = workingDoc.Root.Elements("parameter").ToArray();
            var globalParams =
                paramNodes.Select(paramNode =>
                                  new ExpressionXPathVariable(this.GetAttributeValue(paramNode, "name"),
                                                              this.GetAttributeValueOrDefault(paramNode, "select")))
                          .ToArray();

            customContext.PushVariableScope(workingDoc, globalParams);

            var arguments = templateData.Arguments
                                        .Select(argument => new ConstantXPathVariable(argument.Key, argument.Value))
                                        .ToArray();

            customContext.PushVariableScope(workingDoc, arguments);

            // expand any meta-template directives
            workingDoc = ApplyMetaTransforms(workingDoc, customContext, providers, tempFiles);

            // there was neither inheretance, nor any meta-template directives
            if (this._templateSourcePath == null)
            {
                // save current template to disk
                this._templateSourcePath = this.SaveTempFile(tempFiles, workingDoc);
            }

            // loading template
            List<Stylesheet> stylesheets = new List<Stylesheet>();
            List<Resource> resources = new List<Resource>();
            List<Index> indices = new List<Index>();
            foreach (XElement elem in workingDoc.Root.Elements())
            {
                // we alread proessed the parameters
                if (elem.Name.LocalName == "parameter")
                    continue;

                if (elem.Name.LocalName == "apply-stylesheet")
                {
                    stylesheets.Add(this.ParseStylesheet(providers, stylesheets, elem));
                }
                else if (elem.Name.LocalName == "index")
                {
                    indices.Add(this.ParseIndexDefinition(elem));
                }
                else if (elem.Name.LocalName == "include-resource")
                {
                    resources.Add(ParseResouceDefinition(providers, elem));
                }
                else
                {
                    throw new Exception("Unknown element: " + elem.Name.LocalName);
                }
            }

            return new ParsedTemplate
                       {
                           Parameters = globalParams,
                           Source = workingDoc,
                           Resources = resources.ToArray(),
                           Stylesheets = stylesheets.ToArray(),
                           Indices = indices.ToArray(),
                           TemporaryFiles = tempFiles,
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

        private Resource ParseResouceDefinition(Stack<IFileProvider> providers, XElement elem)
        {
            var source = this.GetAttributeValue(elem, "path");

            IFileProvider resourceProvider;
            Uri sourceUri;
            if (Uri.TryCreate(source, UriKind.Absolute, out sourceUri) && sourceUri.Scheme.StartsWith("http"))
            {
                resourceProvider = new HttpFileProvider();
            }
            else
            {
                resourceProvider = providers.FirstOrDefault(p => p.FileExists(source));
                if (resourceProvider == null)
                    throw new FileNotFoundException("Resource not found: " + source);
            }
        
            XAttribute outputAttr = elem.Attribute("output");

            string output;
            if (outputAttr != null)
                output = outputAttr.Value;
            else
                output = '\'' + Path.GetFileName(source) + '\'';

            List<ResourceTransform> transforms = new List<ResourceTransform>();
            foreach (XElement transform in elem.Elements("transform"))
            {
                string transformName = this.GetAttributeValue(transform, "name");

                var transformParams = transform.Elements("with-param")
                                               .Select(p => new ExpressionXPathVariable(this.GetAttributeValue(p, "name"),
                                                                                        this.GetAttributeValue(p, "select")));

                transforms.Add(new ResourceTransform(transformName, transformParams));
            }

            var resource = new Resource(this.GetAttributeValueOrDefault(elem, "condition"),
                                        this.ParseVariables(elem).ToArray(),
                                        resourceProvider,
                                        source,
                                        output,
                                        transforms.ToArray());
            return resource;
        }

        protected virtual XDocument ApplyMetaTransforms(XDocument workingDoc, CustomXsltContext customContext, Stack<IFileProvider> providers, TempFileCollection tempFiles)
        {
            // check for meta-template directives and expand
            int metaCount = 0;
            XElement metaNode = workingDoc.Root.Elements("meta-template").FirstOrDefault();
            while (metaNode != null)
            {
                if (EvalCondition(customContext, metaNode, this.GetAttributeValueOrDefault(metaNode, "condition")))
                {
                    XslCompiledTransform metaTransform = this.LoadStylesheet(providers,
                                                                             this.GetAttributeValue(metaNode, "stylesheet"));

                    XsltArgumentList xsltArgList = new XsltArgumentList();

                    // TODO this is a quick fix/hack
                    xsltArgList.AddExtensionObject(Namespaces.TemplateExtensions, new TemplateXsltExtensions(null, null));

                    var metaParamNodes = metaNode.Elements("with-param");

                    foreach (XElement paramNode in metaParamNodes)
                    {
                        string pName = this.GetAttributeValue(paramNode, "name");
                        string pExpr = this.GetAttributeValue(paramNode, "select");

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
                    using (XmlWriter outputWriter = XmlWriter.Create(tempStream, new XmlWriterSettings {Indent = true}))
                    {
                        
                        metaTransform.Transform(workingDoc.CreateNavigator(),
                                                xsltArgList,
                                                outputWriter,
                                                new XmlFileProviderResolver(providers));

                        outputWriter.Close();

                        // rewind stream
                        tempStream.Seek(0, SeekOrigin.Begin);
                        outputDoc = XDocument.Load(tempStream, LoadOptions.SetLineInfo);

                        // create and register temp file
                        // this will override the value set in PrepareTemplate in case of template inhertence
                        // TODO this is a bit hacky, maybe add Template.Name {get;} instead of this._basePath (which could be anything)
                        this._templateSourcePath = this.SaveTempFile(tempFiles, outputDoc,
                                                                     this._basePath
                                                                     + ".meta."
                                                                     + (++metaCount).ToString(CultureInfo.InvariantCulture));
                    }


                    TraceSources.TemplateSource.TraceVerbose("Template after transformation by {0}",
                                                             this.GetAttributeValue(metaNode, "stylesheet"));

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

        protected virtual Index ParseIndexDefinition(XElement elem)
        {
            return new Index(this.GetAttributeValue(elem, "name"),
                             this.GetAttributeValue(elem, "match"),
                             this.GetAttributeValue(elem, "key"));
        }

        private static bool EvalCondition(CustomXsltContext customContext, XNode contextNode, string condition)
        {
            bool shouldApply;
            if (string.IsNullOrWhiteSpace(condition))
                shouldApply = true;
            else
            {
                object value = contextNode.XPathEvaluate(condition, customContext);
                shouldApply = ResultToBool(value);
            }
            return shouldApply;
        }

        private Stylesheet ParseStylesheet(Stack<IFileProvider> providers, IEnumerable<Stylesheet> stylesheets, XElement elem)
        {

            var nameAttr = elem.Attribute("name");
            var src = this.GetAttributeValue(elem, "stylesheet");
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
            XslCompiledTransform transform;

            Stylesheet match = stylesheets.FirstOrDefault(s => String.Equals(s.Source, src, StringComparison.Ordinal));
            if (match != null)
                transform = match.Transform;
            else
                transform = this.LoadStylesheet(providers, src);

            return new Stylesheet
                       {
                           Source = src,
                           Transform = transform,
                           SelectExpression = this.GetAttributeValue(elem, "select"),
                           AssetIdExpression = this.GetAttributeValue(elem, "assetId"),
                           OutputExpression = this.GetAttributeValue(elem, "output"),
                           VersionExpression = this.GetAttributeValue(elem, "version"),
                           XsltParams = this.ParseParams(elem.Elements("with-param")).ToArray(),
                           Variables = this.ParseVariables(elem).ToArray(),
                           Name = name,
                           Sections = this.ParseSectionRegistration(elem.Elements("register-section")).ToArray(),
                           AssetAliases = this.ParseAliasRegistration(elem.Elements("register-alias")).ToArray(),
                           ConditionExpression = GetAttributeValueOrDefault(elem, "condition")
                       };
        }

        private string GetAttributeValueOrDefault(XElement elem, string attName, string defaultValue = null)
        {
            var attr = elem.Attribute(attName);

            if (attr == null)
                return defaultValue;

            return attr.Value;
        }

        private string GetAttributeValue(XElement elem, string attName)
        {
            var attr = elem.Attribute(attName);

            if (attr == null)
                throw TemplateException.MissingAttribute(this._templateSourcePath, elem, attName);

            return attr.Value;
        }

        /// <summary>
        /// Applies the loaded templates to <paramref name="templateData"/>.
        /// </summary>
        /// <param name="templateData">
        /// Instance of <see cref="TemplateData"/> containing the various input data needed. 
        /// </param>
        public virtual TemplateOutput Generate(TemplateData templateData)
        {
            Stopwatch timer = Stopwatch.StartNew();

            ParsedTemplate tmpl = this.PrepareTemplate(templateData);

            // collect all work that has to be done
            List<UnitOfWork> work = new List<UnitOfWork>();

            // resource work units
            work.AddRange(this.DiscoverWork(templateData, tmpl.Parameters, tmpl.Resources));

            // stylesheet work units
            {
                List<StylesheetApplication> stylesheetApplications = new List<StylesheetApplication>();
                foreach (Stylesheet stylesheet in tmpl.Stylesheets)
                {
                    stylesheetApplications.AddRange(this.DiscoverWork(templateData, tmpl.Parameters, stylesheet));
                }

                var duplicates =
                    stylesheetApplications.GroupBy(sa => sa.SaveAs, StringComparer.OrdinalIgnoreCase)
                                          .Where(g => g.Count() > 1);

                foreach (var group in duplicates)
                {
                    TraceSources.TemplateSource.TraceError("Duplicate work unit target ({0}) generated from: {1}",
                                                           group.Key,
                                                           string.Join(", ",
                                                                       group.Select(sa => '\'' + sa.StylesheetName + '\'')));

                    foreach (var workunit in group.Skip(1))
                    {
                        stylesheetApplications.Remove(workunit);
                    }
                }

                work.AddRange(stylesheetApplications);
            }

            TraceSources.TemplateSource.TraceInformation("Generating {0:N0} documents from {1:N0} stylesheets.",
                                                         work.Count, tmpl.Stylesheets.Length);

            ConcurrentBag<WorkUnitResult> results = new ConcurrentBag<WorkUnitResult>();

            // create context
            ITemplatingContext context = new TemplatingContext(this._cache,
                                                               this._container,
                                                               templateData.OutputFileProvider, // TODO fix this (this._basePath)
                                                               templateData,
                                                               this._resolvers,
                                                               this._templateInfo.Source);


            // fill indices
            using (TraceSources.TemplateSource.TraceActivity("Indexing input document"))
            {
                var customXsltContext = CreateCustomXsltContext(templateData.IgnoredVersionComponent);
                foreach (var index in tmpl.Indices)
                {
                    TraceSources.TemplateSource.TraceVerbose("Adding index {0} (match: '{1}', key: '{1}')",
                                                             index.Name,
                                                             index.MatchExpr,
                                                             index.KeyExpr);
                    context.DocumentIndex.AddKey(index.Name, index.MatchExpr, index.KeyExpr, customXsltContext);
                }

                TraceSources.TemplateSource.TraceInformation("Indexing...");
                context.DocumentIndex.BuildIndexes();
            }


            int totalCount = work.Count;
            long lastProgress = Stopwatch.GetTimestamp();
            int processed = 0;
            // process all units of work
            ParallelOptions parallelOptions = new ParallelOptions
                                                  {
                                                      //MaxDegreeOfParallelism = 1
                                                  };


            IEnumerable<UnitOfWork> unitsOfWork = work;
            if (templateData.Filter != null)
            {
                unitsOfWork = unitsOfWork
                    .Where(uow =>
                               {
                                   if (templateData.Filter(uow))
                                       return true;

                                   TraceSources.TemplateSource.TraceInformation("Filtered unit of work: [{0}] {1}",
                                                                                uow.GetType().Name,
                                                                                uow.ToString());
                                   return false;
                               });
            }


            Parallel.ForEach(unitsOfWork,
                             parallelOptions,
                             uow =>
                             {
                                 results.Add(uow.Execute(context));
                                 int c = Interlocked.Increment(ref processed);
                                 long lp = Interlocked.Read(ref lastProgress);
                                 if ((Stopwatch.GetTimestamp() - lp) / (double)Stopwatch.Frequency > 5.0)
                                 {
                                     if (Interlocked.CompareExchange(ref lastProgress,
                                                                     Stopwatch.GetTimestamp(),
                                                                     lp) == lp)
                                     {
                                         TraceSources.TemplateSource.TraceInformation(
                                             "Progress: {0:P1} ({1:N0}/{2:N0})",
                                             c / (double)totalCount,
                                             c,
                                             totalCount);
                                     }
                                 }
                             });

            // stop timing
            timer.Stop();


            Stopwatch statsTimer = new Stopwatch();
            // prepare stats
            Dictionary<Type, WorkUnitResult[]> resultGroups =
                results.GroupBy(ps => ps.WorkUnit.GetType()).ToDictionary(g => g.Key, g => g.ToArray());



            var stylesheetStats =
                resultGroups[typeof(StylesheetApplication)]
                .GroupBy(r => ((StylesheetApplication)r.WorkUnit).StylesheetName);

            foreach (var statGroup in stylesheetStats)
            {
                long min = statGroup.Min(ps => ps.Duration);
                long max = statGroup.Max(ps => ps.Duration);
                TraceSources.TemplateSource.TraceInformation("Applied stylesheet '{0}' {1:N0} times in {2:N0} ms (min: {3:N0}, mean {4:N0}, max {5:N0}, avg: {6:N0})",
                                                             statGroup.Key,
                                                             statGroup.Count(),
                                                             statGroup.Sum(ps => ps.Duration) / 1000.0,
                                                             min / 1000.0,
                                                             statGroup.Skip(statGroup.Count() / 2).Take(1).Single().Duration / 1000.0,
                                                             max / 1000.0,
                                                             statGroup.Average(ps => ps.Duration) / 1000.0);


                // TODO this is quick and dirty, should be cleaned up 
                long[] buckets = new long[20];
                int rows = 6;
                /* 
┌────────────────────┐ ◄ 230
│█                  █│
│█                  █│
│█                  █│
│█                  █│
│█                  █│
│█__________________█│
└────────────────────┘ ◄ 0
▲ 12ms               ▲ 12ms
                 */
                // this is a little hacky, but it will do for now
                WorkUnitResult[] sortedResults = statGroup.OrderBy(r => r.Duration).ToArray();
                double bucketSize = (max - min) / (double)buckets.Length;
                int bucketNum = 0;
                long bucketMax = 0;
                foreach (WorkUnitResult result in sortedResults)
                {
                    while ((result.Duration - min) > (bucketNum + 1) * bucketSize)
                        bucketNum++;

                    buckets[bucketNum] += 1;
                    bucketMax = Math.Max(buckets[bucketNum], bucketMax);
                }


                double rowHeight = bucketMax / (double)rows;

                StringBuilder graph = new StringBuilder();
                graph.AppendLine("Graph:");
                const int gutter = 2;
                int columnWidth = graph.Length;
                graph.Append('┌').Append('─', buckets.Length).Append('┐').Append('◄').Append(' ').Append(bucketMax.ToString("N0"));
                int firstLineLength = graph.Length - columnWidth;
                columnWidth = graph.Length - columnWidth + gutter;
                StringBuilder lastLine = new StringBuilder();
                lastLine.Append('▲').Append(' ').Append((min / 1000.0).ToString("N0")).Append("ms");
                lastLine.Append(' ', (buckets.Length + 2) - lastLine.Length - 1);
                lastLine.Append('▲').Append(' ').Append((max / 1000.0).ToString("N0")).Append("ms");
                columnWidth = Math.Max(columnWidth, lastLine.Length + gutter);

                if (columnWidth > firstLineLength)
                    graph.Append(' ', columnWidth - firstLineLength);
                graph.AppendLine("Percentage of the applications processed within a certain time (ms)");

                for (int row = 0; row < rows; row++)
                {
                    // │┌┐└┘─
                    graph.Append('│');
                    for (int col = 0; col < buckets.Length; col++)
                    {
                        if (buckets[col] > (rowHeight * (rows - (row + 1)) + rowHeight / 2.0))
                            graph.Append('█');
                        else if (buckets[col] > rowHeight * (rows - (row + 1)))
                            graph.Append('▄');
                        else if (row == rows - 1)
                            graph.Append('_');
                        else
                            graph.Append(' ');
                    }
                    graph.Append('│');

                    graph.Append(' ', columnWidth - (buckets.Length + 2));
                    switch (row)
                    {
                        case 0:
                            graph.Append(" 100% ").Append((max / 1000.0).ToString("N0"));
                            break;
                        case 1:
                            graph.Append("  95% ").Append((sortedResults[((int)Math.Floor(sortedResults.Length * 0.95))].Duration / 1000.0).ToString("N0"));
                            break;
                        case 2:
                            graph.Append("  90% ").Append((sortedResults[((int)Math.Floor(sortedResults.Length * .9))].Duration / 1000.0).ToString("N0"));
                            break;
                        case 3:
                            graph.Append("  80% ").Append((sortedResults[((int)Math.Floor(sortedResults.Length * 0.8))].Duration / 1000.0).ToString("N0"));
                            break;
                        case 4:
                            graph.Append("  70% ").Append((sortedResults[((int)Math.Floor(sortedResults.Length * 0.7))].Duration / 1000.0).ToString("N0"));
                            break;
                        case 5:
                            graph.Append("  50% ").Append((sortedResults[((int)Math.Floor(sortedResults.Length * 0.5))].Duration / 1000.0).ToString("N0"));
                            break;
                    }

                    graph.AppendLine();
                }
                int len = graph.Length;
                graph.Append('└').Append('─', buckets.Length).Append('┘').Append('◄').Append(" 0");
                len = graph.Length - len;
                if (columnWidth > len)
                    graph.Append(' ', columnWidth - len);

                graph.Append("  10% ").Append((sortedResults[((int)Math.Floor(sortedResults.Length * .1))].Duration / 1000.0).ToString("N0"));

                graph.AppendLine();

                lastLine.Append(' ', columnWidth - lastLine.Length);
                lastLine.Append("   1% ").Append((sortedResults[((int)Math.Floor(sortedResults.Length * .01))].Duration / 1000.0).ToString("N0"));
                graph.Append(lastLine.ToString());

                TraceSources.TemplateSource.TraceVerbose(graph.ToString());

            }

            var resourceStats = resultGroups[typeof(ResourceDeployment)];

            foreach (var statGroup in resourceStats)
            {
                TraceSources.TemplateSource.TraceInformation("Deployed resource '{0}' in {1:N0} ms",
                                                             ((ResourceDeployment)statGroup.WorkUnit).ResourcePath,
                                                             statGroup.Duration);
            }


            TraceSources.TemplateSource.TraceInformation("Documentation generated in {0:N1} seconds (processing time: {1:N1} seconds)",
                                                         timer.Elapsed.TotalSeconds,
                                                         results.Sum(ps => ps.Duration) / 1000000.0);

            TraceSources.TemplateSource.TraceInformation("Statistics generated in {0:N1} seconds",
                                                         statsTimer.Elapsed.TotalSeconds);

            return new TemplateOutput(results.ToArray(), tmpl.TemporaryFiles);
        }

        private IEnumerable<UnitOfWork> DiscoverWork(TemplateData templateData, XPathVariable[] parameters, Resource[] resources)
        {
            CustomXsltContext xpathContext = CreateCustomXsltContext(templateData.IgnoredVersionComponent);
            xpathContext.PushVariableScope(templateData.Document.Root, parameters);
            for (int i = 0; i < resources.Length; i++)
            {
                xpathContext.PushVariableScope(templateData.Document.Root, resources[i].Variables);

                if (EvalCondition(xpathContext, templateData.Document.Root, resources[i].ConditionExpression))
                {
                    List<IResourceTransform> transforms = new List<IResourceTransform>();

                    foreach (var resourceTransform in resources[i].Transforms)
                    {
                        CompositionContainer paramContainer = new CompositionContainer(this._container);

                        // TODO export resourceTransform.Parameters into paramContainer
                        ImportDefinition importDefinition =
                            new MetadataContractBasedImportDefinition(
                                typeof(IResourceTransform),
                                null,
                                new[] {new Tuple<string, object, IEqualityComparer>("Name", resourceTransform.Name, StringComparer.OrdinalIgnoreCase) },
                                ImportCardinality.ExactlyOne,
                                false,
                                false,
                                CreationPolicy.NonShared);

                        Export transformExport = paramContainer.GetExports(importDefinition).Single();

                        transforms.Add((IResourceTransform)transformExport.Value);
                    }

                    yield return
                        new ResourceDeployment(resources[i].FileProvider,
                                               resources[i].Source,
                                               resources[i].Output, // TODO this needs a 'writable' file provider
                                               transforms.ToArray());
                }
                xpathContext.PopVariableScope();
            }
            xpathContext.PopVariableScope();

        }

        private static CustomXsltContext CreateCustomXsltContext(VersionComponent? ignoredVersionComponent)
        {
            CustomXsltContext xpathContext = new CustomXsltContext();
            xpathContext.RegisterFunction(string.Empty, "get-id", new XsltContextAssetIdGetter());
            xpathContext.RegisterFunction(string.Empty, "get-version", new XsltContextAssetVersionGetter());
            xpathContext.RegisterFunction(string.Empty, "substring-before-last", new XsltContextSubstringBeforeLastFunction());
            xpathContext.RegisterFunction(string.Empty, "substring-after-last", new XsltContextSubstringAfterLastFunction());
            xpathContext.RegisterFunction(string.Empty, "iif", new XsltContextTernaryOperator());
            xpathContext.RegisterFunction(string.Empty, "get-significant-version", new XsltContextAssetVersionGetter(ignoredVersionComponent));
            xpathContext.RegisterFunction(string.Empty, "coalesce", new XsltContextCoalesceFunction());
            xpathContext.RegisterFunction(string.Empty, "join", new XsltContextJoinFunction());
            return xpathContext;
        }

        private static string ResultToString(object res)
        {
            string ret = res as string;
            if (ret == null)
            {
                if (res is IEnumerable)
                {
                    object first = ((IEnumerable)res).Cast<object>().FirstOrDefault();
                    ret = first as string;
                    if (ret == null)
                    {
                        if (first is XAttribute)
                            ret = ((XAttribute)first).Value;
                        else if (first is XCData)
                            ret = ((XCData)first).Value;
                        else if (first is XText)
                            ret = ((XText)first).Value;
                        else if (first is XElement)
                            ret = ((XElement)first).Value;
                    }
                }
            }

            return ret;
        }


        /*
         * a number is true if and only if it is neither positive or negative zero nor NaN
         * a node-set is true if and only if it is non-empty
         * a string is true if and only if its length is non-zero
         * an object of a type other than the four basic types is converted to a boolean in a way that is dependent on that type
         */
        protected internal static bool ResultToBool(object res)
        {
            bool ret;
            if (res == null)
                ret = false;
            else if (res is string)
                ret = ((string)res).Length > 0;
            else if (res is bool)
                ret = (bool)res;
            else if (res is double)
            {
                double d = (double)res;
                ret = (d > 0.0 || d < 0.0) && !double.IsNaN(d);
            }
            else if (res is IEnumerable)
            {
                object first = ((IEnumerable)res).Cast<object>().FirstOrDefault();
                ret = ResultToBool(first);
            }
            else
                ret = false;


            return ret;
        }
    }
}
