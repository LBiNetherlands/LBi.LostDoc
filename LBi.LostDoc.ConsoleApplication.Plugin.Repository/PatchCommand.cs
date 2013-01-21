/*
 * Copyright 2013 LBi Netherlands B.V.
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LBi.Cli.Arguments;

namespace LBi.LostDoc.ConsoleApplication.Plugin.Repository
{
    [ParameterSet("Patch", Command = "Patch", HelpMessage = "Patches a set of C# source files using an xml document comments file.")]
    public class PatchCommand : Command
    {
        [Parameter(HelpMessage = "C# source code directory.")]
        public DirectoryInfo Source { get; set; }

        [Parameter(HelpMessage = "Interactive patch.")]
        public Switch Interactive { get; set; }

        [Parameter(HelpMessage = "Input xml document comment file.")]
        public string XmlDocument { get; set; }

        public override void Invoke()
        {
            if (!this.Source.Exists)
                Console.WriteLine("Source dir '{0}' does not exist.", this.Source.FullName);

            
        }
    }
}
