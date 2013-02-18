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
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using LBi.Cli.Arguments;
using LBi.LostDoc.ConsoleApplication.Extensibility;

namespace LBi.LostDoc.ConsoleApplication
{
    [ParameterSet("Save template", Command = "Export", HelpMessage = "Saves an embedded template to disk.")]
    public class SaveTemplateCommand : ICommand
    {
        [Parameter(HelpMessage = "Output path."), Required]
        public string Path { get; set; }

        [Parameter(HelpMessage = "Name of template to export."), Required]
        public string Template { get; set; }

        #region ICommand Members


        public void Invoke(CompositionContainer container)
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


        #endregion
    }
}
