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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using LBi.Cli.Arguments;
using LBi.LostDoc.Core;
using LBi.LostDoc.Core.Diagnostics;
using LBi.LostDoc.Core.Templating;

namespace LBi.LostDoc.ConsoleApplication
{
    [ParameterSet("Template", Command = "Template", HelpMessage = "Apply template to a set of ldoc files to generate output.")]
    public class TemplateCommand : ICommand
    {
        [Parameter(HelpMessage = "Include verbose output.")]
        public LBi.Cli.Arguments.Switch Verbose { get; set; }

        [Parameter(HelpMessage = "Path to ldoc file (or folder containing multiple ldoc files)."), Required]
        public string Path { get; set; }

        [Parameter(HelpMessage = "Template name or path"), Required]
        public string Template { get; set; }

        [Parameter(HelpMessage = "Output path.")]
        public string Output { get; set; }

        [Parameter(HelpMessage = "Which version components to ignore for deduplication.")]
        public VersionComponent? IgnoreVersionComponent { get; set; }

        #region ICommand Members

        public void Invoke()
        {
            var traceListener = new ConsolidatedConsoleTraceListener(new Dictionary<string, string>
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

            if (!this.Verbose.IsPresent)
            {
                TraceSources.TemplateSource.Switch.Level = SourceLevels.Information | SourceLevels.ActivityTracing;
                TraceSources.AssetResolverSource.Switch.Level = SourceLevels.Information | SourceLevels.ActivityTracing;
            }


            LinkedList<FileInfo> includedFiles = new LinkedList<FileInfo>();

            if (File.Exists(this.Path))
                includedFiles.AddLast(new FileInfo(this.Path));
            else if (Directory.Exists(this.Path))
                Directory.GetFiles(this.Path, "*.ldoc", SearchOption.AllDirectories)
                    .Aggregate(includedFiles,
                               (l, f) => l.AddLast(new FileInfo(f)).List);
            else
                throw new FileNotFoundException(System.IO.Path.GetFullPath(this.Path));


            Bundle bundle = new Bundle(this.IgnoreVersionComponent);

            TraceSources.TemplateSource.TraceInformation("Merging LostDoc files into bundle.");

            foreach (FileInfo file in includedFiles)
            {
                TraceSources.TemplateSource.TraceEvent(TraceEventType.Information, 0, "Path: {0}", file.Name);
                XDocument fileDoc = XDocument.Load(file.FullName);

                bundle.Add(fileDoc);
            }


            // find template
            string appDir = Assembly.GetExecutingAssembly().Location;
            string cwDir = Directory.GetCurrentDirectory();


            IFileProvider fsProvider = new DirectoryFileProvider();
            IFileProvider resourceProvider = new ResourceFileProvider("LBi.LostDoc.ConsoleApplication.Templates");

            IFileProvider selectedFileProvider = null;
            string templatePath = null;

            if (System.IO.Path.IsPathRooted(this.Template) &&
                fsProvider.FileExists(System.IO.Path.Combine(this.Template, "template.xml")))
            {
                selectedFileProvider = fsProvider;
                templatePath = this.Template;
            }
            else if (!System.IO.Path.IsPathRooted(this.Template))
            {
                string tmp = System.IO.Path.Combine(cwDir, this.Template, "template.xml");
                if (fsProvider.FileExists(tmp))
                {
                    selectedFileProvider = fsProvider;
                    templatePath = tmp;
                }
                else
                {
                    tmp = System.IO.Path.Combine(appDir, this.Template, "template.xml");
                    if (fsProvider.FileExists(tmp))
                    {
                        selectedFileProvider = fsProvider;
                        templatePath = tmp;
                    }
                    else
                    {
                        tmp = System.IO.Path.Combine(this.Template, "template.xml");
                        if (resourceProvider.FileExists(tmp))
                        {
                            selectedFileProvider = resourceProvider;
                            templatePath = tmp;
                        }
                    }
                }
            }

            if (templatePath == null)
                throw new FileNotFoundException(this.Template);

            string outputDir = this.Output
                               ?? (Directory.Exists(this.Path)
                                       ? this.Path
                                       : System.IO.Path.GetDirectoryName(this.Path));

            Template template = new Template(selectedFileProvider);

            template.Load(templatePath);
            AssetRedirectCollection assetRedirects;
            XDocument mergedDoc = bundle.Merge(out assetRedirects);
            var templateData = new TemplateData
                                   {
                                       AssetRedirects = assetRedirects,
                                       Document = mergedDoc,
                                       IgnoredVersionComponent = this.IgnoreVersionComponent
                                   };
            template.Generate(templateData, outputDir);
        }


        #endregion
    }
}
