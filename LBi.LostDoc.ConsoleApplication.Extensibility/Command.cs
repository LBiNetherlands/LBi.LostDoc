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

using System.ComponentModel.Composition.Hosting;
using LBi.Cli.Arguments;

namespace LBi.LostDoc.ConsoleApplication.Extensibility
{
    public abstract class Command : ICommand
    {
        [Parameter(HelpMessage = "Include errors and warning output only.")]
        public LBi.Cli.Arguments.Switch Quiet { get; set; }

        [Parameter(HelpMessage = "Include verbose output.")]
        public LBi.Cli.Arguments.Switch Verbose { get; set; }

        public abstract void Invoke(CompositionContainer container);
    }
}