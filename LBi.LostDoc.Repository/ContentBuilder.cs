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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Repository.Lucene;
using LBi.LostDoc.Templating;
using LBi.LostDoc.Templating.FileProviders;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Newtonsoft.Json;
using Directory = System.IO.Directory;

namespace LBi.LostDoc.Repository
{
    /// <summary>
    ///     This class builds content.
    /// </summary>
    public class ContentBuilder
    {
        private State _state;

        public ContentBuilder()
        {
            this._state = State.Idle;
        }

        public event Action<object, State> StateChanged;

        public State CurrentState
        {
            get { return this._state; }
        }

        /// <summary>
        ///     Gets or sets the ignored <see cref="VersionComponent" />.
        /// </summary>
        public VersionComponent? IgnoreVersionComponent { get; set; }

        /// <summary>
        ///     Gets or sets the <see cref="Template" />.
        /// </summary>
        public Template Template { get; set; }

        /// <summary>
        /// This method will construct a four folder structure inside <paramref name="targetDirectory"/> containing: Html, Index, Source and Logs
        /// </summary>
        /// <param name="sourceDirectory">
        /// Directory containing ldoc files
        /// </param>
        /// <param name="targetDirectory">
        /// Output directory
        /// </param>
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
            string templateLogFile = Path.Combine(logDir.FullName,
                                                  string.Format("template_{0:yyyy'_'MM'_'dd'__'HH'_'mm'_'ss}.log", DateTime.Now));
            using (TextWriterTraceListener traceListener = new TextWriterTraceListener(templateLogFile))
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
                                           OutputFileProvider = new ScopedFileProvider(new DirectoryFileProvider(), htmlDir.FullName),

                                           //TargetDirectory = htmlDir.FullName,
                                           Arguments = new Dictionary<string, object> { },
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

            string indexLogFile = Path.Combine(logDir.FullName,
                                               string.Format("index_{0:yyyy'_'MM'_'dd'__'HH'_'mm'_'ss}.log", DateTime.Now));
            using (TextWriterTraceListener traceListener = new TextWriterTraceListener(indexLogFile))
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
                    Analyzer nameAnalyzer = new NameAnalyzer();
                    Analyzer camelCaseAnalyzer = new CamelCaseAnalyzer();
                    IDictionary<string, Analyzer> fieldAnalyzers = new Dictionary<string, Analyzer>
                                                                   {
                                                                       { "title", titleAnalyzer },
                                                                       { "name", nameAnalyzer },
                                                                       { "camelCase", camelCaseAnalyzer }
                                                                   };

                    PerFieldAnalyzerWrapper analyzerWrapper = new PerFieldAnalyzerWrapper(analyzer, fieldAnalyzers);

                    using (
                        var writer = new IndexWriter(directory, analyzerWrapper, IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        var saResults =
                            templateOutput.Results.Select(wur => wur.WorkUnit).OfType<StylesheetApplication>();

                        Dictionary<AssetIdentifier, StylesheetApplication> saDict = saResults.ToDictionary(sa => sa.Asset);

                        var indexResults = saDict.Values.Where(sa => sa.SaveAs.EndsWith(".xml"));

                        foreach (var sa in indexResults)
                        {
                            string absPath = Path.Combine(htmlDir.FullName, sa.SaveAs);

                            XDocument indexDoc = XDocument.Load(absPath);

                            foreach (var docElement in indexDoc.Root.Elements())
                            {

                                string assetId = docElement.Attribute("assetId").Value;
                                string name = docElement.Element("name").Value.Trim();
                                string title = docElement.Element("title").Value.Trim();
                                string summary = docElement.Element("summary").Value.Trim();
                                string text = docElement.Element("text").Value.Trim();
                                string type = docElement.Element("type").Value.Trim();

                                var typeFlags = docElement.Element("type")
                                                          .Attributes()
                                                          .Where(a =>
                                                                 a.Name.LocalName.StartsWith("is") &&
                                                                 XmlConvert.ToBoolean(a.Value))
                                                          .Select(a => a.Name.LocalName);

                                var path = docElement.Element("path").Elements("fragment");

                                StylesheetApplication ssApplication;

                                // TODO we blindly generate index data for everything with an assetId which means that sometiems we get misses here
                                if (!saDict.TryGetValue(AssetIdentifier.Parse(assetId), out ssApplication))
                                    continue;

                                Document doc = new Document();
                                
                                doc.Add(new Field("uri",
                                                  new Uri(ssApplication.SaveAs, UriKind.Relative).ToString(),
                                                  Field.Store.YES,
                                                  Field.Index.NO));
                                doc.Add(new Field("aid", ssApplication.Asset, Field.Store.YES, Field.Index.NOT_ANALYZED));

                                doc.Add(new Field("type", type, Field.Store.YES, Field.Index.NOT_ANALYZED));

                                foreach (string typeFlag in typeFlags)
                                {
                                    doc.Add(new Field("typeFlag", typeFlag, Field.Store.YES, Field.Index.NOT_ANALYZED));
                                }

                                foreach (AssetIdentifier aid in ssApplication.Aliases)
                                    doc.Add(new Field("alias", aid, Field.Store.NO, Field.Index.NOT_ANALYZED));

                                foreach (var section in ssApplication.Sections)
                                {
                                    doc.Add(new Field("section",
                                                      section.AssetIdentifier,
                                                      Field.Store.NO,
                                                      Field.Index.NOT_ANALYZED));
                                }

                                doc.Add(new Field("name", name, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
                                doc.Add(new Field("camelCase", name, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
                                doc.Add(new Field("title", title, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
                                doc.Add(new Field("summary", summary, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
                                doc.Add(new Field("content", text, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));

                                using (StringWriter stringWriter = new StringWriter())
                                {
                                    JsonWriter jsonWriter = new JsonTextWriter(stringWriter);
                                    jsonWriter.WriteStartArray();
                                    foreach (var element in path)
                                    {
                                        jsonWriter.WriteStartObject();
                                        jsonWriter.WritePropertyName("assetId");
                                        jsonWriter.WriteValue(element.Attribute("assetId").Value);
                                        jsonWriter.WritePropertyName("name");
                                        jsonWriter.WriteValue(element.Attribute("name").Value);
                                        jsonWriter.WritePropertyName("url");
                                        jsonWriter.WriteValue(element.Attribute("url").Value);
                                        jsonWriter.WritePropertyName("blurb");
                                        jsonWriter.WriteValue(element.Attribute("blurb").Value);
                                        jsonWriter.WritePropertyName("type");
                                        jsonWriter.WriteValue(element.Attribute("type").Value);
                                        jsonWriter.WriteEndObject();
                                    }
                                    jsonWriter.WriteEndArray();
                                    doc.Add(new Field("path",
                                                      stringWriter.ToString(),
                                                      Field.Store.YES,
                                                      Field.Index.NO,
                                                      Field.TermVector.NO));
                                }

                                TraceSources.ContentBuilderSource.TraceVerbose("Indexing document: {0}", doc.ToString());
                                writer.AddDocument(doc);
                            }
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
                             templateOutput.Results.Select(this.ConvertToXml)));

            infoDoc.Save(Path.Combine(targetDirectory, "info.xml"));

            this.OnStateChanged(State.Idle);
        }

        protected void OnStateChanged(State newState)
        {
            this._state = newState;
            Action<object, State> handler = this.StateChanged;
            if (handler != null) handler(this, this._state);
        }

        private static XElement ResourceToXml(WorkUnitResult wu)
        {
            return new XElement("resource",
                                new XAttribute("path", ((ResourceDeployment)wu.WorkUnit).ResourcePath));
        }

        private static XElement StylesheetToXml(WorkUnitResult wu)
        {
            StylesheetApplication ssWu = (StylesheetApplication)wu.WorkUnit;
            return new XElement("document",
                                new XAttribute("assetId", ssWu.Asset),
                                new XAttribute("output", ssWu.SaveAs),
                                ssWu.Aliases.Select(
                                    wual =>
                                    new XElement("alias", new XAttribute("assetId", wual))),
                                ssWu.Sections.Select(
                                    wuse =>
                                    new XElement("section",
                                                 new XAttribute("name", wuse.Name),
                                                 new XAttribute("assetId", wuse.AssetIdentifier))),
                                new XElement("template",
                                             new XAttribute("name", ssWu.StylesheetName)));
        }

        private XElement ConvertToXml(WorkUnitResult wu)
        {
            return new XElement("result",
                                new XAttribute("duration", XmlConvert.ToString(Math.Round(wu.Duration / 1000.0, 2))),
                                wu.WorkUnit is ResourceDeployment ? ResourceToXml(wu) : StylesheetToXml(wu));
        }
    }
}