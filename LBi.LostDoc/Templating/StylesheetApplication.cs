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
using System.Text;
using System.Xml.Linq;
using System.Xml.Xsl;
using LBi.LostDoc.Diagnostics;

namespace LBi.LostDoc.Templating
{
    public class StylesheetApplication : UnitOfWork
    {
        public XslCompiledTransform Transform { get; set; }
        public IEnumerable<KeyValuePair<string, object>> XsltParams { get; set; }
        public string SaveAs { get; set; }
        public XNode InputNode { get; set; }
        public AssetIdentifier[] AssetIdentifiers { get; set; }
        public string StylesheetName { get; set; }
        public List<AssetSection> Sections { get; set; }
        public string Input { get; set; }

        public override WorkUnitResult Execute(ITemplatingContext context)
        {
            Stopwatch localTimer = Stopwatch.StartNew();

            Uri newUri = new Uri(this.SaveAs, UriKind.RelativeOrAbsolute);
            FileMode mode = FileMode.CreateNew;
            bool exists;
            if (!(exists = context.TemplateData.OutputFileProvider.FileExists(this.SaveAs)) || context.TemplateData.OverwriteExistingFiles)
            {
                if (exists)
                    mode = FileMode.Create;

                // register xslt params
                XsltArgumentList argList = new XsltArgumentList();
                foreach (KeyValuePair<string, object> kvp in this.XsltParams)
                    argList.AddParam(kvp.Key, string.Empty, kvp.Value);

                argList.XsltMessageEncountered +=
                    (s, e) => TraceSources.TemplateSource.TraceInformation("Message: {0}.", e.Message);

                // and custom extensions
                argList.AddExtensionObject(Namespaces.Template, new TemplateXsltExtensions(context, newUri));

                using (Stream stream = context.TemplateData.OutputFileProvider.OpenFile(this.SaveAs, mode))
                using (TextWriter writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    if (exists)
                    {
                        TraceSources.TemplateSource.TraceWarning("Replacing {0}", this.SaveAs);
                    }
                    else
                    {
                        TraceSources.TemplateSource.TraceVerbose("{0}", this.SaveAs);
                    }
                    long tickStart = localTimer.ElapsedTicks;
                    this.Transform.Transform(context.Document, argList, writer);
                    TraceSources.TemplateSource.TraceVerbose("Transform applied in: {0:N0} ms",
                                                             ((localTimer.ElapsedTicks - tickStart) /
                                                              (double)Stopwatch.Frequency) * 1000);

                    writer.Close();
                    stream.Close();
                }
            }
            else
            {
                TraceSources.TemplateSource.TraceWarning("Skipping {0}", newUri);
            }

            localTimer.Stop();

            WorkUnitResult result = new WorkUnitResult
                                        {
                                            WorkUnit = this,
                                            Duration = (long)Math.Round(localTimer.ElapsedTicks/(double)Stopwatch.Frequency*1000000)
                                        };
            return result;
        }
    }
}
