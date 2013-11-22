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

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using LBi.Cli.Arguments;
using LBi.LostDoc.Composition;
using LBi.LostDoc.ConsoleApplication.Extensibility;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Extensibility;
using LBi.LostDoc.Templating;
using LBi.LostDoc.Templating.FileProviders;

namespace LBi.LostDoc.ConsoleApplication
{
    [ParameterSet("Template", Command = "Template", HelpMessage = "Apply template to a set of ldoc files to generate output.")]
    public class TemplateCommand : Command
    {
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

        public override void Invoke(CompositionContainer container)
        {
            var traceListener = new ConsolidatedConsoleTraceListener
                                {
                                    { TraceSources.TemplateSource, "Template" },
                                    { TraceSources.BundleSource, "Bundle" },
                                    { TraceSources.AssetResolverSource, "Resolve" }
                                };

            using (traceListener)
            {
                this.ConfigureTraceLevels(traceListener);

                LinkedList<FileInfo> includedFiles = new LinkedList<FileInfo>();

                if (File.Exists(this.Path))
                    includedFiles.AddLast(new FileInfo(this.Path));
                else if (Directory.Exists(this.Path))
                {
                    Directory.GetFiles(this.Path, "*.ldoc", SearchOption.AllDirectories)
                             .Aggregate(includedFiles, (l, f) => l.AddLast(new FileInfo(f)).List);
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

                var lazyProviders = container.GetExports<IFileProvider>(ContractNames.TemplateProvider);
                var realProviders = lazyProviders.Select(lazy => lazy.Value);
                TemplateResolver templateResolver = new TemplateResolver(realProviders.ToArray());
                TemplateInfo templateInfo = templateResolver.Resolve(this.Template);
                Template template = templateInfo.Load(container);

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
                                           OutputFileProvider = new ScopedFileProvider(new DirectoryFileProvider(), outputDir)
                                       };

                template.Generate(templateData);
            }

        }


        #endregion
    }
}
