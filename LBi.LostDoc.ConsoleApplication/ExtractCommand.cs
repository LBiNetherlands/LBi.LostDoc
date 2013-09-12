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
    public class ExtractCommand : Command
    {
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

        public override void Invoke(CompositionContainer container)
        {
            var traceListener = new ConsolidatedConsoleTraceListener(new Dictionary<string, string>
                                                                         {
                                                                             {"LostDoc.Core.DocGenerator", "Build"},
                                                                         });
            try
            {
                TraceSources.GeneratorSource.Listeners.Add(traceListener);
                this.ConfigureTraceLevels(TraceSources.GeneratorSource);

                DocGenerator gen = new DocGenerator(container);
                gen.AssetFilters.Add(new ComObjectTypeFilter());
                gen.AssetFilters.Add(new CompilerGeneratedFilter());
                if (!this.IncludeNonPublic.IsPresent)
                    gen.AssetFilters.Add(new PublicTypeFilter());

                gen.AssetFilters.Add(new PrivateImplementationDetailsFilter());
                gen.AssetFilters.Add(new DynamicallyInvokableAttributeFilter());
                gen.AssetFilters.Add(new CompilerGeneratedFilter());
                gen.AssetFilters.Add(new LogicalMemberInfoVisibilityFilter());
                gen.AssetFilters.Add(new SpecialNameMemberInfoFilter());

                if (!string.IsNullOrWhiteSpace(this.Filter))
                    gen.AssetFilters.Add(new AssetGlobFilter { Include = this.Filter });

                XmlDocEnricher docEnricher = new XmlDocEnricher();
                gen.Enrichers.Add(docEnricher);


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

                    gen.Enrichers.Add(namespaceEnricher);
                }


                if (!File.Exists(this.Path))
                {
                    Console.WriteLine("File not found: '{0}'", this.Path);
                    return;
                }

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

                gen.AddAssembly(this.Path);


                var assemblyName = AssemblyName.GetAssemblyName(this.Path);

                XDocument rawDoc = gen.Generate();
                string fileName = System.IO.Path.Combine(this.Output ?? System.IO.Path.GetDirectoryName(this.Path),
                                                         string.Format("{0}_{1}.ldoc",
                                                                       System.IO.Path.GetFileName(this.Path),
                                                                       assemblyName.Version));

                this.Output = System.IO.Path.GetFullPath(fileName);

                if (!Directory.Exists(System.IO.Path.GetDirectoryName(fileName)))
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fileName));
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

                rawDoc.Save(fileName);

            }
            finally
            {
                TraceSources.GeneratorSource.Listeners.Remove(traceListener);
            }
        }

        #endregion
    }
}
