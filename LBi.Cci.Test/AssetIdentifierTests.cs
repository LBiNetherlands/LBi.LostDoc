using System;
using System.Collections.Generic;
using System.Linq;
using Company.Project.Library;
using Xunit;

namespace LBi.Cci.Test
{
    public class AssetIdentifierTests : HostTestBase
    {
        [Fact]
        public void ParseSimple_ConversionOperator()
        {
            AssetIdentifier aid = AssetIdentifier.Parse("M:Company.Project.Library.RegularClass.op_Explicit(Company.Project.Library.RegularClass)~System.Int64");

            Assert.False(aid.HasVersion);
            Assert.Equal("M:Company.Project.Library.RegularClass.op_Explicit(Company.Project.Library.RegularClass)~System.Int64",
                         aid.AssetId);
        }

        [Fact]
        public void ParseComplex_ConversionOperator()
        {
            AssetIdentifier aid = AssetIdentifier.Parse("{M:Company.Project.Library.RegularClass.op_Explicit(Company.Project.Library.RegularClass)~System.Int64, V:1.0.0.0}");

            Assert.True(aid.HasVersion);
            Assert.Equal("M:Company.Project.Library.RegularClass.op_Explicit(Company.Project.Library.RegularClass)~System.Int64",
                         aid.AssetId);
        }

        [Fact]
        public void ParseSimpleAssetId_NestedTypeAssetIdWithGenericParent()
        {
            AssetIdentifier aid = AssetIdentifier.Parse("T:System.Collections.Generic.List`1.Enumerator");
            Assert.Equal(AssetType.Type, aid.Type);
            Assert.Equal("T", aid.TypeMarker);
            Assert.Equal("T:System.Collections.Generic.List`1.Enumerator", aid.AssetId);
            Assert.Null(aid.Version);
            Assert.False(aid.HasVersion);
        }

        //[Fact]

        //public void ParseMethodsWithRefParams()
        //{
        //    var aid = AssetIdentifier.FromMemberInfo(typeof(GenericClass<>).GetMethod("TryRefMethod"));
        //    // Ensure we can parse this.
        //    var id = AssetIdentifier.Parse(aid.AssetId);
        //    aid = AssetIdentifier.FromMemberInfo(typeof(GenericClass<>).GetMethod("TryRefMethodGeneric"));

        //    Assert.Equal(
        //        "M:Company.Project.Library.GenericClass`1.TryRefMethodGeneric``1(Company.Project.Library.GenericClass{`0}.NestedGeneric{``0}@)",
        //        aid.AssetId);

        //    // Ensure we can parse this.
        //    id = AssetIdentifier.Parse(aid.AssetId);
        //}

        [Fact]
        public void ParseExplicitInterfaceImpl()
        {
            var dict = typeof(Dictionary<string, string>);

            var icol = dict.GetInterfaceMap(typeof(ICollection<KeyValuePair<string, string>>));

            var addMethod = icol.TargetMethods.Single(m => m.IsPrivate && m.Name.EndsWith(".Add"));

            var aid = AssetIdentifier.FromMemberInfo(Fixture.Convert(addMethod));

            var id = AssetIdentifier.Parse(aid.AssetId);
        }

        [Fact]
        public void ParseSimpleAssetId_Type()
        {
            AssetIdentifier aid = AssetIdentifier.Parse("T:System.Object");

            Assert.Equal(AssetType.Type, aid.Type);
            Assert.Equal("T", aid.TypeMarker);
            Assert.Equal("T:System.Object", aid.AssetId);
            Assert.Null(aid.Version);
            Assert.False(aid.HasVersion);
        }

        [Fact]
        public void ParseSimpleAssetId_Method()
        {
            AssetIdentifier aid = AssetIdentifier.Parse("M:System.Object.ToString");
            Assert.Equal(AssetType.Method, aid.Type);
            Assert.Equal("M", aid.TypeMarker);
            Assert.Equal("M:System.Object.ToString", aid.AssetId);
            Assert.Null(aid.Version);
            Assert.False(aid.HasVersion);
        }
        [Fact]
        public void ParseSimpleUnknown()
        {
            AssetIdentifier aid = AssetIdentifier.Parse("Overload:System.Xml.XmlWriter.Create");
            Assert.Equal(AssetType.Unknown, aid.Type);
            Assert.Equal("Overload", aid.TypeMarker);
            Assert.Equal("Overload:System.Xml.XmlWriter.Create", aid.AssetId);
            Assert.Null(aid.Version);
            Assert.False(aid.HasVersion);
        }

        [Fact]
        public void ParseComplexAssetId()
        {
            AssetIdentifier aid = AssetIdentifier.Parse("{T:System.Object, V:4.1.2.3}");
            Assert.Equal(AssetType.Type, aid.Type);
            Assert.Equal("T", aid.TypeMarker);
            Assert.Equal("T:System.Object", aid.AssetId);
            Assert.Equal(new Version(4, 1, 2, 3), aid.Version);
            Assert.True(aid.HasVersion);
        }

        [Fact]
        public void ParseSimpleAssetId_WithVersion()
        {
            AssetIdentifier aid = AssetIdentifier.Parse("{T:System.Object, V:4.1.2.3}");
            Assert.Equal(AssetType.Type, aid.Type);
            Assert.Equal("T", aid.TypeMarker);
            Assert.Equal("T:System.Object", aid.AssetId);
            Assert.Equal(new Version(4, 1, 2, 3), aid.Version);
            Assert.True(aid.HasVersion);
        }

        [Fact]
        public void ParseOpenGenericTypeAssetId_TwoParams()
        {
            AssetIdentifier aid = AssetIdentifier.Parse("{T:System.Collections.Generic.Dictionary`2, V:4.0.0.0}");
            AssetIdentifier aid2 = AssetIdentifier.FromType(Fixture.Convert(typeof(Dictionary<string, string>)));
            Assert.Equal(aid.ToString(), aid2.ToString());
        }

        [Fact]
        public void FromClosedGenericType()
        {
            AssetIdentifier aid = AssetIdentifier.FromType(Fixture.Convert(typeof(List<string>)));
            Assert.Equal("T:System.Collections.Generic.List`1", aid.AssetId);
        }

        [Fact]
        public void FromMethod_Declared()
        {
            AssetIdentifier aid = AssetIdentifier.FromMemberInfo(Fixture.Convert(typeof(RegularClass).GetMethod("Dispose")));
            Assert.Equal("M:Company.Project.Library.RegularClass.Dispose", aid.AssetId);
        }

        [Fact(Skip = "can't run this here atm")]
        public void FromMethod_Inherited()
        {
            AssetIdentifier aid = AssetIdentifier.FromMemberInfo(Fixture.Convert(typeof(RegularClass).GetMethod("ToString")));
            Assert.Equal("M:Company.Project.Library.RegularClass.ToString", aid.AssetId);
        }

        [Fact]
        public void FromClosedGenericType_WithTwoParams_ToString()
        {
            AssetIdentifier aid = AssetIdentifier.FromType(Fixture.Convert(typeof(Dictionary<string, string>)));
            Assert.Equal("{T:System.Collections.Generic.Dictionary`2, V:4.0.0.0}", aid.ToString());
        }

        [Fact]
        public void EqualityTest_SimpleType()
        {
            AssetIdentifier aid = AssetIdentifier.FromType(Fixture.Convert(typeof(string)));
            AssetIdentifier aid2 = AssetIdentifier.FromType(Fixture.Convert(typeof(string)));
            Assert.True(aid.Equals(aid2));
        }

        [Fact]
        public void EqualityTest_SimpleType_NotEqual()
        {
            AssetIdentifier aid = AssetIdentifier.FromType(Fixture.Convert(typeof(string)));
            AssetIdentifier aid2 = AssetIdentifier.FromType(Fixture.Convert(typeof(int)));
            Assert.False(aid.Equals(aid2));
        }

        [Fact]
        public void EqualityTest_OpenGenericType()
        {
            AssetIdentifier aid = AssetIdentifier.FromType(Fixture.Convert(typeof(List<>)));
            AssetIdentifier aid2 = AssetIdentifier.FromType(Fixture.Convert(typeof(List<>)));
            Assert.True(aid.Equals(aid2));
        }


        [Fact]
        public void EqualityTest_OpenGenericType_NotEqual()
        {
            AssetIdentifier aid = AssetIdentifier.FromType(Fixture.Convert(typeof(List<>)));
            AssetIdentifier aid2 = AssetIdentifier.FromType(Fixture.Convert(typeof(HashSet<>)));
            Assert.False(aid.Equals(aid2));
        }

        [Fact]
        public void EqualityTest_ClosedGenericType_NotEqual()
        {
            AssetIdentifier aid = AssetIdentifier.FromType(Fixture.Convert(typeof(List<string>)));
            AssetIdentifier aid2 = AssetIdentifier.FromType(Fixture.Convert(typeof(HashSet<string>)));
            Assert.False(aid.Equals(aid2));
        }

        [Fact]
        public void EqualityTest_ClosedGenericType_NotEqualTypeParam()
        {
            AssetIdentifier aid = AssetIdentifier.FromType(Fixture.Convert(typeof(List<int>)));
            AssetIdentifier aid2 = AssetIdentifier.FromType(Fixture.Convert(typeof(List<string>)));
            Assert.True(aid.Equals(aid2));
        }


        [Fact]
        public void EqualityTest_ClosedGenericType_EqualTypeParam()
        {
            AssetIdentifier aid = AssetIdentifier.FromType(Fixture.Convert(typeof(List<int>)));
            AssetIdentifier aid2 = AssetIdentifier.FromType(Fixture.Convert(typeof(List<int>)));
            Assert.True(aid.Equals(aid2));
        }

        //[Fact]
        //public void FromInheritedMemeberInfo()
        //{
        //    AssetIdentifier aid = AssetIdentifier.FromMemberInfo(typeof(RegularClass).GetMethod("ToString"));

        //    Assert.Equal("{M:Company.Project.Library.RegularClass.ToString, V:" + aid.Version + "}", aid.ToString());
        //}

        [Fact]
        public void EqualityTest_SimpleType_DifferentVersion()
        {
            AssetIdentifier aid = AssetIdentifier.Parse("{T:System.Object, V:4.1.2.3}");
            AssetIdentifier aid2 = AssetIdentifier.FromType(Fixture.Convert(typeof(object)));
            Assert.False(aid.Equals(aid2));
        }


        [Fact]
        public void Parse_With_Specific_Type()
        {
            AssetIdentifier aid =
                AssetIdentifier.Parse("{M:LBi.Testing.Data.RowSet.GetChangeSet(LBi.Testing.Data.RowSet,System.Collections.Generic.IEnumerable{System.String}), V:1.0.0.14}");
        }
    }
}