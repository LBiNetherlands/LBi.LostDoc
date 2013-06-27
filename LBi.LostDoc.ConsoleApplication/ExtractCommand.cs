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
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using LBi.Cli.Arguments;
using LBi.LostDoc.ConsoleApplication.Extensibility;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Enrichers;
using LBi.LostDoc.Filters;

namespace LBi.LostDoc.ConsoleApplication
{
    [ParameterSet("Extract", Command = "Extract", HelpMessage = "Extracts metadata from an assembly to create a ldoc file.")]
    public class ExtractCommand : ICommand
    {
        [Parameter(HelpMessage = "Include errors and warning output only.")]
        public LBi.Cli.Arguments.Switch Quiet { get; set; }

        [Parameter(HelpMessage = "Include verbose output.")]
        public LBi.Cli.Arguments.Switch Verbose { get; set; }

        [Parameter(HelpMessage = "Include non-public members.")]
        public LBi.Cli.Arguments.Switch IncludeNonPublic { get; set; }

        [Parameter(HelpMessage = "Includes doc comments from the BCL for referenced types.")]
        public LBi.Cli.Arguments.Switch IncludeBclDocComments { get; set; }

        [Parameter(HelpMessage = "Source to assembly to extract."), Required]
        [ExampleValue("With filter", "\"c:\\projects\\Company.Library.Project.dll\"")]
        public string Path { get; set; }

        [Parameter(HelpMessage = "Source to xml containing additional comments for Assembly and Namespaces.")]
        public string NamespaceDocPath { get; set; }

        [Parameter(HelpMessage = "Type name filter (Compared against the type's FullName, including Namespace).")]
        [ExampleValue("With filter", "Company.Library.Project.*")]
        public string Filter { get; set; }

        [Parameter(HelpMessage = "Output path.")]
        public string Output { get; set; }

        #region ICommand Members

        public void Invoke(CompositionContainer container)
        {
            var traceListener = new ConsolidatedConsoleTraceListener(new Dictionary<string, string>
                                                                         {
                                                                             {"LostDoc.Core.DocGenerator", "Build"},
                                                                         });

            TraceSources.GeneratorSource.Listeners.Add(traceListener);

            try
            {
                SetTraceLevel();
                
                if (!File.Exists(this.Path))
                {
                    Console.WriteLine("File not found: '{0}'", this.Path);
                    return;
                }

                this.Output = BuildOutputFilePath();

                DocGenerator gen = new DocGenerator(container);

                gen.AssetFilters.AddRange(
                    BuildAssetFilters());

                gen.Enrichers.AddRange(
                    BuildEnrichers());

                gen.AddAssembly(this.Path);
                
                XDocument rawDoc = gen.Generate();

                
                //StringWriter output = new StringWriter();
                //try
                //{
                //    using (
                //        XmlWriter writer = XmlWriter.Create(output,
                //                                            new XmlWriterSettings
                //                                                {
                //                                                    CheckCharacters = true,
                //                                                    Encoding = Encoding.ASCII
                //                                                }))
                //        rawDoc.Save(writer);
                //}
                //catch
                //{
                    
                //}

                rawDoc.Save(this.Output);

            }
            finally
            {
                TraceSources.GeneratorSource.Listeners.Remove(traceListener);
            }
        }

        private string BuildOutputFilePath()
        {
            var assemblyVersion = AssemblyName.GetAssemblyName(this.Path).Version;


            string fileName = System.IO.Path.Combine(this.Output ?? System.IO.Path.GetDirectoryName(this.Path),
                                                     string.Format("{0}_{1}.ldoc",
                                                                   System.IO.Path.GetFileName(this.Path),
                                                                   assemblyVersion));

            string directoryName = System.IO.Path.GetDirectoryName(fileName);

            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);

            return System.IO.Path.GetFullPath(fileName);
        }

        #endregion

        private void SetTraceLevel()
        {
            if (this.Quiet.IsPresent)
            {
                const SourceLevels quietLevel = SourceLevels.Error | SourceLevels.Warning | SourceLevels.Critical;
                TraceSources.GeneratorSource.Switch.Level = quietLevel;
            }
            else if (this.Verbose.IsPresent)
            {
                const SourceLevels verboseLevel = SourceLevels.All;
                TraceSources.GeneratorSource.Switch.Level = verboseLevel;
            }
            else
            {
                const SourceLevels normalLevel = SourceLevels.Information |
                                                 SourceLevels.Warning |
                                                 SourceLevels.Error |
                                                 SourceLevels.Critical |
                                                 SourceLevels.ActivityTracing;
                TraceSources.GeneratorSource.Switch.Level = normalLevel;
            }
        }

        private IEnumerable<IAssetFilter> BuildAssetFilters()
        {
            //TODO: Move to Extensability container?
            //TODO: Confirm 
            var filters = new List<IAssetFilter>{
                new ComObjectTypeFilter(),
                new CompilerGeneratedFilter(),
                new PrivateImplementationDetailsFilter(),
                new DynamicallyInvokableAttributeFilter(),
                new CompilerGeneratedFilter(),
                new LogicalMemberInfoVisibilityFilter(),
                new SpecialNameMemberInfoFilter()
            };

            if (!this.IncludeNonPublic.IsPresent)
                filters.Add(new PublicTypeFilter());

            if (!string.IsNullOrWhiteSpace(this.Filter))
                filters.Add(new AssetGlobFilter { Include = this.Filter });

            return filters;
        }

        private IEnumerable<IEnricher> BuildEnrichers()
        {
            var enrichers = new List<IEnricher>();

            XmlDocEnricher docEnricher = new XmlDocEnricher();
            if (this.IncludeBclDocComments.IsPresent)
            {
                string winPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string bclDocPath = System.IO.Path.Combine(winPath, @"microsoft.net\framework\",
                                                           string.Format("v{0}.{1}.{2}",
                                                                         Environment.Version.Major,
                                                                         Environment.Version.Minor,
                                                                         Environment.Version.Build),
                                                           @"en\");


                docEnricher.AddPath(bclDocPath);

                bclDocPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    @"Reference Assemblies\Microsoft\Framework\.NETFramework",
                    string.Format("v{0}.{1}",
                                  Environment.Version.Major,
                                  Environment.Version.Minor));

                docEnricher.AddPath(bclDocPath);
            }

            enrichers.Add(docEnricher);

            if (!string.IsNullOrEmpty(this.NamespaceDocPath))
            {
                var namespaceEnricher = new ExternalNamespaceDocEnricher();
                if (System.IO.Path.IsPathRooted(this.NamespaceDocPath))
                    namespaceEnricher.Load(this.NamespaceDocPath);
                else if (
                    File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path),
                                                       this.NamespaceDocPath)))
                    namespaceEnricher.Load(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path),
                                                                  this.NamespaceDocPath));
                else
                    namespaceEnricher.Load(this.NamespaceDocPath);

                enrichers.Add(namespaceEnricher);
            }

            return enrichers;
        }
    }
}
