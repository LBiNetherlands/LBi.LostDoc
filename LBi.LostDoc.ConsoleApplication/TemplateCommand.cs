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

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using LBi.Cli.Arguments;
using LBi.LostDoc;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Templating;
using LBi.LostDoc.Templating.FileProviders;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.ConsoleApplication
{
    [ParameterSet("Template", Command = "Template", HelpMessage = "Apply template to a set of ldoc files to generate output.")]
    public class TemplateCommand : ICommand
    {
        [Parameter(HelpMessage = "Include errors and warning output only.")]
        public LBi.Cli.Arguments.Switch Quiet { get; set; }

        [Parameter(HelpMessage = "Include verbose output.")]
        public LBi.Cli.Arguments.Switch Verbose { get; set; }

        [Parameter(HelpMessage = "Overwrites existing files.")]
        public LBi.Cli.Arguments.Switch Force { get; set; }

        [Parameter(HelpMessage = "Source to ldoc file (or folder containing multiple ldoc files)."), Required]
        public string Path { get; set; }

        [Parameter(HelpMessage = "Template name or path"), Required]
        public string Template { get; set; }

        [Parameter(HelpMessage = "Output path.")]
        public string Output { get; set; }

        [Parameter(HelpMessage = "Which version components to ignore for deduplication.")]
        public VersionComponent? IgnoreVersionComponent { get; set; }

        [Parameter(HelpMessage = "Optional template arguments.")]
        [DefaultValue("@{}")]
        public Dictionary<string, object> Arguments { get; set; }

        #region ICommand Members

        public void Invoke()
        {
            var traceListener = new ConsolidatedConsoleTraceListener(
                new Dictionary<string, string>
                    {
                        {
                            "LostDoc.Core.Template",
                            "Template"
                        },
                        {
                            "LostDoc.Core.Bundle",
                            "Bundle"
                        },
                        {
                            "LostDoc.Core.Template.AssetResolver",
                            "Resolve"
                        }
                    });

            
            TraceSources.TemplateSource.Listeners.Add(traceListener);
            TraceSources.AssetResolverSource.Listeners.Add(traceListener);
            try
            {
                if (this.Quiet.IsPresent)
                {
                    const SourceLevels quietLevel = SourceLevels.Error | SourceLevels.Warning | SourceLevels.Critical;
                    TraceSources.TemplateSource.Switch.Level = quietLevel;
                    TraceSources.AssetResolverSource.Switch.Level = quietLevel;
                    TraceSources.BundleSource.Listeners.Add(traceListener);
                }
                else if (this.Verbose.IsPresent)
                {
                    const SourceLevels verboseLevel = SourceLevels.All;
                    TraceSources.TemplateSource.Switch.Level = verboseLevel;
                    TraceSources.AssetResolverSource.Switch.Level = verboseLevel;
                    TraceSources.BundleSource.Listeners.Add(traceListener);
                }
                else
                {
                    const SourceLevels normalLevel = SourceLevels.Information | SourceLevels.Warning | SourceLevels.Error | SourceLevels.ActivityTracing;
                    TraceSources.TemplateSource.Switch.Level = normalLevel;
                    TraceSources.AssetResolverSource.Switch.Level = normalLevel;
                }

                LinkedList<FileInfo> includedFiles = new LinkedList<FileInfo>();

                if (File.Exists(this.Path))
                    includedFiles.AddLast(new FileInfo(this.Path));
                else if (Directory.Exists(this.Path))
                {
                    Directory.GetFiles(this.Path, "*.ldoc", SearchOption.AllDirectories)
                             .Aggregate(includedFiles,
                                        (l, f) => l.AddLast(new FileInfo(f)).List);
                }
                else
                    throw new FileNotFoundException(System.IO.Path.GetFullPath(this.Path));


                Bundle bundle = new Bundle(this.IgnoreVersionComponent);

                TraceSources.TemplateSource.TraceInformation("Merging LostDoc files into bundle.");

                foreach (FileInfo file in includedFiles)
                {
                    TraceSources.TemplateSource.TraceEvent(TraceEventType.Information, 0, "Source: {0}", file.Name);
                    XDocument fileDoc = XDocument.Load(file.FullName);

                    bundle.Add(fileDoc);
                }


                // find template
                string appDir = Assembly.GetExecutingAssembly().Location;
                string cwDir = Directory.GetCurrentDirectory();


                IReadOnlyFileProvider fsProvider = new DirectoryFileProvider();
                IReadOnlyFileProvider resourceProvider = new ResourceFileProvider("LBi.LostDoc.ConsoleApplication.Templates");

                TemplateResolver templateResolver = new TemplateResolver(fsProvider, resourceProvider);

                Template template = new Template();
                template.Load(templateResolver, this.Template);

                string outputDir = this.Output
                                   ?? (Directory.Exists(this.Path)
                                           ? this.Path
                                           : System.IO.Path.GetDirectoryName(this.Path));
                AssetRedirectCollection assetRedirects;
                XDocument mergedDoc = bundle.Merge(out assetRedirects);

                var templateData = new TemplateData(mergedDoc)
                                       {
                                           AssetRedirects = assetRedirects,
                                           OverwriteExistingFiles = this.Force.IsPresent,
                                           IgnoredVersionComponent = this.IgnoreVersionComponent,
                                           Arguments = this.Arguments,
                                           TargetDirectory = outputDir
                                       };

                template.Generate(templateData);
            }
            finally
            {
                TraceSources.TemplateSource.Listeners.Remove(traceListener);
                TraceSources.AssetResolverSource.Listeners.Remove(traceListener);
            }
        }


        #endregion
    }
}
