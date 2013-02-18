/*
 * Copyright 2012,2013 LBi Netherlands B.V.
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
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using LBi.Cli.Arguments;
using LBi.LostDoc.ConsoleApplication.Extensibility;

namespace LBi.LostDoc.ConsoleApplication
{
    public class Program
    {
        /// <summary>
        /// Mains the specified args.
        /// </summary>
        /// <param name="args">
        /// The args. 
        /// </param>
        public static void Main(string[] args)
        {
            WriteSignature();

            using (AggregateCatalog aggregateCatalog = new AggregateCatalog())
            {
                aggregateCatalog.Catalogs.Add(new TypeCatalog(typeof(DefaultCommandProvider)));

                string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                aggregateCatalog.Catalogs.Add(new DirectoryCatalog(appPath, "*.dll"));
                string pluginPath = Path.Combine(appPath, "plugins");
                if (Directory.Exists(pluginPath))
                    aggregateCatalog.Catalogs.Add(new DirectoryCatalog(pluginPath));

                using (CompositionContainer container = new CompositionContainer(aggregateCatalog))
                {
                    ICommandProvider[] providers = container.GetExports<ICommandProvider>().Select(l => l.Value).ToArray();
                    var commands = providers.SelectMany(p => p.GetCommands()).ToArray();

                    ArgumentParser<ICommand> argumentParser = new ArgumentParser<ICommand>(commands);
                    ICommand command;
                    if (argumentParser.TryParse(args, out command))
                    {
                        command.Invoke(container);
                    }
                }
            }
        }

        private static void WriteSignature()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var prodAttr = executingAssembly.GetCustomAttribute<AssemblyProductAttribute>();
            Console.WriteLine("{0} (Version: {1})", prodAttr.Product, executingAssembly.GetName().Version);
            Console.WriteLine();
        }
    }
}
