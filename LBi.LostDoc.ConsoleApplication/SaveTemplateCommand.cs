/*
 * Copyright 2012-2013 LBi Netherlands B.V.
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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using LBi.Cli.Arguments;
using LBi.LostDoc.ConsoleApplication.Extensibility;
using LBi.LostDoc.Templating;

namespace LBi.LostDoc.ConsoleApplication
{
    [ParameterSet("Save template", Command = "Export", HelpMessage = "Saves a template to disk.")]
    public class SaveTemplateCommand : ICommand
    {
        [ImportingConstructor]
        public SaveTemplateCommand(TemplateResolver templateResolver)
        {
            this.TemplateResolver = templateResolver;
        }

        [Parameter(HelpMessage = "Output path."), Required]
        public string Path { get; set; }

        [Parameter(HelpMessage = "Name of template to export."), Required]
        public string Template { get; set; }

        [Parameter(HelpMessage = "Name of template to export.")]
        [DefaultValue("$true")]
        public bool IncludeInherited { get; set; }

        protected TemplateResolver TemplateResolver { get; set; }
        #region ICommand Members


        public void Invoke(CompositionContainer container)
        {
            TemplateInfo templateInfo;
            if (!this.TemplateResolver.TryResolve(this.Template, out templateInfo))
                Console.WriteLine("Template not found: '{0}'.", this.Template);

            do
            {
                foreach (string filename in templateInfo.GetFiles())
                {
                    Console.WriteLine(filename);
                    string targetPath = System.IO.Path.Combine(this.Path, filename);
                    string targetDir = System.IO.Path.GetDirectoryName(targetPath);
                    Directory.CreateDirectory(targetDir);
                    using (var target = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    using (var content = templateInfo.Source.OpenFile(filename, FileMode.Open))
                    {
                        content.CopyTo(target);
                        content.Close();
                        target.Close();
                    }
                }

                templateInfo = templateInfo.Inherits;
            } while (this.IncludeInherited && templateInfo != null);
        }


        #endregion
    }
}
