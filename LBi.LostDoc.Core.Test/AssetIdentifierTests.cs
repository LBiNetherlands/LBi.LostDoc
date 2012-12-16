using System;
using System.Collections.Generic;
using Company.Project.Library;
using Xunit;

namespace LBi.LostDoc.Core.Test
{
    public class AssetIdentifierTests
    {
        [Fact]
        public void ParseSimple_ConversionOperator()
        {
            AssetIdentifier aid =
                AssetIdentifier.Parse(
                                      "M:Company.Project.Library.RegularClass.op_Explicit(Company.Project.Library.RegularClass)~System.Int64");

            Assert.False(aid.HasVersion);
            Assert.Equal(
                         "M:Company.Project.Library.RegularClass.op_Explicit(Company.Project.Library.RegularClass)~System.Int64",
                         aid.AssetId);
        }

        [Fact]
        public void ParseComplex_ConversionOperator()
        {
            AssetIdentifier aid =
                AssetIdentifier.Parse(
                                      "{M:Company.Project.Library.RegularClass.op_Explicit(Company.Project.Library.RegularClass)~System.Int64, V:1.0.0.0}");

            Assert.True(aid.HasVersion);
            Assert.Equal(
                         "M:Company.Project.Library.RegularClass.op_Explicit(Company.Project.Library.RegularClass)~System.Int64",
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
            AssetIdentifier aid2 = AssetIdentifier.FromMemberInfo(typeof(Dictionary<string, string>));
            Assert.Equal(aid.ToString(), aid2.ToString());
        }

        [Fact]
        public void FromClosedGenericType()
        {
            AssetIdentifier aid = AssetIdentifier.FromMemberInfo(typeof(List<string>));
            Assert.Equal("T:System.Collections.Generic.List`1", aid.AssetId);
        }

        [Fact]
        public void FromClosedGenericType_ToString()
        {
            AssetIdentifier aid = AssetIdentifier.FromMemberInfo(typeof(List<string>));
            Assert.Equal("T:System.Collections.Generic.List`1", aid.AssetId);
        }


        [Fact]
        public void FromClosedGenericType_WithTwoParams_ToString()
        {
            AssetIdentifier aid = AssetIdentifier.FromMemberInfo(typeof(Dictionary<string, string>));
            Assert.Equal("{T:System.Collections.Generic.Dictionary`2, V:4.0.0.0}", aid.ToString());
        }

        [Fact]
        public void EqualityTest_SimpleType()
        {
            AssetIdentifier aid = AssetIdentifier.FromMemberInfo(typeof(string));
            AssetIdentifier aid2 = AssetIdentifier.FromMemberInfo(typeof(string));
            Assert.True(aid.Equals(aid2));
        }

        [Fact]
        public void EqualityTest_SimpleType_NotEqual()
        {
            AssetIdentifier aid = AssetIdentifier.FromMemberInfo(typeof(string));
            AssetIdentifier aid2 = AssetIdentifier.FromMemberInfo(typeof(int));
            Assert.False(aid.Equals(aid2));
        }

        [Fact]
        public void EqualityTest_OpenGenericType()
        {
            AssetIdentifier aid = AssetIdentifier.FromMemberInfo(typeof(List<>));
            AssetIdentifier aid2 = AssetIdentifier.FromMemberInfo(typeof(List<>));
            Assert.True(aid.Equals(aid2));
        }


        [Fact]
        public void EqualityTest_OpenGenericType_NotEqual()
        {
            AssetIdentifier aid = AssetIdentifier.FromMemberInfo(typeof(List<>));
            AssetIdentifier aid2 = AssetIdentifier.FromMemberInfo(typeof(HashSet<>));
            Assert.False(aid.Equals(aid2));
        }

        [Fact]
        public void EqualityTest_ClosedGenericType_NotEqual()
        {
            AssetIdentifier aid = AssetIdentifier.FromMemberInfo(typeof(List<string>));
            AssetIdentifier aid2 = AssetIdentifier.FromMemberInfo(typeof(HashSet<string>));
            Assert.False(aid.Equals(aid2));
        }

        [Fact]
        public void EqualityTest_ClosedGenericType_NotEqualTypeParam()
        {
            AssetIdentifier aid = AssetIdentifier.FromMemberInfo(typeof(List<int>));
            AssetIdentifier aid2 = AssetIdentifier.FromMemberInfo(typeof(List<string>));
            Assert.True(aid.Equals(aid2));
        }


        [Fact]
        public void EqualityTest_ClosedGenericType_EqualTypeParam()
        {
            AssetIdentifier aid = AssetIdentifier.FromMemberInfo(typeof(List<int>));
            AssetIdentifier aid2 = AssetIdentifier.FromMemberInfo(typeof(List<int>));
            Assert.True(aid.Equals(aid2));
        }

        [Fact]
        public void FromInheritedMemeberInfo()
        {
            AssetIdentifier aid = AssetIdentifier.FromMemberInfo(typeof(RegularClass).GetMethod("ToString"));

            Assert.Equal("{M:Company.Project.Library.RegularClass.ToString, V:" + aid.Version + "}", aid.ToString());
        }

        [Fact]
        public void EqualityTest_SimpleType_DifferentVersion()
        {
            AssetIdentifier aid = AssetIdentifier.Parse("{T:System.Object, V:4.1.2.3}");
            AssetIdentifier aid2 = AssetIdentifier.FromMemberInfo(typeof(object));
            Assert.False(aid.Equals(aid2));
        }


        [Fact]
        public void Parse_With_Specific_Type()
        {
            AssetIdentifier aid =
                AssetIdentifier.Parse(
                                      "{M:LBi.Testing.Data.RowSet.GetChangeSet(LBi.Testing.Data.RowSet,System.Collections.Generic.IEnumerable{System.String}), V:1.0.0.14}");
        }
    }
}