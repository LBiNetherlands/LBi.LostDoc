using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using PSArgs;

namespace LBi.LostDoc.ConsoleApplication
{
    internal class Program
    {
        /// <summary>
        /// Mains the specified args.
        /// </summary>
        /// <param name="args">
        /// The args. 
        /// </param>
        private static void Main(string[] args)
        {
            using (AggregateCatalog aggregateCatalog = new AggregateCatalog())
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    aggregateCatalog.Catalogs.Add(new AssemblyCatalog(asm));
                }

                string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
                string pluginPath = Path.Combine(appPath, "plugins");
                if (Directory.Exists(pluginPath))
                    aggregateCatalog.Catalogs.Add(new DirectoryCatalog(pluginPath));

                using (CompositionContainer container = new CompositionContainer(aggregateCatalog))
                {
                    ICommand[] commands = container.GetExports<ICommand>().Select(l => l.Value).ToArray();
                    if (args.Length > 0)
                    {
                        ICommand[] filteredCommands =
                            commands.Where(
                                           c =>
                                           c.Name.Any(n => n.StartsWith(args[0], StringComparison.OrdinalIgnoreCase)))
                                .ToArray();

                        if (filteredCommands.Length == 1)
                        {
                            ICommand command = filteredCommands[0];
                            ArgsSetter argParser = new ArgsSetter(args.Skip(1).ToArray());
                            argParser.SetParameters(command);
                            if (argParser.Errors.Any())
                            {
                                WriteSignature();
                                Console.WriteLine("Command: " +
                                                  AttributedModelServices.GetContractName(command.GetType()));
                                command.Usage(Console.Out);
                                Console.WriteLine();
                            }
                            else
                                command.Invoke();
                        }
                        else if (filteredCommands.Length > 1)
                        {
                            IEnumerable<string> matchingCommands =
                                filteredCommands.SelectMany(c => c.Name)
                                    .Where(n => n.StartsWith(args[0], StringComparison.OrdinalIgnoreCase));
                            WriteSignature();
                            Console.WriteLine("Ambiguous command: " + string.Join(", ", matchingCommands));
                        }
                        else
                        {
                            WriteSignature();
                            Console.WriteLine("Unknown command!");
                            foreach (ICommand cmd in commands)
                            {
                                Console.WriteLine("Command: " + cmd.Name.First());
                                cmd.Usage(Console.Out);
                                Console.WriteLine();
                            }
                        }
                    }
                    else
                    {
                        WriteSignature();
                        foreach (ICommand cmd in commands)
                        {
                            Console.WriteLine("Command: " + cmd.Name.First());
                            cmd.Usage(Console.Out);
                            Console.WriteLine();
                        }
                    }
                }
            }
        }

        private static void WriteSignature()
        {
            Console.WriteLine("LBi LostDoc (Version: " + Assembly.GetExecutingAssembly().GetName().Version + ")");
            Console.WriteLine("Usage: lostdoc.exe [command] [args]");
        }
    }
}