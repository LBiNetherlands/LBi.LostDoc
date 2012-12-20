using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using LBi.Cli.Arguments;
using LBi.LostDoc.Core;

namespace LBi.LostDoc.ConsoleApplication
{
    [ParameterSet("Extract & Template", HelpMessage = "Generates documentation for a set of assemblies.")]
    public class ExtractAndTemplateCommand : ICommand
    {
        [Parameter(HelpMessage = "Template name"), Required]
        public string Template { get; set; }

        [Parameter(HelpMessage = "Assemblies"), Required]
        public string[] Path { get; set; }

        [Parameter(HelpMessage = "Output directory")]
        public string Output{ get; set; }

        [Parameter(HelpMessage = "Optional template arguments.")]
        [DefaultValue("@{}")]
        public Dictionary<string, object> Arguments { get; set; }

        [Parameter(HelpMessage = "Include non-public members.")]
        public LBi.Cli.Arguments.Switch IncludeNonPublic { get; set; }

        [Parameter(HelpMessage = "Includes doc comments from the BCL for referenced types.")]
        public LBi.Cli.Arguments.Switch IncludeBclDocComments { get; set; }

        [Parameter(HelpMessage = "Which version components to ignore for deduplication.")]
        public VersionComponent? IgnoreVersionComponent { get; set; }

        [Parameter(HelpMessage = "Path to xml containing additional comments for Assembly and Namespaces.")]
        public string NamespaceDocPath { get; set; }

        [Parameter(HelpMessage = "Type name filter (Compared against the type's FullName, including Namespace).")]
        public string Filter { get; set; }

        [Parameter(HelpMessage = "Include verbose output.")]
        public Switch Verbose { get; set; }

        public void Invoke()
        {
            // this is very quick & dirty
            List<string> ldocFiles = new List<string>();

            foreach (var path in this.Path)
            {
                ExtractCommand extract = new ExtractCommand();
                extract.IncludeNonPublic = this.IncludeNonPublic;
                extract.Filter = this.Filter;
                extract.IncludeBclDocComments = this.IncludeBclDocComments;
                extract.NamespaceDocPath = this.NamespaceDocPath;
                extract.Path = path;
                extract.Invoke();
                ldocFiles.Add(extract.Output);
            }

            string tempFolder = System.IO.Path.GetTempPath();
            tempFolder = System.IO.Path.Combine(tempFolder, "ldoc_{yyyyMMddHHmmss}");
            var tempDir = Directory.CreateDirectory(tempFolder);
            try
            {
                foreach (var ldocFile in ldocFiles)
                {
                    File.Copy(ldocFile, System.IO.Path.Combine(tempFolder, System.IO.Path.GetFileName(ldocFile)));
                }

                TemplateCommand template = new TemplateCommand();
                template.Path = tempFolder;
                template.Arguments = this.Arguments;
                template.IgnoreVersionComponent = this.IgnoreVersionComponent;
                template.Output = this.Output;
                template.Template = this.Template;
                template.Verbose = this.Verbose;
                template.Invoke();
            }
            finally
            {
                // cleanup
                tempDir.Delete(true);
            }
        }
    }
}