using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LBi.LostDoc.ConsoleApplication.SingleAssembly
{
    class Program
    {
        static void Main(string[] args)
        {
            const string embeddedPrefix = "LBi.LostDoc.ConsoleApplication.SingleAssembly.EmbeddedAssemblies.";

            AppDomain.CurrentDomain.AssemblyResolve +=
                (sender, arg) =>
                {
                    String resourceName = embeddedPrefix +
                                          new AssemblyName(arg.Name).Name +
                                          ".dll";

                    Console.WriteLine("Loading: " + resourceName);

                    return LoadEmbeddedAssembly(resourceName);
                };

            Assembly lostdocAssembly = null;
            IEnumerable<string> names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            names = names.Where(n => n.StartsWith(embeddedPrefix, StringComparison.Ordinal));
            foreach (string name in names)
            {
                if (name.EndsWith(".dll") || name.EndsWith(".exe"))
                {
                    var asm = LoadEmbeddedAssembly(name);
                    if (name.EndsWith("lostdoc.exe", StringComparison.Ordinal))
                        lostdocAssembly = asm;
                }
            }

            MethodInfo entryPoint = lostdocAssembly.EntryPoint;

            entryPoint.Invoke(null, new object[] { args });
        }

        private static Assembly LoadEmbeddedAssembly(string resourceName)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;

                Byte[] assemblyData = new Byte[stream.Length];

                stream.Read(assemblyData, 0, assemblyData.Length);

                return Assembly.Load(assemblyData);
            }
        }
    }
}
