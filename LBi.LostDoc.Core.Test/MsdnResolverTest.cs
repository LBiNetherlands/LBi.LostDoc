using System;
using System.Diagnostics;
using LBi.LostDoc.Core.Templating.AssetResolvers;
using Xunit;

namespace LBi.LostDoc.Core.Test
{
    public class MsdnResolverTest
    {
        [Fact]
        public void ResolveSystemString()
        {
            MsdnResolver resolver = new MsdnResolver();
            Uri uri = resolver.ResolveAssetId("T:System.String", null);
            Debug.WriteLine(uri.ToString());
            Assert.NotNull(uri);
        }

        [Fact]
        public void ResolveGenericDictionary()
        {
            MsdnResolver resolver = new MsdnResolver();
            Uri uri = resolver.ResolveAssetId(Naming.GetAssetId(typeof(System.Collections.Generic.Dictionary<,>)), null);
            Debug.WriteLine(uri.ToString());
            Assert.NotNull(uri);
        }

        [Fact]
        public void ResolveInheritedProperty()
        {
            MsdnResolver resolver = new MsdnResolver();
            Uri uri = resolver.ResolveAssetId("P:System.Reflection.MethodInfo.ContainsGenericParameters", null);
            Debug.WriteLine(uri.ToString());
            Assert.NotNull(uri);
        }

        [Fact]
        public void ResolveInheritedPropertyOnDeclaringType()
        {
            MsdnResolver resolver = new MsdnResolver();
            Uri uri = resolver.ResolveAssetId("P:System.Reflection.MethodBase.ContainsGenericParameters", null);
            Debug.WriteLine(uri.ToString());
            Assert.NotNull(uri);
        }
    }
}