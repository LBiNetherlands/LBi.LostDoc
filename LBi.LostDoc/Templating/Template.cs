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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using LBi.LostDoc.Composition;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Extensibility;
using LBi.LostDoc.Templating.AssetResolvers;
using LBi.LostDoc.Templating.FileProviders;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{
    // TODO figure out how to untangle this mess of a class, extract parsing to TemplateParser
    // TODO fix error handling, a bad template.xml file will just throw random exceptions, xml schema validation?
    public class Template
    {
        public const string TemplateDefinitionFileName = "template.xml";

        private readonly ObjectCache _cache;
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

        /// <summary>
        /// The load stylesheet.
        /// </summary>
        /// <param name="fileProvider"><see cref="IFileProvider"/> used to load the stylesheet.</param>
        /// <param name="name">
        /// </param>
        /// <returns>
        /// </returns>
        private XslCompiledTransform LoadStylesheet(IFileProvider fileProvider, string name)
        {
            XslCompiledTransform ret = new XslCompiledTransform(true);

            using (Stream str = fileProvider.OpenFile(name, FileMode.Open))
            {
                XmlReader reader = XmlReader.Create(str, new XmlReaderSettings { CloseInput = true, });
                XsltSettings settings = new XsltSettings(true, true);
                XmlResolver resolver = new XmlFileProviderResolver(fileProvider, null);
                ret.Load(reader, settings, resolver);
            }

            return ret;
        }

        #endregion





        protected virtual ParsedTemplate PrepareTemplate(TemplateData templateData, Stack<IFileProvider> providers = null)
        {
            // set up temp file container
            TempFileCollection tempFiles = new TempFileCollection(templateData.TemporaryFilesPath,
                                                                  templateData.KeepTemporaryFiles);

            if (!Directory.Exists(tempFiles.TempDir))
                Directory.CreateDirectory(tempFiles.TempDir);

            if (providers == null)
            {
                providers = new Stack<IFileProvider>();
                providers.Push(new HttpFileProvider());
                providers.Push(new DirectoryFileProvider());
            }

            // clone orig doc
            XDocument workingDoc;

            // this is required to preserve the line information 
            using (var xmlReader = this._templateDefinition.CreateReader())
                workingDoc = XDocument.Load(xmlReader, LoadOptions.SetLineInfo);

            // template inheritence
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
                this._templateSourcePath = this.SaveTempFile(tempFiles, workingDoc, "inherited." + depth + '.' + _templateInfo.Name);
            }

            // add our file provider to the top of the stack
            providers.Push(this.GetScopedFileProvider());

            // create stacked provider
            IFileProvider provider = new StackedFileProvider(providers);

            // start by loading any parameters as they are needed for meta-template evaluation
            CustomXsltContext customContext = CreateCustomXsltContext(templateData.IgnoredVersionComponent);

            XElement[] paramNodes = workingDoc.Root.Elements("parameter").ToArray();
            List<XPathVariable> globalParams = new List<XPathVariable>();

            foreach (XElement paramNode in paramNodes)
            {
                string name = paramNode.GetAttributeValue("name");
                object argValue;
                if (templateData.Arguments.TryGetValue(name, out argValue))
                    globalParams.Add(new ConstantXPathVariable(name, argValue));
                else
                {
                    string expr = paramNode.GetAttributeValueOrDefault("select");
                    globalParams.Add(new ExpressionXPathVariable(name, expr));
                }
            }

            customContext.PushVariableScope(workingDoc, globalParams.ToArray());

            var arguments = templateData.Arguments
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
                    stylesheets.Add(this.ParseStylesheet(provider, stylesheets, elem));
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

            return new ParsedTemplate
                       {
                           Parameters = globalParams.ToArray(),
                           Source = workingDoc,
                           ResourceDirectives = resources.ToArray(),
                           StylesheetsDirectives = stylesheets.ToArray(),
                           IndexDirectives = indices.ToArray(),
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
                    XslCompiledTransform metaTransform = this.LoadStylesheet(provider,
                                                                             metaNode.GetAttributeValue("stylesheet"));

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



        private StylesheetDirective ParseStylesheet(IFileProvider provider, IEnumerable<StylesheetDirective> stylesheets, XElement elem)
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
            XslCompiledTransform transform;

            /* TODO see if we can't defer this till after ParepareTemplate as the current situation means we
             * load dupliacte stylesheets inherited templates, also prevents applying meta-templates to
             * the stylesheets themselves as they are then already loaded
             */
            StylesheetDirective match = stylesheets.FirstOrDefault(s => String.Equals(s.Source, src, StringComparison.Ordinal));
            if (match != null)
                transform = match.Transform;
            else
                transform = this.LoadStylesheet(provider, src);

            var ret = new StylesheetDirective
                       {
                           Source = src,
                           Transform = transform,
                           SelectExpression = elem.GetAttributeValueOrDefault("select", "/"),
                           InputExpression = elem.GetAttributeValueOrDefault("input"),
                           OutputExpression = elem.GetAttributeValueOrDefault("output"),
                           XsltParams = this.ParseParams(elem.Elements("with-param")).ToArray(),
                           Variables = this.ParseVariables(elem).ToArray(),
                           Name = name,
                           Sections = this.ParseSectionRegistration(elem.Elements("register-section")).ToArray(),
                           AssetRegistrations = this.ParseAssetRegistration(elem.Elements("register-asset")).ToArray(),
                           ConditionExpression = elem.GetAttributeValueOrDefault("condition")
                       };

            return ret;
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

            this._uriFactory.Clear();
            this._fileResolver.Clear();

            ParsedTemplate tmpl = this.PrepareTemplate(templateData);

            // collect all work that has to be done
            ITemplateContext templateContext = new TemplateContext(templateData.Document,
                                                                   CreateCustomXsltContext(templateData.IgnoredVersionComponent),
                                                                   this._uriFactory,
                                                                   this._fileResolver,
                                                                   this._container.Catalog);

            UnitOfWork[] work = tmpl.DiscoverWork(templateContext).ToArray();

            TraceSources.TemplateSource.TraceInformation("Generating {0:N0} documents from {1:N0} stylesheets.",
                                                         work.Length, tmpl.StylesheetsDirectives.Length);

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
                foreach (var index in tmpl.IndexDirectives)
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

            //List<Task<WorkUnitResult>> tasks = new List<Task<WorkUnitResult>>();

            //foreach (UnitOfWork unitOfWork in work)
            //{
            //    Task<WorkUnitResult> task = new Task<WorkUnitResult>(uow => ((UnitOfWork) uow).Execute(context), unitOfWork);
            //    tasks.Add(task);
            //    this._dependencyProvider.Add(uow.);
            //}
            //    work.Select(uow => new Task<WorkUnitResult>(() => uow.Execute(context)));

            //this._dependencyProvider.Add(); 

            int totalCount = work.Length;
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
                                 results.Add(uow.CreateTask(context));
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

        private static CustomXsltContext CreateCustomXsltContext(VersionComponent? ignoredVersionComponent)
        {
            CustomXsltContext xpathContext = new CustomXsltContext();
            xpathContext.RegisterFunction(string.Empty, "get-asset", new XsltContextAssetIdGetter());
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
    }
}
