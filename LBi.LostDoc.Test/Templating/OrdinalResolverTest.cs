/*
 * Copyright 2014 DigitasLBi Netherlands B.V.
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
using LBi.LostDoc.Templating;
using Xunit;

namespace LBi.LostDoc.Test.Templating
{
    public class OrdinalResolverTest
    {
        public class ResolveOrdinal
        {
            [Fact]
            public void Single()
            {
                OrdinalResolver<int> resolver = new OrdinalResolver<int>(new Lazy<int>(() => -1));
                resolver.Add(0, new Lazy<int>(() => 0));
                Assert.Equal(-1, resolver.ResolveOrdinal(0));
                Assert.Equal(0, resolver.ResolveOrdinal(1));
                Assert.Equal(0, resolver.ResolveOrdinal(int.MaxValue));
            }


            [Fact]
            public void Contiguous()
            {
                OrdinalResolver<int> resolver = new OrdinalResolver<int>(new Lazy<int>(() => -1));
                resolver.Add(0, new Lazy<int>(() => 0));
                resolver.Add(1, new Lazy<int>(() => 1));
                Assert.Equal(-1, resolver.ResolveOrdinal(0));
                Assert.Equal(0, resolver.ResolveOrdinal(1));
                Assert.Equal(1, resolver.ResolveOrdinal(2));
                Assert.Equal(1, resolver.ResolveOrdinal(int.MaxValue));
            }

            [Fact]
            public void SingleSparse()
            {
                OrdinalResolver<int> resolver = new OrdinalResolver<int>(new Lazy<int>(() => -1));
                resolver.Add(2, new Lazy<int>(() => 2));
                Assert.Equal(-1, resolver.ResolveOrdinal(0));
                Assert.Equal(-1, resolver.ResolveOrdinal(1));
                Assert.Equal(-1, resolver.ResolveOrdinal(2));
                Assert.Equal(2, resolver.ResolveOrdinal(int.MaxValue));
            }
        }

        public class Add
        {
            [Fact]
            public void AddInReverseOrderThrows()
            {
                OrdinalResolver<int> resolver = new OrdinalResolver<int>(new Lazy<int>(() => -1));
                resolver.Add(2, new Lazy<int>(() => 2));
                Assert.Throws<ArgumentOutOfRangeException>(() => resolver.Add(1, new Lazy<int>(() => 1)));
            }

            [Fact]
            public void AddDuplicateThrows()
            {
                OrdinalResolver<int> resolver = new OrdinalResolver<int>(new Lazy<int>(() => -1));
                resolver.Add(2, new Lazy<int>(() => 2));
                Assert.Throws<ArgumentOutOfRangeException>(() => resolver.Add(2, new Lazy<int>(() => 2)));
            }
        }

        public class Resolve
        {
            [Fact]
            public void Single()
            {
                OrdinalResolver<int> resolver = new OrdinalResolver<int>(new Lazy<int>(() => -1));
                resolver.Add(0, new Lazy<int>(() => 0));
                Assert.Equal(-1, resolver.Resolve(0).Value);
                Assert.Equal(0, resolver.Resolve(1).Value);
                Assert.Equal(0, resolver.Resolve(int.MaxValue).Value);
            }


            [Fact]
            public void Contiguous()
            {
                OrdinalResolver<int> resolver = new OrdinalResolver<int>(new Lazy<int>(() => -1));
                resolver.Add(0, new Lazy<int>(() => 0));
                resolver.Add(1, new Lazy<int>(() => 1));
                Assert.Equal(-1, resolver.Resolve(0).Value);
                Assert.Equal(0, resolver.Resolve(1).Value);
                Assert.Equal(1, resolver.Resolve(2).Value);
                Assert.Equal(1, resolver.Resolve(int.MaxValue).Value);
            }

            [Fact]
            public void SingleSparse()
            {
                OrdinalResolver<int> resolver = new OrdinalResolver<int>(new Lazy<int>(() => -1));
                resolver.Add(2, new Lazy<int>(() => 2));
                Assert.Equal(-1, resolver.Resolve(0).Value);
                Assert.Equal(-1, resolver.Resolve(1).Value);
                Assert.Equal(-1, resolver.Resolve(2).Value);
                Assert.Equal(2, resolver.Resolve(int.MaxValue).Value);
            }
        }
    }
}
