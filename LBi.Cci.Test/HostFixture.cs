using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Cci;

namespace LBi.Cci.Test
{
    public class HostFixture : IDisposable
    {
        public HostFixture()
        {
            this.Assemblies = new List<IAssembly>();
            this.Host = new PeReader.DefaultHost();
            this.Assemblies.Add(this.LoadAssembly(@"Company.Project.Library.dll"));
            this.Assemblies.Add(this.LoadAssembly(@"Company.Project.AnotherLibrary.dll"));

            List<IAssembly> referencedAssemblies = new List<IAssembly>();
            foreach (IAssembly assembly in this.Assemblies)
            {
                foreach (IAssemblyReference assemblyReference in assembly.AssemblyReferences)
                {
                    var resolvedIdentity = this.Host.ProbeAssemblyReference(assembly, assemblyReference.AssemblyIdentity);
                    if (this.Host.FindAssembly(resolvedIdentity) is Dummy)
                    {
                        IAssembly referencedAssembly = this.Host.LoadAssembly(resolvedIdentity);
                        if (referencedAssembly is Dummy)
                            throw new InvalidOperationException("Dummy assembly found");
                        referencedAssemblies.Add(referencedAssembly);
                    }
                }
            }

            this.Assemblies.AddRange(referencedAssemblies);
        }

        public List<IAssembly> Assemblies { get; set; }

        public ITypeDefinition Convert(Type type)
        {
            IAssembly asm = this.Assemblies.Single(a => UnitHelper.StrongName((IAssemblyReference)a) == type.Assembly.FullName);

            INamedTypeDefinition typeDef;
            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();

            if (type.IsGenericTypeDefinition)
                typeDef = UnitHelper.FindType(this.Host.NameTable, asm, type.FullName.Substring(0, type.FullName.LastIndexOf('`')), type.GetGenericArguments().Length);
            else
                typeDef = UnitHelper.FindType(this.Host.NameTable, asm, type.FullName);

            return typeDef;
        }

        public ITypeDefinitionMember Convert(MethodInfo addMethod)
        {
            ITypeDefinition typeDef = Convert(addMethod.ReflectedType);

            return typeDef.GetMatchingMembers(m => m.Name.Value.Equals(addMethod.Name)).Single();
        }

        private IAssembly LoadAssembly(string asmPath)
        {
            System.Reflection.AssemblyName asmName = System.Reflection.AssemblyName.GetAssemblyName(asmPath);
            AssemblyIdentity asmId = UnitHelper.GetAssemblyIdentity(asmName, this.Host);
            AssemblyIdentity realAsmId = new AssemblyIdentity(asmId, asmPath);
            IAssembly asm = this.Host.LoadAssembly(realAsmId);
            return asm;
        }

        public MetadataReaderHost Host { get; set; }

        public void Dispose()
        {
            this.Host.Dispose();
        }

        
    }
}