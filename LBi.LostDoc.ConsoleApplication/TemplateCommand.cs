using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using LBi.LostDoc.Core;
using LBi.LostDoc.Core.Diagnostics;
using LBi.LostDoc.Core.Templating;
using PSArgs;

namespace LBi.LostDoc.ConsoleApplication
{
    [Export(typeof(ICommand))]
    public class TemplateCommand : ICommand
    {
        [Parameter]
        public bool? Verbose { get; set; }

        [Parameter(Mandatory = true)]
        public string Path { get; set; }

        [Parameter(Mandatory = true)]
        public string Template { get; set; }

        [Parameter]
        public string Output { get; set; }

        [Parameter]
        public VersionComponent? IgnoreVersionComponent { get; set; }

        #region ICommand Members

        public string[] Name
        {
            get { return new[] {"Template"}; }
        }

        public void Invoke()
        {
            var traceListener = new ConsolidatedConsoleTraceListener(new Dictionary<string, string>
                                                                         {
                                                                             {"LostDoc.Core.Template", "Template"},
                                                                             {
                                                                                 "LostDoc.Core.Template.AssetResolver",
                                                                                 "Resolve"
                                                                                 }
                                                                         });

            TraceSources.TemplateSource.Listeners.Add(traceListener);
            TraceSources.AssetResolveSource.Listeners.Add(traceListener);

            if (!this.Verbose.HasValue || !this.Verbose.Value)
            {
                TraceSources.TemplateSource.Switch.Level = SourceLevels.Information | SourceLevels.ActivityTracing;
                TraceSources.AssetResolveSource.Switch.Level = SourceLevels.Information | SourceLevels.ActivityTracing;
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

        public void Usage(TextWriter output)
        {
            output.WriteLine(
                             @"Parameters:
Path                    Path to ldoc file (or folder containing multiple ldoc files).
Output                  Output path.
Template                Selected template.
Verbose                 Enable verbose output.
");
        }

        #endregion
    }
}