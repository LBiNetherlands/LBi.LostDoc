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
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Templating.IO;

namespace LBi.LostDoc.Templating
{
    public class StylesheetApplication : UnitOfWork
    {
        public StylesheetApplication(int order,
                                     string stylesheetName,
                                     Uri stylesheet,
                                     Uri input,
                                     Uri output,
                                     XNode inputNode,
                                     IEnumerable<KeyValuePair<string, object>> xsltParams,
                                     IEnumerable<AssetIdentifier> assetIdentifiers,
                                     IEnumerable<AssetSection> sections,
                                     XmlResolver xmlResolver)
            : base(output, order)
        {
            this.Stylesheet = stylesheet;
            this.InputNode = inputNode;
            this.XsltParams = xsltParams.ToArray();
            this.AssetIdentifiers = assetIdentifiers.ToArray();
            this.StylesheetName = stylesheetName;
            this.Sections = sections.ToArray();
            this.Input = input;
            this.XmlResolver = xmlResolver;
        }

        public KeyValuePair<string, object>[] XsltParams { get; protected set; }

        public XNode InputNode { get; protected set; }

        public AssetIdentifier[] AssetIdentifiers { get; protected set; }

        public string StylesheetName { get; protected set; }

        public AssetSection[] Sections { get; protected set; }

        public Uri Input { get; protected set; }

        public Uri Stylesheet { get; protected set; }

        public XmlResolver XmlResolver { get; protected set; }

        public override WorkUnitResult Execute(ITemplatingContext context)
        {
            Stopwatch localTimer = Stopwatch.StartNew();

            FileReference outputFileRef = context.Storage.Resolve(this.Output);
            FileMode mode = FileMode.CreateNew;
            bool exists;
            if (!(exists = outputFileRef.Exists) || context.Settings.OverwriteExistingFiles)
            {
                if (exists)
                    mode = FileMode.Create;

                // register xslt params
                XsltArgumentList argList = new XsltArgumentList();
                foreach (KeyValuePair<string, object> kvp in this.XsltParams)
                    argList.AddParam(kvp.Key, string.Empty, kvp.Value);

                argList.XsltMessageEncountered += (s, e) => TraceSources.TemplateSource.TraceInformation("Message: {0}.", e.Message);

                // and custom extensions
                argList.AddExtensionObject(Namespaces.Template, new TemplateXsltExtensions(context, this.Output));

                using (Stream stream = outputFileRef.GetStream(mode))
                using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = Encoding.UTF8, CloseOutput = false }))
                {
                    if (exists)
                        TraceSources.TemplateSource.TraceWarning("Replacing {0}", this.Output);
                    else
                        TraceSources.TemplateSource.TraceVerbose("{0}", this.Output);

                    long tickStart = localTimer.ElapsedTicks;

                    var transform = this.LoadStylesheet(context);

                    transform.Transform(context.Document,
                                        argList,
                                        writer,
                                        new XmlFileProviderResolver(context.Storage, StorageSchemas.Temporary));

                    double duration = ((localTimer.ElapsedTicks - tickStart) / (double)Stopwatch.Frequency) * 1000;

                    TraceSources.TemplateSource.TraceVerbose("Transform applied in: {0:N0} ms", duration);

                    writer.Close();
                    stream.Close();
                }
            }
            else
            {
                TraceSources.TemplateSource.TraceWarning("Skipping {0}", this.Output);
            }

            localTimer.Stop();

            return new WorkUnitResult(outputFileRef.FileProvider,
                                      this,
                                      (long)Math.Round(localTimer.ElapsedTicks / (double)Stopwatch.Frequency * 1000000));
        }

        protected virtual XslCompiledTransform LoadStylesheet(ITemplatingContext context)
        {
            XslCompiledTransform ret = context.Cache.Get(this.Stylesheet.ToString()) as XslCompiledTransform;

            if (ret == null)
            {
                ret = new XslCompiledTransform(true);
                FileReference stylesheet = context.Storage.Resolve(this.Stylesheet);
                using (Stream str = stylesheet.GetStream())
                {
                    XmlReader reader = XmlReader.Create(str, new XmlReaderSettings { CloseInput = true, });
                    XsltSettings settings = new XsltSettings(true, true);
                    XmlResolver resolver = new XmlFileProviderResolver(context.Storage, StorageSchemas.Template);
                    ret.Load(reader, settings, resolver);
                }

                context.Cache.Add(this.Stylesheet.ToString(), ret, new CacheItemPolicy { Priority = CacheItemPriority.Default });
            }

            return ret;
        }
    }
}