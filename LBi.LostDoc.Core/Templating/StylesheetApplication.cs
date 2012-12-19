/*
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
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using LBi.Diagnostics;
using LBi.LostDoc.Core.Diagnostics;

namespace LBi.LostDoc.Core.Templating
{
    public class StylesheetApplication : UnitOfWork
    {
        public List<AssetIdentifier> Aliases { get; set; }
        public XslCompiledTransform Transform { get; set; }
        public IEnumerable<KeyValuePair<string, object>> XsltParams { get; set; }
        public string SaveAs { get; set; }
        public XElement InputElement { get; set; }
        public AssetIdentifier Asset { get; set; }
        public string StylesheetName { get; set; }
        public List<AssetSection> Sections { get; set; }

        public override WorkUnitResult Execute(ITemplatingContext context)
        {
            Stopwatch localTimer = Stopwatch.StartNew();

            string targetPath = Path.Combine(context.TemplateData.TargetDirectory, this.SaveAs);

            string targetDir = Path.GetDirectoryName(targetPath);
            if (targetDir != null && !Directory.Exists(targetDir))
            {
                TraceSources.TemplateSource.TraceVerbose("Creating directory: {0}", targetDir);

                // noop if exists
                Directory.CreateDirectory(targetDir);
            }

            Uri newUri = new Uri(this.SaveAs, UriKind.RelativeOrAbsolute);

            bool exists;
            if (!(exists = File.Exists(targetPath)) || context.TemplateData.OverwriteExistingFiles)
            {
                // register xslt params
                XsltArgumentList argList = new XsltArgumentList();
                foreach (KeyValuePair<string, object> kvp in this.XsltParams)
                    argList.AddParam(kvp.Key, string.Empty, kvp.Value);

                argList.XsltMessageEncountered +=
                    (s, e) => TraceSources.TemplateSource.TraceInformation("Message: {0}.", e.Message);

                // and custom extensions
                argList.AddExtensionObject("urn:lostdoc-core", new TemplateXsltExtensions(context, newUri));

                using (FileStream stream = File.Create(targetPath))
                using (XmlWriter xmlWriter = XmlWriter.Create(stream,
                                                              new XmlWriterSettings
                                                              {
                                                                  CloseOutput = true,
                                                                  Indent = true
                                                              }))
                {
                    if (exists)
                    {
                        TraceSources.TemplateSource.TraceWarning("{0}, {1} => Replacing {2}",
                                                                 this.Asset.AssetId,
                                                                 this.Asset.Version, this.SaveAs);
                    }
                    else
                    {
                        TraceSources.TemplateSource.TraceVerbose("{0}, {1} => {2}",
                                                             this.Asset.AssetId,
                                                             this.Asset.Version, this.SaveAs);
                    }

                    this.Transform.Transform(context.TemplateData.Document.CreateNavigator(),
                                             argList,
                                             xmlWriter);
                    xmlWriter.Close();
                }
            }
            else
            {
                TraceSources.TemplateSource.TraceWarning(
                                                         "{0}, {1} => Skipped, already generated ({2})",
                                                         this.Asset.AssetId, 
                                                         this.Asset.Version,
                                                         newUri);
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
