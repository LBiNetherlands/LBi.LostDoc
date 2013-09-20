/*
 * Copyright 2012-2013 LBi Netherlands B.V.
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using LBi.LostDoc.Diagnostics;
using Microsoft.Cci;
using AssemblyName = System.Reflection.AssemblyName;

namespace LBi.LostDoc.Reflection
{
    public class AssemblyLoader : IAssemblyLoader
    {
        private readonly ObjectCache _cache;
        private readonly string[] _locations;
        private readonly HashSet<IAssembly> _loadedAssemblies;
        private readonly IMetadataHost _host;

        public AssemblyLoader(ObjectCache cache, IEnumerable<string> assemblyLocation)
        {
            this._cache = cache;
            this._locations = assemblyLocation.ToArray();
            this._loadedAssemblies = new HashSet<IAssembly>();
            this._host = new PeReader.DefaultHost();
            //AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += this.OnFailedAssemblyResolve;
        }

        public IAssembly Load(string name)
        {
            IAssembly assembly = this._host.LoadAssembly(UnitHelper.GetAssemblyIdentity(new System.Reflection.AssemblyName(name), this._host));

            this.RegisterAssembly(assembly);

            return assembly;

        }

        private void RegisterAssembly(IAssembly assembly, bool direct = true)
        {
            if (assembly != null && this._loadedAssemblies.Add(assembly))
            {
                TraceSources.AssemblyLoader.TraceVerbose(TraceEvents.CacheHit,
                                         "Loading assembly {0}.",
                                         UnitHelper.GetAssemblyIdentity(assembly).ToString());

                if (direct)
                {
                    this.GetAssemblyChain(assembly);
                }
            }
        }

        public IAssembly LoadFrom(string path)
        {
            AssemblyName assemblyName = System.Reflection.AssemblyName.GetAssemblyName(path);
            AssemblyIdentity identity = UnitHelper.GetAssemblyIdentity(assemblyName, this._host);
            IAssembly assembly = this._host.LoadAssembly(identity);
            this.RegisterAssembly(assembly);
            return assembly;
        }

        public IEnumerable<IAssembly> GetAssemblyChain(IAssembly assembly)
        {
            string fullName = assembly.AssemblyIdentity.ToString();
            var obj = this._cache.Get(fullName);

            if (obj != null)
            {
                TraceSources.AssemblyLoader.TraceVerbose(TraceEvents.CacheHit,
                                                         "Getting assembly chain for assembly {0} from cache.",
                                                         fullName);
                return (IAssembly[])obj;
            }
            Stopwatch timer = Stopwatch.StartNew();

            List<IAssembly> ret = new List<IAssembly> { assembly };

            TraceSources.AssemblyLoader.TraceVerbose(TraceEvents.CacheMiss,
                                         "No assembly chain cached for assembly {0}.",
                                         fullName);

            foreach (IAssemblyReference assemblyRef in assembly.AssemblyReferences)
            {
                IAssembly refAsm = this.LoadAssemblyInternal(assemblyRef.AssemblyIdentity.ToString(),
                                                            Path.GetDirectoryName(assembly.Location));

                TraceSources.AssemblyLoader.TraceVerbose("Loading referenced assembly: {0}", refAsm.AssemblyIdentity.ToString());

                Debug.Assert(refAsm != null);

                ret.Add(refAsm);
            }

            if (!this._cache.Add(fullName, ret.ToArray(), ObjectCache.InfiniteAbsoluteExpiration))
                TraceSources.AssemblyLoader.TraceVerbose("Failed to add assembly chain to cache for assembly: {0}", fullName);

            TraceSources.AssemblyLoader.TraceData(TraceEventType.Verbose,
                                                  TraceEvents.CachePenalty,
                                                  (ulong)((timer.ElapsedTicks / (double)Stopwatch.Frequency) * 1000000));

            return ret;
        }

        private IAssembly LoadAssemblyInternal(string fullName, params string[] probePaths)
        {
            AssemblyIdentity assemblyIdentity = UnitHelper.GetAssemblyIdentity(new AssemblyName(fullName), this._host);
            IAssembly ret = _host.FindAssembly(assemblyIdentity);

            _host.
            if (ret == null)
            {
                try
                {
                    ret = _host.LoadAssembly(assemblyIdentity);
                }
                catch (FileNotFoundException)
                {
                    foreach (string asmPath in probePaths)
                    {
                        IEnumerable<string> allFiles;
                        allFiles = Directory.EnumerateFiles(asmPath, "*.dll");
                        allFiles = allFiles.Concat(Directory.EnumerateFiles(asmPath, "*.exe"));
                        allFiles = allFiles.Concat(this._locations.SelectMany(loc => Directory.EnumerateFiles(loc, "*.dll")));
                        allFiles = allFiles.Concat(this._locations.SelectMany(loc => Directory.EnumerateFiles(loc, "*.exe")));

                        foreach (string fileName in allFiles)
                        {
                            if (AssemblyName.GetAssemblyName(fileName).FullName == fullName)
                            {
                                ret = _host.LoadAssembly(new AssemblyIdentity(assemblyIdentity, fileName));
                                break;
                            }
                        }

                        if (ret != null)
                            break;
                    }
                }
            }

            this.RegisterAssembly(ret, direct: false);

            return ret;
        }


        public void Dispose()
        {
        }

        public IEnumerator<IAssembly> GetEnumerator()
        {
            var clone = new HashSet<IAssembly>(this._loadedAssemblies);
            var seen = new HashSet<IAssembly>();
            do
            {
                foreach (IAssembly assembly in clone)
                {
                    yield return assembly;
                }

                seen.UnionWith(clone);
                clone = new HashSet<IAssembly>(this._loadedAssemblies);
                clone.ExceptWith(seen);

            } while (clone.Count > 0);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
