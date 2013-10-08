using System.Linq;
using System.Xml.Linq;
using Company.Project.Library;
using LBi.LostDoc.Cci;
using Microsoft.Cci;
using Xunit;

namespace LBi.LostDoc.Test
{
    public class DocGeneratorTests : HostTestBase
    {
        [Fact]
        public void SimpleTest()
        {
            using (CciDocGenerator docGenerator = new CciDocGenerator(new PeAssemblyLoader()))
            {
                docGenerator.AddAssembly("Company.Project.Library.dll");
                XDocument doc = docGenerator.Generate();
            }
        }
    }

    public class AssetHierarchyTests : HostTestBase
    {
        [Fact]
        public void FromAssembly()
        {
            var assembly = this.Fixture.Assemblies.First();

            AssetHierarchyCollector hierarchyCollector = new AssetHierarchyCollector();
            assembly.Dispatch(hierarchyCollector);
            Assert.NotEmpty(hierarchyCollector);
            Assert.Equal(new[] { assembly }, hierarchyCollector);
        }

        [Fact]
        public void FromType()
        {
            AssetHierarchyCollector hierarchyCollector = new AssetHierarchyCollector();
            this.Fixture.Convert(typeof(RegularClass)).Dispatch(hierarchyCollector);
            Assert.NotEmpty(hierarchyCollector);
            Assert.Equal(6, hierarchyCollector.Count());
        }

        [Fact]
        public void FromTypeMember()
        {
            AssetHierarchyCollector hierarchyCollector = new AssetHierarchyCollector();
            var memebers = this.Fixture.Convert(typeof(RegularClass)).Members;
            foreach (ITypeDefinitionMember member in memebers)
            {
                member.Dispatch(hierarchyCollector);
                Assert.Equal(7, hierarchyCollector.Count());
                hierarchyCollector.Clear();
            }
        }


        [Fact]
        public void FromGenericType()
        {
            AssetHierarchyCollector hierarchyCollector = new AssetHierarchyCollector();
            this.Fixture.Convert(typeof(GenericClass<>)).Dispatch(hierarchyCollector);
            Assert.NotEmpty(hierarchyCollector);
            Assert.Equal(6, hierarchyCollector.Count());
        }

        [Fact]
        public void FromNestedGenericType()
        {
            AssetHierarchyCollector hierarchyCollector = new AssetHierarchyCollector();
            this.Fixture.Convert(typeof(GenericClass<>)).NestedTypes.First().Dispatch(hierarchyCollector);
            Assert.NotEmpty(hierarchyCollector);
            Assert.Equal(7, hierarchyCollector.Count());
        }

        [Fact]
        public void FromNestedGenericTypeMember()
        {
            AssetHierarchyCollector hierarchyCollector = new AssetHierarchyCollector();
            this.Fixture.Convert(typeof(GenericClass<>)).NestedTypes.First().Members.First().Dispatch(hierarchyCollector);
            Assert.NotEmpty(hierarchyCollector);
            Assert.Equal(8, hierarchyCollector.Count());
        }
    }
}
