using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Company.Project.AnotherLibrary;
using Company.Project.Library;
using Microsoft.Cci;
using Xunit;

namespace LBi.Cci.Test
{

    public class NamingTests : IUseFixture<HostFixture>
    {
        [Fact]
        public void TypeNaming()
        {
            //PeReader.DefaultHost host = new PeReader.DefaultHost();
            ////string asmPath = @"C:\src\lbi\LBi.Cli.Arguments\LBi.Cli.Arguments\bin\Debug\LBi.Cli.Arguments.dll";
            //string asmPath = @"Company.Project.AnotherLibrary.dll";
            //AssemblyName asmName = System.Reflection.AssemblyName.GetAssemblyName(asmPath);
            //AssemblyIdentity asmId = UnitHelper.GetAssemblyIdentity(asmName, host);
            //AssemblyIdentity realAsmId = new AssemblyIdentity(asmId, asmPath);
            //IAssembly asm = host.LoadAssembly(realAsmId);
            //INamedTypeDefinition type = UnitHelper.FindType(host.NameTable, asm, "Company.Project.AnotherLibrary.SpecialClass");

            Assert.Equal("T:Company.Project.AnotherLibrary.SpecialClass", Naming.GetAssetId(this._data.Convert(typeof(SpecialClass))));
        }

        //[Fact]
        //public void MethodOnGenericClass()
        //{
        //    Assert.Equal("M:Company.Project.AnotherLibrary.SpecialClass.ReturnsGeneric",
        //                 Naming.GetAssetId(typeof(SpecialClass).GetMethod("ReturnsGeneric")));
        //}

        //[Fact]
        //public void GenericMethodOnOpenGenericType_WithGenericParameter()
        //{
        //    Assert.Equal(
        //                 "M:Company.Project.Library.GenericClass`1.AnotherGeneric``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IEnumerable{`0},System.Collections.Generic.IEnumerable{System.Int32})",
        //                 Naming.GetAssetId(typeof(GenericClass<>).GetMethod("AnotherGeneric")));
        //}

        //[Fact]
        //public void InheritedGenericMethodOnGenericClass_WithGenericParameter()
        //{
        //    Assert.Equal(
        //                 "M:Company.Project.AnotherLibrary.SpecialClass.AnotherGeneric``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IEnumerable{Company.Project.Library.RegularClass},System.Collections.Generic.IEnumerable{System.Int32})",
        //                 Naming.GetAssetId(typeof(SpecialClass).GetMethod("AnotherGeneric")));
        //}


        //[Fact(Skip = "Generic params no longer supported in Naming")]
        //public void OverriddenGenericMethod__WithGenericParameter()
        //{
        //    Assert.Equal(
        //                 "M:Company.Project.AnotherLibrary.InheritsSpecialClass.AnotherGeneric``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IEnumerable{Company.Project.Library.RegularClass},System.Collections.Generic.IEnumerable{System.Int32})",
        //                 Naming.GetAssetId(typeof(InheritsSpecialClass).GetMethod("AnotherGeneric")));
        //}

        [Fact]
        public void ClosedGenericType()
        {
            Assert.Equal("T:System.Collections.Generic.List`1",
                         Naming.GetAssetId(this._data.Convert(typeof(List<string>))));
        }

        [Fact]
        public void OpenGenericType()
        {
            Assert.Equal("T:System.Collections.Generic.List`1",
                         Naming.GetAssetId(this._data.Convert(typeof(List<>))));
        }

        //[Fact]
        //public void ConversionOperator()
        //{
        //    Assert.Equal(
        //                 "M:Company.Project.Library.RegularClass.op_Explicit(Company.Project.Library.RegularClass)~System.Int64",
        //                 Naming.GetAssetId(
        //                                   typeof(RegularClass).GetMethods(BindingFlags.Static | BindingFlags.Public)
        //                                       .First(p => p.Name == "op_Explicit")));
        //}

        [Fact]
        public void NestedOpenGenericType()
        {
            string aid = Naming.GetAssetId(this._data.Convert(typeof(GenericClass<>.NestedGeneric<>)));
            Assert.Equal("T:Company.Project.Library.GenericClass`1.NestedGeneric`1", aid);
        }


        [Fact]
        public void NestedClosedGenericType()
        {
            string aid = Naming.GetAssetId(this._data.Convert(typeof(GenericClass<string>.NestedGeneric<int>)));
            Assert.Equal("T:Company.Project.Library.GenericClass`1.NestedGeneric`1", aid);
        }


        //[Fact]
        //public void MethodOnNestedClosedGenericType()
        //{
        //    string aid =
        //        Naming.GetAssetId(
        //                          typeof(GenericClass<string>.NestedGeneric<int>).GetMethods().First(
        //                                                                                             m =>
        //                                                                                             m.Name.StartsWith
        //                                                                                                 ("ConsumeP")));
        //    Assert.Equal("M:Company.Project.Library.GenericClass`1.NestedGeneric`1.ConsumeP(System.Int32)", aid);
        //}


        //[Fact]
        //public void OverriddenGenericMethod()
        //{
        //    MethodInfo method = typeof(SpecialClass).GetMethods().First(m => m.Name == "ConsumesGeneric");

        //    Assert.Equal(
        //                 "M:Company.Project.AnotherLibrary.SpecialClass.ConsumesGeneric(Company.Project.Library.RegularClass)",
        //                 Naming.GetAssetId(method));
        //}

        public void SetFixture(HostFixture data)
        {
            this._data = data;
        }

        private HostFixture _data;
    }

    public class HostFixture : IDisposable
    {
        public HostFixture()
        {
            this.Assemblies = new List<IAssembly>();
            this.Host = new PeReader.DefaultHost();
            this.Assemblies.Add(this.LoadAssembly(@"Company.Project.Library.dll"));
            this.Assemblies.Add(this.LoadAssembly(@"Company.Project.AnotherLibrary.dll"));

            List<IAssembly> referencedAssemblies = new List<IAssembly>();
            foreach (IAssembly assembly in Assemblies)
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
            IAssembly asm = this.Assemblies.Single(a => UnitHelper.StrongName(a) == type.Assembly.FullName);

            INamedTypeDefinition typeDef;
            if (type.IsGenericTypeDefinition)
                typeDef = UnitHelper.FindType(this.Host.NameTable, asm, type.FullName.Substring(0, type.FullName.LastIndexOf('`')), type.GetGenericArguments().Length);
            else
                typeDef = UnitHelper.FindType(this.Host.NameTable, asm, type.FullName);

            return typeDef;
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
