﻿/*
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Templating;
using LBi.LostDoc.Repository.Lucene;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Directory = System.IO.Directory;

namespace LBi.LostDoc.Repository
{
    /// <summary>
    /// This class builds content.
    /// </summary>
    public class ContentBuilder
    {
        private State _state;

        public ContentBuilder()
        {
            this._state = State.Idle;
        }

        public event Action<object, State> StateChanged;

        protected void OnStateChanged(State newState)
        {
            this._state = newState;
            Action<object, State> handler = this.StateChanged;
            if (handler != null) handler(this, this._state);
        }

        public State CurrentState { get { return this._state; } }

        /// <summary>
        /// Gets or sets the ignored <see cref="VersionComponent"/>.
        /// </summary>
        public VersionComponent? IgnoreVersionComponent { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Template"/>.
        /// </summary>
        public Template Template { get; set; }

        /// <summary>
        /// This method will construct a three folder structure inside <paramref name="targetDirectory"/> containing: Html, Index, and Source
        /// </summary>
        /// <param name="sourceDirectory">Directory containing ldoc files</param>
        /// <param name="targetDirectory">Output directory</param>
        public void Build(string sourceDirectory, string targetDirectory)
        {
            if (Directory.Exists(targetDirectory) && Directory.EnumerateFileSystemEntries(targetDirectory).Any())
                throw new InvalidOperationException("Target path is not empty.");

            this.OnStateChanged(State.Preparing);

            string htmlRoot = Path.Combine(targetDirectory, "Html");
            string indexRoot = Path.Combine(targetDirectory, "Index");
            string sourceRoot = Path.Combine(targetDirectory, "Source");
            string logRoot = Path.Combine(targetDirectory, "Logs");


            DirectoryInfo htmlDir = Directory.CreateDirectory(htmlRoot);
            DirectoryInfo indexDir = Directory.CreateDirectory(indexRoot);
            DirectoryInfo sourceDir = Directory.CreateDirectory(sourceRoot);
            DirectoryInfo logDir = Directory.CreateDirectory(logRoot);
            var sourceFiles = Directory.EnumerateFiles(sourceDirectory, "*.ldoc", SearchOption.TopDirectoryOnly);

            // copy all source files to output directory and add to bundle
            Bundle bundle = new Bundle(this.IgnoreVersionComponent);
            foreach (var sourceFile in sourceFiles)
            {
                string targetFile = Path.Combine(sourceDir.FullName, Path.GetFileName(sourceFile));
                File.Copy(sourceFile, targetFile);
                bundle.Add(XDocument.Load(targetFile));
            }

            TemplateOutput templateOutput;

            // wire up logging
            using (TextWriterTraceListener traceListener =
                new TextWriterTraceListener(Path.Combine(logDir.FullName,
                                                         string.Format(
                                                             "template_{0:yyyy'_'MM'_'dd'__'HH'_'mm'_'ss}.log",
                                                             DateTime.Now))))
            {

                // log everything
                traceListener.Filter = new EventTypeFilter(SourceLevels.All);
                LostDoc.Diagnostics.TraceSources.TemplateSource.Switch.Level = SourceLevels.All;
                LostDoc.Diagnostics.TraceSources.BundleSource.Switch.Level = SourceLevels.All;
                LostDoc.Diagnostics.TraceSources.AssetResolverSource.Switch.Level = SourceLevels.All;
                LostDoc.Diagnostics.TraceSources.TemplateSource.Listeners.Add(traceListener);
                LostDoc.Diagnostics.TraceSources.BundleSource.Listeners.Add(traceListener);
                LostDoc.Diagnostics.TraceSources.AssetResolverSource.Listeners.Add(traceListener);

                // merge ldoc files
                this.OnStateChanged(State.Merging);
                AssetRedirectCollection assetRedirects;
                var mergedDoc = bundle.Merge(out assetRedirects);

                // generate output
                var templateData = new TemplateData(mergedDoc)
                                       {
                                           AssetRedirects = assetRedirects,
                                           IgnoredVersionComponent = this.IgnoreVersionComponent,
                                           TargetDirectory = htmlDir.FullName,
                                           Arguments = new Dictionary<string, object> {{"SearchUri", "/search/"}},
                                           KeepTemporaryFiles = true,
                                           TemporaryFilesPath = Path.Combine(logDir.FullName, "temp")
                                       };

                this.OnStateChanged(State.Templating);
                templateOutput = this.Template.Generate(templateData);

                LostDoc.Diagnostics.TraceSources.TemplateSource.Listeners.Remove(traceListener);
                LostDoc.Diagnostics.TraceSources.BundleSource.Listeners.Remove(traceListener);
                LostDoc.Diagnostics.TraceSources.AssetResolverSource.Listeners.Remove(traceListener);
            }

            this.OnStateChanged(State.Indexing);

            using (TextWriterTraceListener traceListener =
                new TextWriterTraceListener(Path.Combine(logDir.FullName,
                                                         string.Format(
                                                             "index_{0:yyyy'_'MM'_'dd'__'HH'_'mm'_'ss}.log",
                                                             DateTime.Now))))
            {
                // log everything
                traceListener.Filter = new EventTypeFilter(SourceLevels.All);
                TraceSources.ContentBuilderSource.Switch.Level = SourceLevels.All;
                TraceSources.ContentBuilderSource.Listeners.Add(traceListener);
                

                // one stop-word per line
                StringReader stopWordsReader = new StringReader(@"missing");

                // index output
                using (var directory = FSDirectory.Open(indexDir))
                using (stopWordsReader)
                {
                    Analyzer analyzer = new StandardAnalyzer(global::Lucene.Net.Util.Version.LUCENE_30, stopWordsReader);
                    Analyzer titleAnalyzer = new TitleAnalyzer();
                    IDictionary<string, Analyzer> fieldAnalyzers = new Dictionary<string, Analyzer>
                                                                       {
                                                                           {"title", titleAnalyzer}
                                                                       };

                    PerFieldAnalyzerWrapper analyzerWrapper = new PerFieldAnalyzerWrapper(analyzer, fieldAnalyzers);

                    using (
                        var writer = new IndexWriter(directory, analyzerWrapper, IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        var saResults =
                            templateOutput.Results.Select(wur => wur.WorkUnit).OfType<StylesheetApplication>();

                        var saDict = saResults.ToDictionary(sa => sa.Asset);

                        var indexResults = saDict.Values.Where(sa => sa.SaveAs.EndsWith(".xml"));

                        foreach (var sa in indexResults)
                        {
                            string absPath = Path.Combine(htmlDir.FullName, sa.SaveAs);

                            XDocument indexDoc = XDocument.Load(absPath);

                            string assetId = indexDoc.Root.Attribute("assetId").Value;
                            string title = indexDoc.Root.Element("title").Value.Trim();
                            string summary = indexDoc.Root.Element("summary").Value.Trim();
                            string text = indexDoc.Root.Element("text").Value.Trim();

                            var ssApplication = saDict[AssetIdentifier.Parse(assetId)];


                            var doc = new Document();

                            doc.Add(new Field("uri",
                                              new Uri(ssApplication.SaveAs, UriKind.Relative).ToString(),
                                              Field.Store.YES,
                                              Field.Index.NO));
                            doc.Add(new Field("aid", ssApplication.Asset, Field.Store.YES, Field.Index.NOT_ANALYZED));
                            foreach (AssetIdentifier aid in ssApplication.Aliases)
                                doc.Add(new Field("alias", aid, Field.Store.NO, Field.Index.NOT_ANALYZED));

                            foreach (var section in ssApplication.Sections)
                            {
                                doc.Add(new Field("section",
                                                  section.AssetIdentifier,
                                                  Field.Store.NO,
                                                  Field.Index.NOT_ANALYZED));
                            }

                            doc.Add(new Field("title", title, Field.Store.YES, Field.Index.ANALYZED));
                            doc.Add(new Field("summary", summary, Field.Store.YES, Field.Index.ANALYZED));
                            doc.Add(new Field("content", text, Field.Store.NO, Field.Index.ANALYZED));
                            TraceSources.ContentBuilderSource.TraceVerbose("Indexing document: {0}", doc.ToString());
                            writer.AddDocument(doc);
                        }

                        writer.Optimize();
                        writer.Commit();
                    }
                    analyzerWrapper.Close();
                    analyzer.Close();
                }

                TraceSources.ContentBuilderSource.Listeners.Remove(traceListener);
            }
            this.OnStateChanged(State.Finalizing);

            var infoDoc = new XDocument(
                new XElement("content",
                             new XAttribute("created",
                                            XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc)),
                             templateOutput.Results.Select(ConvertToXml)));

            infoDoc.Save(Path.Combine(targetDirectory, "info.xml"));

            this.OnStateChanged(State.Idle);
        }

        private XElement ConvertToXml(WorkUnitResult wu)
        {
            return new XElement("result",
                                new XAttribute("duration", XmlConvert.ToString(Math.Round((double)wu.Duration/1000.0, 2))),
                                wu.WorkUnit is ResourceDeployment ? ResourceToXml(wu) : StylesheetToXml(wu));
        }

        private static XElement StylesheetToXml(WorkUnitResult wu)
        {
            StylesheetApplication ssWu = ((StylesheetApplication)wu.WorkUnit);
            return new XElement("document",
                                new XAttribute("assetId", ssWu.Asset),
                                new XAttribute("output", ssWu.SaveAs),
                                ssWu.Aliases.Select(
                                                    wual =>
                                                    new XElement("alias", new XAttribute("assetId", wual))),
                                ssWu.Sections.Select(
                                                     wuse =>
                                                     new XElement("section",
                                                         new XAttribute("name",wuse.Name),
                                                         new XAttribute("assetId",wuse.AssetIdentifier))),
                                new XElement("template",
                                             new XAttribute("name", ssWu.StylesheetName)));
        }

        private static XElement ResourceToXml(WorkUnitResult wu)
        {
            return new XElement("resource",
                                new XAttribute("path", ((ResourceDeployment)wu.WorkUnit).ResourcePath));
        }
    }
}
