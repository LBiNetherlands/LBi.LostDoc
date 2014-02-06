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
                                     Uri outputPath,
                                     XNode inputNode,
                                     IEnumerable<KeyValuePair<string, object>> xsltParams,
                                     IEnumerable<AssetIdentifier> assetIdentifiers,
                                     string stylesheetName,
                                     IEnumerable<AssetSection> sections,
                                     Uri input,
                                     XslCompiledTransform transform,
                                     XmlResolver xmlResolver)
            : base(outputPath, order)
        {
            this.InputNode = inputNode;
            this.XsltParams = xsltParams.ToArray();
            this.AssetIdentifiers = assetIdentifiers.ToArray();
            this.StylesheetName = stylesheetName;
            this.Sections = sections.ToArray();
            this.Input = input;
            this.Transform = transform;
            this.XmlResolver = xmlResolver;
        }

        public XslCompiledTransform Transform { get; protected set; }

        public KeyValuePair<string, object>[] XsltParams { get; protected set; }

        public XNode InputNode { get; protected set; }

        public AssetIdentifier[] AssetIdentifiers { get; protected set; }

        public string StylesheetName { get; protected set; }

        public AssetSection[] Sections { get; protected set; }

        public Uri Input { get; protected set; }

        public XmlResolver XmlResolver { get; protected set; }

        public override WorkUnitResult Execute(ITemplatingContext context)
        {
            Stopwatch localTimer = Stopwatch.StartNew();

            FileMode mode = FileMode.CreateNew;
            bool exists;
            if (!(exists = context.Settings.OutputFileProvider.FileExists(this.Path.ToString())) || context.Settings.OverwriteExistingFiles)
            {
                if (exists)
                    mode = FileMode.Create;

                // register xslt params
                XsltArgumentList argList = new XsltArgumentList();
                foreach (KeyValuePair<string, object> kvp in this.XsltParams)
                    argList.AddParam(kvp.Key, string.Empty, kvp.Value);

                argList.XsltMessageEncountered += (s, e) => TraceSources.TemplateSource.TraceInformation("Message: {0}.", e.Message);

                // and custom extensions
                argList.AddExtensionObject(Namespaces.Template, new TemplateXsltExtensions(context, this.Path));

                using (Stream stream = context.Settings.OutputFileProvider.OpenFile(this.Path.ToString(), mode))
                using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = Encoding.UTF8, CloseOutput = false }))
                {
                    if (exists)
                        TraceSources.TemplateSource.TraceWarning("Replacing {0}", this.Path);
                    else
                        TraceSources.TemplateSource.TraceVerbose("{0}", this.Path);

                    long tickStart = localTimer.ElapsedTicks;

                    this.Transform.Transform(context.Document,
                                             argList,
                                             writer,
                                             new XmlFileProviderResolver(new StackedFileProvider(context.TemplateFileProvider)));

                    double duration = ((localTimer.ElapsedTicks - tickStart) / (double)Stopwatch.Frequency) * 1000;

                    TraceSources.TemplateSource.TraceVerbose("Transform applied in: {0:N0} ms", duration);

                    writer.Close();
                    stream.Close();
                }
            }
            else
            {
                TraceSources.TemplateSource.TraceWarning("Skipping {0}", this.Path);
            }

            localTimer.Stop();

            return new WorkUnitResult(context.OutputFileProvider,
                                      this,
                                      (long)Math.Round(localTimer.ElapsedTicks / (double)Stopwatch.Frequency * 1000000));
        }
    }
}