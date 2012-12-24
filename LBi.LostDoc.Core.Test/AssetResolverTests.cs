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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Company.Project.Library;
using Xunit;
using Xunit.Extensions;

namespace LBi.LostDoc.Core.Test
{
    public class AssetResolverTests : IUseFixture<AssemblyFixture>
    {
        private AssetResolver _resolver;

        [Theory]
        [InlineData("T:Company.Project.Library.GenericClass`1.NestedGeneric`1.ConsumeU``1(``0)+U", typeof(GenericClass<>.NestedGeneric<>))]
        [InlineData("T:System.Collections.Generic.List`1.Enumerator", typeof(List<>.Enumerator))]
        [InlineData("M:System.Collections.Generic.List`1.GetEnumerator", typeof(List<>))]
        [InlineData("M:System.Collections.Generic.List`1+T", typeof(List<>))]
        [InlineData("P:System.Collections.Generic.List`1.Length", typeof(List<>))]
        public void GetDeclaringType(string assetId, Type declaringType)
        {
            AssetIdentifier aid = AssetIdentifier.Parse(assetId);
            int pos = 0;
            Type resolvedType = this._resolver.GetDeclaringType(aid.AssetId.Substring(aid.TypeMarker.Length + 1),
                                                                ref pos,
                                                                null);
            Assert.Equal(resolvedType, declaringType);
        }

        [Theory]
        [InlineData("T:Company.Project.Library.GenericClass`1.NestedGeneric`1.ConsumeU``1(``0)+U", typeof(GenericClass<>.NestedGeneric<>))]
        [InlineData("T:System.Collections.Generic.List`1.Enumerator", typeof(List<>.Enumerator))]
        [InlineData("M:System.Collections.Generic.List`1.GetEnumerator", typeof(List<>))]
        [InlineData("M:System.Collections.Generic.List`1+T", typeof(List<>))]
        [InlineData("P:System.Collections.Generic.List`1.Length", typeof(List<>))]
        public void GetDeclaringType_WithHintAssembly(string assetId, Type declaringType)
        {
            AssetIdentifier aid = AssetIdentifier.Parse(assetId);
            int pos = 0;
            Type resolvedType = this._resolver.GetDeclaringType(aid.AssetId.Substring(aid.TypeMarker.Length + 1),
                                                                ref pos,
                                                                declaringType.Assembly);
            Assert.Equal(resolvedType, declaringType);
        }


        [Theory]
        [InlineData("T:System.Collections.Generic.List`1.Enumerator")]
        [InlineData("T:Company.Project.Library.AccessModifierTests")]
        [InlineData("T:Company.Project.Library.ACustomAttribute")]
        [InlineData("T:Company.Project.Library.GenericClass`1")]
        [InlineData("T:Company.Project.ExtensionMethodTest")]
        [InlineData("T:Company.Project.Bar`1")]
        [InlineData("T:Company.Project.Foo")]
        [InlineData("T:Company.Project.Library.InheritedGenericClass")]
        [InlineData("T:Company.Project.Library.InheritedRegularClass")]
        [InlineData("T:Company.Project.Library.RegularClass")]
        [InlineData("T:Company.Project.Library.RegularClass.NestedEnum")]
        [InlineData("T:Company.Project.Library.RegularClass.RegularNesteedClass")]
        [InlineData("T:Company.Project.Library.InheritedRegularClass.NestedClassThatInheritsParent")]
        [InlineData("T:Company.Project.Library.SecondLevelSimpleGeneric`2")]
        [InlineData("T:Company.Project.Library.SimpleGeneric`3")]
        [InlineData("T:Company.Project.Library.ThirdLevelSimpleGeneric`1")]
        public void RoundTripTypes(string assetId)
        {
            Debug.WriteLine(assetId);
            AssetIdentifier aid = AssetIdentifier.Parse(assetId);
            object obj = this._resolver.Resolve(aid);
            Assert.NotNull(obj);
            Assert.Equal(assetId, AssetIdentifier.FromMemberInfo((Type)obj).AssetId);
        }

        [Theory]
        [InlineData("F:Company.Project.Library.ACustomAttribute.AField")]
        [InlineData("M:Company.Project.Library.ACustomAttribute.#ctor")]
        [InlineData("M:Company.Project.Library.ACustomAttribute.#ctor(System.Int32)")]
        [InlineData("M:Company.Project.Library.ACustomAttribute.#ctor(System.Int32,System.Object)")]
        [InlineData("M:Company.Project.Library.ACustomAttribute.Finalize")]
        [InlineData("P:Company.Project.Library.ACustomAttribute.AProp")]
        [InlineData("P:Company.Project.Library.ACustomAttribute.ArrayProp")]
        [InlineData("M:Company.Project.Library.GenericClass`1.#ctor(`0)")]
        [InlineData("M:Company.Project.Library.GenericClass`1.GetEnumerator")]
        [InlineData("M:Company.Project.Library.GenericClass`1.System#Collections#IEnumerable#GetEnumerator")]
        [InlineData("M:Company.Project.Library.GenericClass`1.ReturnsGeneric")]
        [InlineData("M:Company.Project.Library.GenericClass`1.AnotherGeneric``1(System.Collections.Generic.IEnumerable{``0},System.Collections.Generic.IEnumerable{`0},System.Collections.Generic.IEnumerable{System.Int32})")]
        [InlineData("M:Company.Project.Library.GenericClass`1.TryRefMethod(System.String@)")]
        [InlineData("M:Company.Project.Library.GenericClass`1.TryRefMethodGeneric``1(Company.Project.Library.GenericClass{`0}.NestedGeneric{``0}@)")]
        [InlineData("M:Company.Project.Library.GenericClass`1.ConsumesGeneric(`0)")]
        [InlineData("M:Company.Project.Library.GenericClass`1.ConsumesGeneric``1(`0,``0)")]
        [InlineData("M:Company.Project.Library.GenericClass`1.NestedGeneric`1.ConsumeU``1(``0)")]
        [InlineData("M:Company.Project.ExtensionMethodTest.FooExt(Company.Project.Foo,System.String)")]
        [InlineData("M:Company.Project.ExtensionMethodTest.RegularClassExt(Company.Project.Library.RegularClass,System.String)")]
        [InlineData("M:Company.Project.Foo.op_Equality(Company.Project.Foo,Company.Project.Foo)")]
        [InlineData("M:Company.Project.Foo.op_Inequality(Company.Project.Foo,Company.Project.Foo)")]
        [InlineData("M:Company.Project.Library.InheritedGenericClass.#ctor(System.String)")]
        [InlineData("M:Company.Project.Library.RegularClass.#ctor")]
        [InlineData("M:Company.Project.Library.RegularClass.#ctor(System.String,System.Int32)")]
        [InlineData("M:Company.Project.Library.RegularClass.#ctor(System.String,System.Nullable{System.Int32})")]
        [InlineData("M:Company.Project.Library.RegularClass.Dispose")]
        [InlineData("M:Company.Project.Library.RegularClass.Foo")]
        [InlineData("M:Company.Project.Library.RegularClass.Foo(System.Int32)")]
        [InlineData("M:Company.Project.Library.RegularClass.Foo(System.String)")]
        [InlineData("M:Company.Project.Library.RegularClass.Foo(System.Single)")]
        [InlineData("M:Company.Project.Library.RegularClass.Foo(System.Collections.Generic.List{System.String})")]
        [InlineData("M:Company.Project.Library.RegularClass.op_Implicit(Company.Project.Library.RegularClass)~System.String")]
        [InlineData("M:Company.Project.Library.RegularClass.op_Implicit(Company.Project.Library.RegularClass)~System.Int32")]
        [InlineData("M:Company.Project.Library.RegularClass.op_Explicit(Company.Project.Library.RegularClass)~System.Int64")]
        [InlineData("M:Company.Project.Library.RegularClass.op_Explicit(System.Int32)~Company.Project.Library.RegularClass")]
        [InlineData("M:Company.Project.Library.RegularClass.op_Equality(Company.Project.Library.RegularClass,Company.Project.Library.RegularClass)")]
        [InlineData("M:Company.Project.Library.RegularClass.op_Inequality(Company.Project.Library.RegularClass,Company.Project.Library.RegularClass)")]
        [InlineData("M:Company.Project.Library.RegularClass.op_Addition(Company.Project.Library.RegularClass,Company.Project.Library.RegularClass)")]
        [InlineData("M:Company.Project.Library.RegularClass.op_Subtraction(Company.Project.Library.RegularClass,Company.Project.Library.RegularClass)")]
        [InlineData("M:Company.Project.Library.RegularClass.op_Division(Company.Project.Library.RegularClass,System.Int32)")]
        [InlineData("M:Company.Project.Library.RegularClass.WithOut(System.Int32@)")]
        [InlineData("M:Company.Project.Library.RegularClass.WithRefObj(System.Object@)")]
        [InlineData("M:Company.Project.Library.RegularClass.WithOutObj(System.Object@)")]
        [InlineData("M:Company.Project.Library.RegularClass.WithRef(System.Int32@)")]
        [InlineData("M:Company.Project.Library.RegularClass.WithIntPtr(System.IntPtr)")]
        [InlineData("P:Company.Project.Library.RegularClass.StringProperty")]
        [InlineData("P:Company.Project.Library.RegularClass.ReadonlyIntProperty")]
        [InlineData("M:Company.Project.Library.InheritedRegularClass.Foo")]
        [InlineData("M:Company.Project.Library.InheritedRegularClass.Dispose")]
        [InlineData("M:Company.Project.Library.InheritedRegularClass.WithOut(System.Int32@)")]
        [InlineData("M:Company.Project.Library.SimpleGeneric`3.SimpleTest(`0,`1,`2)")]
        [InlineData("M:Company.Project.Library.SecondLevelSimpleGeneric`2.SimpleTest(System.String,`0,`1)")]
        [InlineData("M:Company.Project.Library.ThirdLevelSimpleGeneric`1.SimpleTest(System.String,System.Int32,`0)")]
        public void RoundTripMember(string assetId)
        {
            Debug.WriteLine(assetId);
            AssetIdentifier aid = AssetIdentifier.Parse(assetId);
            MemberInfo obj = (MemberInfo)this._resolver.Resolve(aid);
            Assert.NotNull(obj);
            Assert.Equal(assetId, AssetIdentifier.FromMemberInfo(obj).AssetId);
        }

        public void SetFixture(AssemblyFixture data)
        {
            this._resolver = new AssetResolver(data);
        }
    }

    public class AssemblyFixture : IEnumerable<Assembly>
    {
        private HashSet<Assembly> _assemblies;

        public AssemblyFixture()
        {
            _assemblies = new HashSet<Assembly>();

            DiscoverAllAssemblies(_assemblies, Assembly.GetExecutingAssembly());
        }

        private static void DiscoverAllAssemblies(HashSet<Assembly> assemblies, Assembly assembly)
        {
            if (!assemblies.Add(assembly))
                return;

            AssemblyName[] names = assembly.GetReferencedAssemblies();
            
            var refs = names.Select(
                an =>
                    {
                        Assembly asm =
                            AppDomain.CurrentDomain
                                     .GetAssemblies()
                                     .SingleOrDefault(a => a.GetName().FullName == an.FullName);

                        return asm ?? Assembly.Load(an);
                    }).ToArray();


            foreach (Assembly @ref in refs)
            {
                DiscoverAllAssemblies(assemblies, @ref);
            }
        }

        public IEnumerator<Assembly> GetEnumerator()
        {
            return this._assemblies.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
