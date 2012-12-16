using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using LBi.LostDoc.Core;
using LBi.LostDoc.Core.Diagnostics;
using LBi.LostDoc.Core.Enrichers;
using LBi.LostDoc.Core.Filters;
using PSArgs;

namespace LBi.LostDoc.ConsoleApplication
{
    [Export(typeof(ICommand))]
    public class ExtractCommand : ICommand
    {
        [Parameter]
        public bool? Verbose { get; set; }

        [Parameter]
        public bool? IncludeNonPublic { get; set; }

        [Parameter]
        public bool? IncludeBclDocComments { get; set; }

        [Parameter(Mandatory = true)]
        public string Path { get; set; }

        [Parameter]
        public string NamespaceDocPath { get; set; }

        [Parameter]
        public string Filter { get; set; }

        [Parameter]
        public string Output { get; set; }

        #region ICommand Members

        public string[] Name
        {
            get { return new[] {"Extract"}; }
        }

        public void Invoke()
        {
            var traceListener = new ConsolidatedConsoleTraceListener(new Dictionary<string, string>
                                                                         {
                                                                             {"LostDoc.Core.DocGenerator", "Build"},
                                                                         });

            TraceSources.GeneratorSource.Listeners.Add(traceListener);

            if (!this.Verbose.HasValue || !this.Verbose.Value)
                TraceSources.GeneratorSource.Switch.Level = SourceLevels.Information | SourceLevels.ActivityTracing;

            DocGenerator gen = new DocGenerator();

            gen.AssetFilters.Add(new CompilerGeneratedFilter());
            if (!this.IncludeNonPublic.HasValue || !this.IncludeNonPublic.Value)
                gen.AssetFilters.Add(new PublicTypeFilter());

            gen.AssetFilters.Add(new CompilerGeneratedFilter());
            gen.AssetFilters.Add(new LogicalMemberInfoVisibilityFilter());
            gen.AssetFilters.Add(new SpecialNameMemberInfoFilter());

            if (!string.IsNullOrWhiteSpace(this.Filter))
                gen.AssetFilters.Add(new TypeNameGlobFilter {Include = this.Filter});

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
                Console.WriteLine("Path not found: '{0}'", this.Path);
                return;
            }

            if (this.IncludeBclDocComments.HasValue && this.IncludeBclDocComments.Value)
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

            Assembly assembly = Assembly.ReflectionOnlyLoadFrom(this.Path);

            XDocument rawDoc = gen.Generate();
            string fileName = System.IO.Path.Combine(this.Output ?? System.IO.Path.GetDirectoryName(this.Path),
                                                     string.Format("{0}_{1}.ldoc",
                                                                   System.IO.Path.GetFileName(this.Path),
                                                                   assembly.GetName().Version));

            if (!Directory.Exists(System.IO.Path.GetDirectoryName(fileName)))
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fileName));

            rawDoc.Save(fileName);
        }

        public void Usage(TextWriter output)
        {
            output.WriteLine(
                             @"Parameters:
Path                    Path to assembly to extract.
Output                  Output path.
IncludeNonPublic        Includes non public types.
Filter                  Type name filter (Compared against the type's FullName, including Namespace).
                        Example: -Filter Company.Library.Project.*
IncludeBclDocComments   Includes doc comments from the BCL for referenced types.
NamespaceDocPath        Path to xml containing additional comments for Assembly and Namespaces.
Verbose                 Enable verbose output.
");
        }

        #endregion
    }
}