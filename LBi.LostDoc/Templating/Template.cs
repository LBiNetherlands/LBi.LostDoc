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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Templating.IO;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{
    public class Template
    {
        public Template(FileReference templateSource,
                        IFileProvider templateFileProvider,
                        IEnumerable<TemplateParameterInfo> parameters,
                        IEnumerable<ResourceDirective> resourceDirectives,
                        IEnumerable<StylesheetDirective> stylesheetsDirectives,
                        IEnumerable<IndexDirective> indexDirectives)
        {
            this.Source = templateSource;
            this.TemplateFileProvider = templateFileProvider;
            this.Parameters = parameters.ToArray();
            this.ResourceDirectives = resourceDirectives.ToArray();
            this.StylesheetsDirectives = stylesheetsDirectives.ToArray();
            this.IndexDirectives = indexDirectives.ToArray();
        }

        public FileReference Source { get; private set; }

        public IFileProvider TemplateFileProvider { get; set; }

        /// <summary>
        /// Contains all StylesheetDirective definitions specified in the template.
        /// </summary>
        public StylesheetDirective[] StylesheetsDirectives { get; set; }

        /// <summary>
        /// Contains all resource definitions specified in the template.
        /// </summary>
        public ResourceDirective[] ResourceDirectives { get; set; }

        /// <summary>
        /// Contains the index definitions specified in the template.
        /// </summary>
        public IndexDirective[] IndexDirectives { get; set; }

        /// <summary>
        /// Set of parameters
        /// </summary>
        public TemplateParameterInfo[] Parameters { get; set; }

        public event EventHandler<ProgressArgs> Progress;

        protected virtual void OnProgress(string action, int? percent = null)
        {
            EventHandler<ProgressArgs> handler = this.Progress;
            if (handler != null)
                handler(this, new ProgressArgs(action, percent));
        }

        /// <summary>
        /// Applies the loaded templates to <paramref name="settings"/>.
        /// </summary>
        /// <param name="inputDocument"></param>
        /// <param name="settings">
        ///     Instance of <see cref="TemplateSettings"/> containing the various input data needed. 
        /// </param>
        public virtual TemplateOutput Generate(XDocument inputDocument, TemplateSettings settings)
        {
            Stopwatch timer = Stopwatch.StartNew();
            
            StorageResolver storageResolver = new StorageResolver();
            storageResolver.Add(Storage.UriSchemeTemporary, new TemporaryFileProvider(), stripScheme: true);
            storageResolver.Add(Storage.UriSchemeOutput, settings.OutputFileProvider, stripScheme: true);
            storageResolver.Add(Storage.UriSchemeTemplate, this.TemplateFileProvider, stripScheme: true);
            // TODO file, http, https, ftp, etc shouldn't be hardcoded
            storageResolver.Add("file", new DirectoryFileProvider(), stripScheme: true);
            storageResolver.Add("http", new HttpFileProvider(), stripScheme: false);
            storageResolver.Add("https", new HttpFileProvider(), stripScheme: false);

            FileReference inputFileRef = storageResolver.Resolve(Storage.InputDocumentUri);
            using (Stream inputStream = inputFileRef.GetStream(FileMode.Create))
            using (XmlWriter inputWriter = XmlWriter.Create(inputStream, new XmlWriterSettings { Encoding = Encoding.UTF8, CloseOutput = false}))
            {
                inputDocument.Save(inputWriter);
                inputWriter.Close();
                inputStream.Close();
            }

            DependencyProvider dependencyProvider = new DependencyProvider(settings.CancellationToken);

            List<IAssetUriResolver> assetUriResolvers = new List<IAssetUriResolver>();
            assetUriResolvers.Add(settings.FileResolver);
            if (settings.UriResolvers != null)
                assetUriResolvers.AddRange(settings.UriResolvers);

            // create context
            ITemplatingContext context = new TemplatingContext(settings.Cache,
                                                               settings.Catalog,
                                                               settings,
                                                               inputDocument,
                                                               assetUriResolvers,
                                                               storageResolver,
                                                               dependencyProvider);

            // collect all work that has to be done
            Task<WorkUnitResult>[] work = this.DiscoverWork(inputDocument, context).ToArray();

            // fill indices
            using (TraceSources.TemplateSource.TraceActivity("Indexing input document"))
            {
                this.OnProgress("Indexing input");
                var customXsltContext = CustomXsltContext.Create(settings.IgnoredVersionComponent);
                foreach (var index in this.IndexDirectives)
                {
                    TraceSources.TemplateSource.TraceVerbose("Adding index {0} (match: '{1}', key: '{1}')",
                                                             index.Name,
                                                             index.MatchExpression,
                                                             index.KeyExpression);
                    context.DocumentIndex.AddKey(index.Name, index.MatchExpression, index.KeyExpression, customXsltContext);
                }

                TraceSources.TemplateSource.TraceInformation("Indexing...");
                context.DocumentIndex.BuildIndexes();
            }

            ParallelOptions parallelOptions = new ParallelOptions
                                              {
                                                  //MaxDegreeOfParallelism = 1
                                                  CancellationToken = settings.CancellationToken
                                              };

            // TODO is this really the best way to execute the tasks?
            Parallel.ForEach(work,
                             parallelOptions,
                             t =>
                             {
                                 if (t.Status == TaskStatus.Created)
                                     t.Start();
                             });

            Task.WaitAll(work, settings.CancellationToken);

            List<WorkUnitResult> results = work.Select(t => t.Result).ToList();
       
      
            //int totalCount = work.Length;
            //long lastProgress = Stopwatch.GetTimestamp();
            //int processed = 0;


            //IEnumerable<UnitOfWork> unitsOfWork = work;
            //if (settings.Filter != null)
            //{
            //    unitsOfWork = unitsOfWork
            //        .Where(uow =>
            //               {
            //                   if (settings.Filter(uow))
            //                       return true;

            //                   TraceSources.TemplateSource.TraceVerbose("Filtered unit of work: [{0}] {1}",
            //                                                            uow.GetType().Name,
            //                                                            uow.ToString());
            //                   return false;
            //               });
            //}


            //Parallel.ForEach(unitsOfWork,
            //                 parallelOptions,
            //                 uow =>
            //                 {
            //                     results.Add(uow.Execute(context));
            //                     int c = Interlocked.Increment(ref processed);
            //                     long lp = Interlocked.Read(ref lastProgress);
            //                     if ((Stopwatch.GetTimestamp() - lp) / (double)Stopwatch.Frequency > 5.0)
            //                     {
            //                         if (Interlocked.CompareExchange(ref lastProgress,
            //                                                         Stopwatch.GetTimestamp(),
            //                                                         lp) == lp)
            //                         {
            //                             double percent = c / (double)totalCount;
            //                             this.OnProgress("Generating", (int)Math.Round(percent));
            //                             TraceSources.TemplateSource.TraceInformation("Progress: {0:P1} ({1:N0}/{2:N0})",
            //                                                                          percent,
            //                                                                          c,
            //                                                                          totalCount);
            //                         }
            //                     }
            //                 });

            // stop timing
            timer.Stop();

            this.OnProgress("Generating", 100);

            #region statistics (hack)
            Stopwatch statsTimer = new Stopwatch();
            // prepare stats

            Dictionary<Type, WorkUnitResult[]> resultGroups = results.GroupBy(ps => ps.WorkUnit.GetType()).ToDictionary(g => g.Key, g => g.ToArray());

            if (resultGroups.ContainsKey(typeof(StylesheetApplication)))
            {
                var stylesheetStats = resultGroups[typeof(StylesheetApplication)].GroupBy(r => ((StylesheetApplication)r.WorkUnit).Stylesheet);

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
            }

            if (resultGroups.ContainsKey(typeof(ResourceDeployment)))
            {
                WorkUnitResult[] resourceStats = resultGroups[typeof(ResourceDeployment)];

                foreach (var statGroup in resourceStats)
                {
                    TraceSources.TemplateSource.TraceInformation("Deployed resource '{0}' in {1:N0} ms",
                                                                 ((ResourceDeployment)statGroup.WorkUnit).Input,
                                                                 statGroup.Duration);
                }
            }

            TraceSources.TemplateSource.TraceInformation("Statistics generated in {0:N1} seconds",
                                             statsTimer.Elapsed.TotalSeconds);

            #endregion

            TraceSources.TemplateSource.TraceInformation("Documentation generated in {0:N1} seconds (processing time: {1:N1} seconds)",
                                                         timer.Elapsed.TotalSeconds,
                                                         results.Sum(ps => ps.Duration) / 1000000.0);


            return new TemplateOutput(results.ToArray());
        }

        private IEnumerable<Task<WorkUnitResult>> DiscoverWork(XDocument inputDocument, ITemplatingContext context)
        {
            CustomXsltContext xsltContext = CustomXsltContext.Create(context.Settings.IgnoredVersionComponent);
            xsltContext.PushVariableScope(inputDocument.Root, this.GetGlobalParameters(this.Parameters, context.Settings.Arguments));

            ITemplateContext templateContext = new TemplateContext(context.Cache,
                                                                   inputDocument,
                                                                   xsltContext,
                                                                   context.Settings.FileResolver,
                                                                   context.Settings.Catalog,
                                                                   this.TemplateFileProvider,
                                                                   context.StorageResolver, 
                                                                   context.DependencyProvider);


            // the directives _have_ to be processed in declaration order
            IOrderedEnumerable<ITemplateDirective> directives = this.ResourceDirectives
                                                                    .Cast<ITemplateDirective>()
                                                                    .Concat(this.StylesheetsDirectives)
                                                                    .OrderBy(dir => dir.Ordinal);


            this.OnProgress("Processing directives", 0);

            foreach (var templateDirective in directives)
            {
                IEnumerable<UnitOfWork> work = templateDirective.DiscoverWork(templateContext);

                foreach (UnitOfWork unitOfWork in work)
                {
                    Func<object, WorkUnitResult> func =
                        uow =>
                        {
                            var ctx = context;
                            var unit = (UnitOfWork)uow;
                            FileReference outputRef;
                            // TODO maybe InputDocumentUri needs to be random so we don't get this collision
                            if (unit.Output != Storage.InputDocumentUri &&
                                ctx.DependencyProvider.IsFinal(unit.Output, unit.Ordinal))
                                outputRef = ctx.StorageResolver.Resolve(unit.Output);
                            else
                            {
                                // redirect intermediary output files to the temp:// provider
                                string path = unit.Output.OriginalString.Substring(unit.Output.Scheme.Length);
                                string uriSuffix = '.' + unit.Ordinal.ToString(CultureInfo.InvariantCulture);
                                Uri tempOutput = new Uri(Storage.UriSchemeTemporary + path + uriSuffix);
                                outputRef = ctx.StorageResolver.Resolve(tempOutput);
                            }

                            using (Stream outputStream = outputRef.GetStream(FileMode.Create))
                            {
                                var ticks = Stopwatch.GetTimestamp();
                                unit.Execute(context, outputStream);
                                outputStream.Close();
                                ticks = Stopwatch.GetTimestamp() - ticks;
                                return new WorkUnitResult(outputRef,
                                                          unit,
                                                          (long)
                                                          Math.Round(ticks / (double)Stopwatch.Frequency * 1000000));
                            }
                        };

                    Task<WorkUnitResult> task = new Task<WorkUnitResult>(func, unitOfWork);
                    context.DependencyProvider.Add(unitOfWork.Output, unitOfWork.Ordinal, task);
                    yield return task;
                }
            }
            this.OnProgress("Processing directives", 100);
        }

        protected virtual List<XPathVariable> GetGlobalParameters(IEnumerable<TemplateParameterInfo> parameters, Dictionary<string, object> arguments)
        {
            List<XPathVariable> globalParams = new List<XPathVariable>();
            foreach (var parameterInfo in parameters)
            {
                object argValue;
                if (arguments.TryGetValue(parameterInfo.Name, out argValue))
                    globalParams.Add(new ConstantXPathVariable(parameterInfo.Name, argValue));
                else
                    globalParams.Add(new ExpressionXPathVariable(parameterInfo.Name, parameterInfo.DefaultExpression));
            }
            return globalParams;
        }
    }
}
