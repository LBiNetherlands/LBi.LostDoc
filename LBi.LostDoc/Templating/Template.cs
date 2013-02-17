/*
 * Copyright 2012,2013 LBi Netherlands B.V.
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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
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
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Templating.AssetResolvers;
using LBi.LostDoc.Templating.FileProviders;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{


    public class Template
    {
        public const string TemplateDefinitionFileName = "template.xml";

        private readonly ObjectCache _cache;
        private readonly CompositionContainer _container;
        private readonly FileResolver _fileResolver;
        private readonly List<IAssetUriResolver> _resolvers;

        private string _basePath;
        private XDocument _templateDefinition;
        private IReadOnlyFileProvider _fileProvider;
        private TemplateResolver _templateResolver;

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

        public virtual void Load(TemplateResolver resolver, string name)
        {
            string path;
            if (resolver.Resolve(name, out this._fileProvider, out path))
            {
                using (Stream str = this._fileProvider.OpenFile(path))
                    _templateDefinition = XDocument.Load(str);

                this._basePath = Path.GetDirectoryName(path);
                this._templateResolver = resolver;
            }
            else
            {
                throw new FileNotFoundException("Template not found, search paths: {0}", resolver.ToString());
            }
        }

        protected virtual IReadOnlyFileProvider GetScopedFileProvider()
        {
            return new ScopedFileProvider(this._fileProvider, this._basePath);
        }


        private IEnumerable<AliasRegistration> ParseAliasRegistration(IEnumerable<XElement> elements)
        {
            foreach (XElement elem in elements)
            {
                yield return
                    new AliasRegistration
                        {
                            Variables = this.ParseVariables(elem).ToArray(),
                            SelectExpression = elem.Attribute("select") == null ? "." : elem.Attribute("select").Value,
                            AssetIdExpression = elem.Attribute("assetId").Value,
                            VersionExpression = elem.Attribute("version").Value,
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
                            SelectExpression = elem.Attribute("select").Value,
                            NameExpression = elem.Attribute("name").Value,
                            AssetIdExpression = elem.Attribute("assetId").Value,
                            VersionExpression = elem.Attribute("version").Value,
                            ConditionExpression = GetAttributeValueOrDefault(elem, "condition"),
                            Variables = this.ParseVariables(elem).ToArray(),
                        };
            }
        }

        private IEnumerable<XPathVariable> ParseVariables(XElement element)
        {
            return
                element.Attributes()
                       .Where(a => a.Name.NamespaceName == "urn:lost-doc:template.variable")
                       .Select(a => new ExpressionXPathVariable(a.Name.LocalName, a.Value));
        }

        private IEnumerable<XPathVariable> ParseParams(IEnumerable<XElement> elements)
        {
            foreach (XElement elem in elements)
            {
                if (elem.Name.LocalName == "with-param")
                {
                    yield return new ExpressionXPathVariable(elem.Attribute("name").Value,
                                                             elem.Attribute("select").Value);
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
        private XslCompiledTransform LoadStylesheet(IReadOnlyFileProvider resourceProvider, string name)
        {
            XslCompiledTransform ret = new XslCompiledTransform(true);
            using (Stream str = resourceProvider.OpenFile(name))
            {
                XmlReader reader = XmlReader.Create(str, new XmlReaderSettings { CloseInput = true, });
                XsltSettings settings = new XsltSettings(false, true);
                XmlResolver resolver = new XmlFileProviderResolver(resourceProvider);
                ret.Load(reader, settings, resolver);
            }

            return ret;
        }

        #endregion

        protected virtual IEnumerable<StylesheetApplication> DiscoverWork(TemplateData templateData, XPathVariable[] parameters, Stylesheet stylesheet)
        {
            TraceSources.TemplateSource.TraceInformation("Processing stylesheet instructions: {0}",
                                                         (object)stylesheet.Name);

            CustomXsltContext xpathContext = CreateCustomXsltContext(templateData.IgnoredVersionComponent);

            xpathContext.PushVariableScope(templateData.XDocument.Root, parameters);

            XElement[] inputElements =
                templateData.XDocument.XPathSelectElements(stylesheet.SelectExpression, xpathContext).ToArray();

            // TODO this code requires a proper cleanup to make the scoping/resolution rules more obvious
            foreach (XElement inputElement in inputElements)
            {
                xpathContext.PushVariableScope(inputElement, stylesheet.Variables);

                string saveAs = ResultToString(inputElement.XPathEvaluate(stylesheet.OutputExpression, xpathContext));
                string version = ResultToString(inputElement.XPathEvaluate(stylesheet.VersionExpression, xpathContext));
                string assetId = ResultToString(inputElement.XPathEvaluate(stylesheet.AssetIdExpression, xpathContext));
                List<AssetIdentifier> aliases = new List<AssetIdentifier>();
                List<AssetSection> sections = new List<AssetSection>();

                // eval condition, shortcut and log instead of wrapping entire loop in if
                if (!EvalCondition(xpathContext, inputElement, stylesheet.ConditionExpression))
                {
                    TraceSources.TemplateSource.TraceVerbose("{0}, {1} => Condition not met", assetId, version);
                    xpathContext.PopVariableScope();
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
                        xpathContext.PushVariableScope(aliasInputElement, alias.Variables);

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
                        xpathContext.PopVariableScope();
                    }
                }

                // sections
                foreach (SectionRegistration section in stylesheet.Sections)
                {
                    XElement[] sectionInputElements = inputElement.XPathSelectElements(section.SelectExpression, xpathContext).ToArray();

                    foreach (XElement sectionInputElement in sectionInputElements)
                    {
                        xpathContext.PushVariableScope(sectionInputElement, section.Variables);

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

                        xpathContext.PopVariableScope();
                    }
                }

                var xsltParams = ResolveXsltParams(stylesheet.XsltParams, inputElement, xpathContext).ToArray();

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

            xpathContext.PopVariableScope();
        }

        private static IEnumerable<KeyValuePair<string, object>> ResolveXsltParams(IEnumerable<XPathVariable> xsltParams, XElement contextElement, XsltContext xpathContext)
        {
            foreach (var param in xsltParams)
            {
                var contextVariable = param.Evaluate(contextElement, xpathContext);
                object val = contextVariable.Evaluate(xpathContext);
                yield return new KeyValuePair<string, object>(param.Name, val);
            }
        }

        protected virtual ParsedTemplate PrepareTemplate(TemplateData templateData, Dictionary<string, IReadOnlyFileProvider> providers = null)
        {
            // clone orig doc
            XDocument workingDoc = new XDocument(this._templateDefinition);

            // template inheritence
            XAttribute templateInheritsAttr = workingDoc.Root.Attribute("inherits");
            if (templateInheritsAttr != null)
            {
                if (providers == null)
                    providers = new Dictionary<string, IReadOnlyFileProvider>();

                int depth = providers.Count + 1;
                Template inheritedTemplate = new Template(this._container);
                inheritedTemplate.Load(this._templateResolver, templateInheritsAttr.Value);
                ParsedTemplate parsedTemplate = inheritedTemplate.PrepareTemplate(templateData, providers);

                providers.Add(depth.ToString(System.Globalization.CultureInfo.InvariantCulture),
                              inheritedTemplate.GetScopedFileProvider());

                // a little hacky but it should work with the Reverse()/AddFirst()
                foreach (XElement elem in parsedTemplate.Source.Root.Elements().Reverse())
                {
                    var clone = new XElement(elem);
                    clone.Add(new XAttribute("source", depth));
                    workingDoc.Root.AddFirst(clone);
                }
            }

            // start by loading any parameters as they are needed for meta-template evaluation
            CustomXsltContext customContext = CreateCustomXsltContext(templateData.IgnoredVersionComponent);

            XElement[] paramNodes = workingDoc.Root.Elements("parameter").ToArray();
            var globalParams =
                paramNodes.Select(paramNode =>
                                  new ExpressionXPathVariable(paramNode.Attribute("name").Value,
                                                              this.GetAttributeValueOrDefault(paramNode, "select")))
                          .ToArray();

            customContext.PushVariableScope(workingDoc, globalParams);

            var arguments = templateData.Arguments
                                        .Select(argument => new ConstantXPathVariable(argument.Key, argument.Value))
                                        .ToArray();

            customContext.PushVariableScope(workingDoc, arguments);

            // we're going to need this later
            XmlFileProviderResolver fileResolver = new XmlFileProviderResolver(this._fileProvider, this._basePath);

            // expand any meta-template directives
            workingDoc = ApplyMetaTransforms(workingDoc, customContext, fileResolver);

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
                           Indices = indices.ToArray()
                       };
        }

        private Resource ParseResouceDefinition(Dictionary<string, IReadOnlyFileProvider> providers, XElement elem)
        {
            XAttribute depthAttr = elem.Attribute("depth");

            var source = elem.Attribute("path").Value;

            IReadOnlyFileProvider resourceProvider;
            Uri sourceUri;
            if (Uri.TryCreate(source, UriKind.Absolute, out sourceUri))
            {
                resourceProvider = new HttpFileProvider();
            }
            else
            {
                if (depthAttr == null)
                {
                    resourceProvider = this.GetScopedFileProvider();
                }
                else if (providers == null || !providers.TryGetValue(depthAttr.Value, out resourceProvider))
                {
                    throw new InvalidOperationException(
                        "Depth specified on 'include-resource' but the corresponding provider was not found.");
                }
            }

            XAttribute outputAttr = elem.Attribute("output");

            string output;
            if (outputAttr != null)
                output = outputAttr.Value;
            else
                output = '\'' + Path.GetFileName(source) + '\'';

            foreach (XElement transform in elem.Elements("transform"))
            {

            }

            var resource = new Resource(this.GetAttributeValueOrDefault(elem, "condition"),
                                        this.ParseVariables(elem).ToArray(),
                                        resourceProvider,
                                        source,
                                        output);
            return resource;
        }

        protected virtual XDocument ApplyMetaTransforms(XDocument workingDoc, CustomXsltContext customContext, XmlFileProviderResolver fileResolver)
        {
            // check for meta-template directives and expand
            XElement metaNode = workingDoc.Root.Elements("meta-template").FirstOrDefault();
            while (metaNode != null)
            {
                if (EvalCondition(customContext, metaNode, this.GetAttributeValueOrDefault(metaNode, "condition")))
                {
                    XslCompiledTransform metaTransform = this.LoadStylesheet(this.GetScopedFileProvider(),
                                                                             metaNode.Attribute("stylesheet").Value);

                    XsltArgumentList xsltArgList = new XsltArgumentList();

                    // TODO this is a quick fix/hack
                    xsltArgList.AddExtensionObject("urn:lostdoc-core", new TemplateXsltExtensions(null, null));

                    var metaParamNodes = metaNode.Elements("with-param");

                    foreach (XElement paramNode in metaParamNodes)
                    {
                        string pName = paramNode.Attribute("name").Value;
                        string pExpr = paramNode.Attribute("select").Value;

                        try
                        {
                            xsltArgList.AddParam(pName,
                                                 string.Empty,
                                                 workingDoc.XPathEvaluate(pExpr, customContext));
                        }
                        catch (XPathException ex)
                        {
                            throw new Exception(
                                string.Format("Unable to process 'with-param' element in with name '{0}': {1}",
                                              pName,
                                              ex.Message),
                                ex);
                        }
                    }

                    XDocument outputDoc = new XDocument();
                    using (XmlWriter outputWriter = outputDoc.CreateWriter())
                    {
                        metaTransform.Transform(workingDoc.CreateNavigator(),
                                                xsltArgList,
                                                outputWriter,
                                                fileResolver);
                    }


                    TraceSources.TemplateSource.TraceVerbose("Template after transformation by {0}",
                                                             metaNode.Attribute("stylesheet").Value);

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
            return new Index(elem.Attribute("name").Value,
                             elem.Attribute("match").Value,
                             elem.Attribute("key").Value);
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

        private Stylesheet ParseStylesheet(Dictionary<string, IReadOnlyFileProvider> providers, IEnumerable<Stylesheet> stylesheets, XElement elem)
        {
            XAttribute depthAttr = elem.Attribute("depth");

            IReadOnlyFileProvider resourceProvider;
            if (depthAttr == null)
                resourceProvider = this.GetScopedFileProvider();
            else if (providers == null || !providers.TryGetValue(depthAttr.Value, out resourceProvider))
            {
                throw new InvalidOperationException(
                    "Depth specified on 'apply-stylesheet' but the corresponding provider was not found.");
            }


            string name = elem.Attribute("name") == null
                              ? Path.GetFileNameWithoutExtension(elem.Attribute("stylesheet").Value)
                              : elem.Attribute("name").Value;
            if (elem.Attribute("name") != null)
                TraceSources.TemplateSource.TraceInformation("Loading stylesheet: {0} ({1})", name, elem.Attribute("stylesheet").Value);
            else
                TraceSources.TemplateSource.TraceInformation("Loading stylesheet: {0}", name);

            string src = elem.Attribute("stylesheet").Value;
            XslCompiledTransform transform;

            Stylesheet match = stylesheets.FirstOrDefault(s => String.Equals(s.Source, src, StringComparison.Ordinal));
            if (match != null)
                transform = match.Transform;
            else
                transform = this.LoadStylesheet(resourceProvider, src);


            return new Stylesheet
                       {
                           Source = src,
                           Transform = transform,
                           SelectExpression = elem.Attribute("select").Value,
                           AssetIdExpression = elem.Attribute("assetId").Value,
                           OutputExpression = elem.Attribute("output").Value,
                           VersionExpression = elem.Attribute("version").Value,
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
                                                               this._basePath,
                                                               templateData,
                                                               this._resolvers,
                                                               this._fileProvider);


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

                TraceSources.TemplateSource.TraceInformation(graph.ToString());

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

            return new TemplateOutput(results.ToArray());
        }

        private IEnumerable<UnitOfWork> DiscoverWork(TemplateData templateData, XPathVariable[] parameters, Resource[] resources)
        {
            CustomXsltContext xpathContext = CreateCustomXsltContext(templateData.IgnoredVersionComponent);
            xpathContext.PushVariableScope(templateData.XDocument.Root, parameters);
            for (int i = 0; i < resources.Length; i++)
            {
                xpathContext.PushVariableScope(templateData.XDocument.Root, resources[i].Variables);

                if (EvalCondition(xpathContext, templateData.XDocument.Root, resources[i].ConditionExpression))
                    yield return new ResourceDeployment(resources[i].FileProvider, resources[i].Source);

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
