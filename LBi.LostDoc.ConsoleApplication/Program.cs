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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.Registration;
using System.IO;
using System.Linq;
using System.Reflection;
using LBi.Cli.Arguments;
using LBi.Cli.Arguments.Binding;
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
                RegistrationBuilder registrationBuilder = new RegistrationBuilder();

                registrationBuilder.ForTypesDerivedFrom<ICommand>()
                                   .Export(conf => conf.AsContractName(AttributedModelServices.GetContractName(typeof(ICommand))))
                                   .SetCreationPolicy(CreationPolicy.NonShared);
                
                aggregateCatalog.Catalogs.Add(new ApplicationCatalog(registrationBuilder));

                string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string pluginPath = Path.Combine(appPath, "plugins");
                if (Directory.Exists(pluginPath))
                    aggregateCatalog.Catalogs.Add(new DirectoryCatalog(pluginPath, registrationBuilder));

                using (CompositionContainer container = new CompositionContainer(aggregateCatalog))
                {
                    ICommandProvider[] providers = container.GetExports<ICommandProvider>().Select(l => l.Value).ToArray();
                    Type[] commands = providers.SelectMany(p => p.GetCommands()).ToArray();

                    Func<Type, object> mefActivator =
                        t =>
                        {
                            if (!typeof(ICommand).IsAssignableFrom(t))
                                return DefaultActivator.Instance.CreateInstance(t);

                            ImportDefinition importDefinition = new ImportDefinition(ed => (string)ed.Metadata[CompositionConstants.ExportTypeIdentityMetadataName] == AttributedModelServices.GetTypeIdentity(t),
                                                                                     AttributedModelServices.GetContractName(typeof(ICommand)),
                                                                                     ImportCardinality.ExactlyOne,
                                                                                     false,
                                                                                     true);

                            return container.GetExports(importDefinition).First().Value;
                        };

                    ArgumentParserSettings parserSettings = new ArgumentParserSettings
                                                            {
                                                                TypeActivator = new DelegateActivator(mefActivator)
                                                            };

                    ArgumentParser<ICommand> argumentParser = new ArgumentParser<ICommand>(parserSettings, commands);
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
