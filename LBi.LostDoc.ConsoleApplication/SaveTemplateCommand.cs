using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using PSArgs;

namespace LBi.LostDoc.ConsoleApplication
{
    [Export(typeof(ICommand))]
    public class SaveTemplateCommand : ICommand
    {
        [Parameter(Mandatory = true)]
        public string Path { get; set; }

        [Parameter(Mandatory = true)]
        public string Template { get; set; }

        #region ICommand Members

        public string[] Name
        {
            get { return new[] {"Export-Template"}; }
        }

        public void Invoke()
        {
            var traceListener = new ConsolidatedConsoleTraceListener(new Dictionary<string, string>());

            string basePath = this.GetType().Namespace + ".Templates." + this.Template;
            Assembly asm = Assembly.GetExecutingAssembly();
            string[] names = asm.GetManifestResourceNames();

            foreach (string name in names)
            {
                if (!name.StartsWith(basePath))
                    continue;


                string outputPath =
                    System.IO.Path.GetFullPath(
                                               System.IO.Path.Combine(
                                                                      this.Path, name.Substring(basePath.Length + 1)));
                string outputDir = System.IO.Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);
                using (Stream inputStream = asm.GetManifestResourceStream(name))
                using (Stream fileStream = File.OpenWrite(outputPath))
                    inputStream.CopyTo(fileStream);
            }
        }

        public void Usage(TextWriter output)
        {
            output.WriteLine(
                             @"Parameters:
Template                Name of template to export.
Path                    Output path.
");
        }

        #endregion
    }
}