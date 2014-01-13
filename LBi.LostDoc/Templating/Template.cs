/*
 * Copyright 2012-2013 DigitasLBi Netherlands B.V.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{
    public class Template
    {
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
        /// Contains the processed source document, required for template inheritence.
        /// </summary>
        public XDocument Source { get; set; }

        /// <summary>
        /// Set of parameters
        /// </summary>
        public XPathVariable[] Parameters { get; set; }

        /// <summary>
        /// Temporary files generated while templating, useful for debugging.
        /// </summary>
        public TempFileCollection TemporaryFiles { get; set; }

        // TODO implement this
        public event EventHandler<ProgressArgs> Progress;

        protected virtual void OnProgress(int percent)
        {
            EventHandler<ProgressArgs> handler = this.Progress;
            if (handler != null)
                handler(this, new ProgressArgs(percent));
        }

        public virtual IEnumerable<UnitOfWork> DiscoverWork(ITemplateContext context)
        {
            var ret = Enumerable.Empty<UnitOfWork>();

            context.XsltContext.PushVariableScope(context.Document.Root, this.Parameters);

            foreach (ResourceDirective resource in this.ResourceDirectives)
                ret = ret.Concat(resource.DiscoverWork(context));

            foreach (StylesheetDirective stylesheet in this.StylesheetsDirectives)
                ret = ret.Concat(stylesheet.DiscoverWork(context));

            context.XsltContext.PopVariableScope();

            return ret;
        }

        /// <summary>
        /// Applies the loaded templates to <paramref name="settings"/>.
        /// </summary>
        /// <param name="settings">
        /// Instance of <see cref="TemplateData"/> containing the various input data needed. 
        /// </param>
        public virtual TemplateOutput Generate(TemplateSettings settings, XDocument inputDocument)
        {
            Stopwatch timer = Stopwatch.StartNew();

            this._uriFactory.Clear();
            this._fileResolver.Clear();

            //ParsedTemplate tmpl = this.PrepareTemplate(settings);

            // collect all work that has to be done
            CustomXsltContext xsltContext = CreateCustomXsltContext(settings.IgnoredVersionComponent);
            xsltContext.PushVariableScope(inputDocument.Root, this.Parameters);
            ITemplateContext templateContext = new TemplateContext(this._cache,
                                                                   inputDocument,
                                                                   xsltContext,
                                                                   this._uriFactory,
                                                                   this._fileResolver,
                                                                   this._container.Catalog);

            UnitOfWork[] work = this.DiscoverWork(templateContext).ToArray();

            // TODO this isn't true (total number of work-units include resource deployments)
            TraceSources.TemplateSource.TraceInformation("Generating {0:N0} documents from {1:N0} stylesheets.",
                                                         work.Length,
                                                         this.StylesheetsDirectives.Length);

            ConcurrentBag<WorkUnitResult> results = new ConcurrentBag<WorkUnitResult>();

            // create context
            ITemplatingContext context = new TemplatingContext(this._cache,
                                                               this._container.Catalog,
                                                               settings.OutputFileProvider, // TODO fix this (this._basePath)
                                                               settings,
                                                               inputDocument,
                                                               this._resolvers,
                                                               this._templateInfo.Source);


            // fill indices
            using (TraceSources.TemplateSource.TraceActivity("Indexing input document"))
            {
                var customXsltContext = CustomXsltContext.Create(settings.IgnoredVersionComponent);
                foreach (var index in this.IndexDirectives)
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

            List<Task<WorkUnitResult>> tasks = new List<Task<WorkUnitResult>>();

            // register all tasks in the dependency provider
            foreach (UnitOfWork unitOfWork in work)
            {
                Task<WorkUnitResult> task = new Task<WorkUnitResult>(uow => ((UnitOfWork)uow).Execute(context), unitOfWork);
                tasks.Add(task);
                this._dependencyProvider.Add(new Uri(unitOfWork.Path, UriKind.RelativeOrAbsolute), task);
            }
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
            if (settings.Filter != null)
            {
                unitsOfWork = unitsOfWork
                    .Where(uow =>
                    {
                        if (settings.Filter(uow))
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

            return new TemplateOutput(results.ToArray(), this.TemporaryFiles);
        }
    }
}
