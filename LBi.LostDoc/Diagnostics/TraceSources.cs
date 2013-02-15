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

using System.Diagnostics;

namespace LBi.LostDoc.Diagnostics
{
    public static class TraceEvents
    {
        public static readonly int CacheHit = 1001;
        public static readonly int CacheMiss = 1002;
        public static readonly int CachePenalty = 1003;
    }

    public static class TraceSources
    {
        public static readonly TraceSource TemplateSource;
        public static readonly TraceSource AssetResolverSource;
        public static readonly TraceSource GeneratorSource;
        public static readonly TraceSource BundleSource;
        public static readonly TraceSource AssemblyLoader;

        static TraceSources()
        {
            AssemblyLoader = new TraceSource("LostDoc.Core.ReflectionOnlyAssemblyLoader", SourceLevels.All);
            TemplateSource = new TraceSource("LostDoc.Core.Template", SourceLevels.All);
            AssetResolverSource = new TraceSource("LostDoc.Core.Template.AssetResolver", SourceLevels.All);
            GeneratorSource = new TraceSource("LostDoc.Core.DocGenerator", SourceLevels.All);
            BundleSource = new TraceSource("LostDoc.Core.Bundle", SourceLevels.All);
        }
    }
}
