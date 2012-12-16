using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Company.Project.Library;
using Xunit;
using Xunit.Extensions;

namespace LBi.LostDoc.Core.Test
{
    public class AssetResolverTests
    {
        private AssetResolver _resolver;

        public AssetResolverTests()
        {
            List<Assembly> assemblies = new List<Assembly>();
            assemblies.Add(Assembly.GetExecutingAssembly());
            AssemblyName[] names = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            assemblies.AddRange(names.Select(an =>
                                                 {
                                                     Assembly asm =
                                                         AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(
                                                                                                                 a =>
                                                                                                                 a.
                                                                                                                     GetName
                                                                                                                     ().
                                                                                                                     FullName ==
                                                                                                                 an.
                                                                                                                     FullName);

                                                     return asm ?? Assembly.Load(an);
                                                 }));

            this._resolver = new AssetResolver(assemblies);
        }

        [Theory]
        [InlineData("T:Company.Project.Library.GenericClass`1.NestedGeneric`1.ConsumeU``1(``0)+U",
            typeof(GenericClass<>.NestedGeneric<>))]
        [InlineData("T:System.Collections.Generic.List`1.Enumerator", typeof(List<>.Enumerator))]
        [InlineData("M:System.Collections.Generic.List`1.GetEnumerator", typeof(List<>))]
        [InlineData("M:System.Collections.Generic.List`1+T", typeof(List<>))]
        [InlineData("P:System.Collections.Generic.List`1.Length", typeof(List<>))]
        public void GetDeclaringType(string assetId, Type declaringType)
        {
            AssetIdentifier aid = AssetIdentifier.Parse(assetId);
            int pos = 0;
            Type resolvedType = this._resolver.GetDeclaringType(aid.AssetId.Substring(aid.TypeMarker.Length + 1),
                                                                ref pos);
            Assert.Equal(resolvedType, declaringType);
        }


        [Theory]
        [InlineData("T:Company.Project.Library.GenericClass`1.NestedGeneric`1")]
        [InlineData("T:System.Collections.Generic.List`1.Enumerator")]
        [InlineData("T:System.Collections.Generic.List`1")]
        [InlineData("T:Company.Project.Library.GenericClass`1.<GetEnumerator>d__0")]
        [InlineData("T:Company.Project.Library.GenericClass`1")]
        public void RoundTripTypes(string assetId)
        {
            Debug.WriteLine(assetId);
            AssetIdentifier aid = AssetIdentifier.Parse(assetId);
            object obj = this._resolver.Resolve(aid);
            Assert.NotNull(obj);
            Assert.Equal(assetId, AssetIdentifier.FromMemberInfo((Type)obj).AssetId);
        }

        [Theory]
        [InlineData(
            "M:Company.Project.Library.RegularClass.op_Explicit(Company.Project.Library.RegularClass)~System.Int64")]
        public void RoundTripMember(string assetId)
        {
            Debug.WriteLine(assetId);
            AssetIdentifier aid = AssetIdentifier.Parse(assetId);
            MemberInfo obj = (MemberInfo)this._resolver.Resolve(aid);
            Assert.NotNull(obj);
            Assert.Equal(assetId, AssetIdentifier.FromMemberInfo(obj).AssetId);
        }
    }
}