using System.Linq;
using Company.Project.Library;
using LBi.LostDoc.Cci;
using Xunit;

namespace LBi.LostDoc.Test
{
    public class AssetHierarchyTests : HostTestBase
    {
        [Fact]
        public void FromAssembly()
        {
            var assembly = this.Fixture.Assemblies.First();

            AssetHierarchyBuilder hierarchyBuilder = new AssetHierarchyBuilder();
            assembly.Dispatch(hierarchyBuilder);
            Assert.NotEmpty(hierarchyBuilder);
            Assert.Equal(new[] { assembly }, hierarchyBuilder);
        }

        [Fact]
        public void FromType()
        {
            AssetHierarchyBuilder hierarchyBuilder = new AssetHierarchyBuilder();
            this.Fixture.Convert(typeof(RegularClass)).Dispatch(hierarchyBuilder);
            Assert.NotEmpty(hierarchyBuilder);
            Assert.Equal(6, hierarchyBuilder.Count());
        }

        [Fact]
        public void FromTypeMember()
        {
            AssetHierarchyBuilder hierarchyBuilder = new AssetHierarchyBuilder();
            this.Fixture.Convert(typeof(RegularClass)).Members.First().Dispatch(hierarchyBuilder);
            Assert.NotEmpty(hierarchyBuilder);
            Assert.Equal(7, hierarchyBuilder.Count());
        }


        [Fact]
        public void FromGenericType()
        {
            AssetHierarchyBuilder hierarchyBuilder = new AssetHierarchyBuilder();
            this.Fixture.Convert(typeof(GenericClass<>)).Dispatch(hierarchyBuilder);
            Assert.NotEmpty(hierarchyBuilder);
            Assert.Equal(6, hierarchyBuilder.Count());
        }

        [Fact]
        public void FromNestedGenericType()
        {
            AssetHierarchyBuilder hierarchyBuilder = new AssetHierarchyBuilder();
            this.Fixture.Convert(typeof(GenericClass<>)).NestedTypes.First().Dispatch(hierarchyBuilder);
            Assert.NotEmpty(hierarchyBuilder);
            Assert.Equal(7, hierarchyBuilder.Count());
        }

        [Fact]
        public void FromNestedGenericTypeMember()
        {
            AssetHierarchyBuilder hierarchyBuilder = new AssetHierarchyBuilder();
            this.Fixture.Convert(typeof(GenericClass<>)).NestedTypes.First().Members.First().Dispatch(hierarchyBuilder);
            Assert.NotEmpty(hierarchyBuilder);
            Assert.Equal(8, hierarchyBuilder.Count());
        }
    }
}
