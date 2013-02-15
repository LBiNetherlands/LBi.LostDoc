/*
 * Copyright 2012 LBi Netherlands B.V.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */

using System;
using System.Diagnostics;
using LBi.LostDoc.Templating.AssetResolvers;
using Xunit;

namespace LBi.LostDoc.Test
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
