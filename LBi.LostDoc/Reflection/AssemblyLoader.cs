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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using LBi.LostDoc.Diagnostics;
using Microsoft.Cci;

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
            IAssembly assembly;
            using (var host = new PeReader.DefaultHost())
            {
                try
                {
                    assembly = host.Lo
                }
                catch (FileLoadException)
                {
                    var assemblyName = AssemblyName.GetAssemblyName(path);
                    var fullName = assemblyName.FullName;
                    var loadedAssemblies = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies();
                    assembly =
                        loadedAssemblies.Single(a => StringComparer.Ordinal.Equals(a.GetName().FullName, fullName));
                }

                this.RegisterAssembly(assembly);
            }
            return assembly;
        }

        public IEnumerable<IAssembly> GetAssemblyChain(IAssembly assembly)
        {

            var obj = this._cache.Get(assembly.);

            if (obj != null)
            {
                TraceSources.AssemblyLoader.TraceVerbose(TraceEvents.CacheHit,
                                                         "Getting assembly chain for assembly {0} from cache.",
                                                         assembly.FullName);
                return (IAssembly[])obj;
            }
            Stopwatch timer = Stopwatch.StartNew();

            List<IAssembly> ret = new List<IAssembly> { assembly };

            TraceSources.AssemblyLoader.TraceVerbose(TraceEvents.CacheMiss,
                                         "No assembly chain cached for assembly {0}.",
                                         assembly.FullName);

            foreach (AssemblyName assemblyName in assembly.GetReferencedAssemblies())
            {
                Assembly refAsm = this.LoadAssemblyInternal(assemblyName.FullName,
                                                            Path.GetDirectoryName(assembly.Location));

                TraceSources.AssemblyLoader.TraceVerbose("Loading referenced assembly: {0}", refAsm.FullName);

                Debug.Assert(refAsm != null);

                ret.Add(refAsm);
            }

            if (!this._cache.Add(assembly.FullName, ret.ToArray(), ObjectCache.InfiniteAbsoluteExpiration))
                TraceSources.AssemblyLoader.TraceVerbose("Failed to add assembly chain to cache for assembly: {0}", assembly.FullName);

            TraceSources.AssemblyLoader.TraceData(TraceEventType.Verbose,
                                                  TraceEvents.CachePenalty,
                                                  (ulong)((timer.ElapsedTicks / (double)Stopwatch.Frequency) * 1000000));

            return ret;
        }

        private Assembly LoadAssemblyInternal(string fullName, params string[] probePaths)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies();

            Assembly ret = assemblies.FirstOrDefault(a => a.FullName == fullName);

            if (ret == null)
            {
                try
                {
                    ret = Assembly.ReflectionOnlyLoad(fullName);
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
                                ret = Assembly.ReflectionOnlyLoadFrom(fileName);
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

        private Assembly OnFailedAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var asm = this.LoadAssemblyInternal(args.Name, Path.GetDirectoryName(args.RequestingAssembly.Location));

            var obj = this._cache.Get(args.RequestingAssembly.FullName);
            if (asm != null && obj != null)
            {
                Assembly[] assemblies = (Assembly[])obj;
                if (Array.IndexOf(assemblies, asm) < 0)
                {
                    Array.Resize(ref assemblies, assemblies.Length + 1);
                    assemblies[assemblies.Length - 1] = asm;
                    this._cache.Add(args.RequestingAssembly.FullName, assemblies, ObjectCache.InfiniteAbsoluteExpiration);
                }
            }
            return asm;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= this.OnFailedAssemblyResolve;
        }

        public IEnumerator<Assembly> GetEnumerator()
        {
            var clone = new HashSet<Assembly>(this._loadedAssemblies);
            var seen = new HashSet<Assembly>();
            do
            {
                foreach (Assembly assembly in clone)
                {
                    yield return assembly;
                }

                seen.UnionWith(clone);
                clone = new HashSet<Assembly>(this._loadedAssemblies);
                clone.ExceptWith(seen);

            } while (clone.Count > 0);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
    //public class ReflectionOnlyAssemblyLoader : IAssemblyLoader
    //{
    //    private readonly ObjectCache _cache;
    //    private readonly string[] _locations;
    //    private HashSet<Assembly> _loadedAssemblies;

    //    public ReflectionOnlyAssemblyLoader(ObjectCache cache, IEnumerable<string> assemblyLocation)
    //    {
    //        this._cache = cache;
    //        this._locations = assemblyLocation.ToArray();
    //        this._loadedAssemblies = new HashSet<Assembly>();
    //        AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += this.OnFailedAssemblyResolve;
    //    }

    //    public Assembly Load(string name)
    //    {
    //        Assembly assembly;
    //        try
    //        {
    //            assembly = Assembly.ReflectionOnlyLoad(name);
    //        }
    //        catch (FileLoadException)
    //        {
    //            var loadedAssemblies = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies();
    //            assembly = loadedAssemblies.Single(a => StringComparer.Ordinal.Equals(a.GetName().FullName, name));
    //        }

    //        this.RegisterAssembly(assembly);

    //        return assembly;
    //    }

    //    private void RegisterAssembly(Assembly assembly, bool direct = true)
    //    {
    //        if (assembly != null && this._loadedAssemblies.Add(assembly))
    //        {
    //            TraceSources.AssemblyLoader.TraceVerbose(TraceEvents.CacheHit,
    //                                     "Loading assembly {0}.",
    //                                     assembly.FullName);

    //            if (direct)
    //            {
    //                this.GetAssemblyChain(assembly);
    //            }
    //        } 
    //    }

    //    public Assembly LoadFrom(string path)
    //    {
    //        Assembly assembly;
    //        try
    //        {
    //            assembly = Assembly.ReflectionOnlyLoadFrom(path);
    //        }
    //        catch (FileLoadException)
    //        {
    //            var assemblyName = AssemblyName.GetAssemblyName(path);
    //            var fullName = assemblyName.FullName;
    //            var loadedAssemblies = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies();
    //            assembly = loadedAssemblies.Single(a => StringComparer.Ordinal.Equals(a.GetName().FullName, fullName));
    //        }

    //        this.RegisterAssembly(assembly);

    //        return assembly;
    //    }

    //    public IEnumerable<Assembly> GetAssemblyChain(Assembly assembly)
    //    {

    //        var obj = this._cache.Get(assembly.FullName);

    //        if (obj != null)
    //        {
    //            TraceSources.AssemblyLoader.TraceVerbose(TraceEvents.CacheHit,
    //                                                     "Getting assembly chain for assembly {0} from cache.",
    //                                                     assembly.FullName);
    //            return (Assembly[])obj;
    //        }
    //        Stopwatch timer = Stopwatch.StartNew();

    //        List<Assembly> ret = new List<Assembly> {assembly};

    //        TraceSources.AssemblyLoader.TraceVerbose(TraceEvents.CacheMiss,
    //                                     "No assembly chain cached for assembly {0}.",
    //                                     assembly.FullName);

    //        foreach (AssemblyName assemblyName in assembly.GetReferencedAssemblies())
    //        {
    //            Assembly refAsm = this.LoadAssemblyInternal(assemblyName.FullName,
    //                                                        Path.GetDirectoryName(assembly.Location));

    //            TraceSources.AssemblyLoader.TraceVerbose("Loading referenced assembly: {0}", refAsm.FullName);

    //            Debug.Assert(refAsm != null);

    //            ret.Add(refAsm);
    //        }

    //        if (!this._cache.Add(assembly.FullName, ret.ToArray(), ObjectCache.InfiniteAbsoluteExpiration))
    //            TraceSources.AssemblyLoader.TraceVerbose("Failed to add assembly chain to cache for assembly: {0}", assembly.FullName);

    //        TraceSources.AssemblyLoader.TraceData(TraceEventType.Verbose,
    //                                              TraceEvents.CachePenalty,
    //                                              (ulong)((timer.ElapsedTicks / (double)Stopwatch.Frequency) * 1000000));

    //        return ret;
    //    }

    //    private Assembly LoadAssemblyInternal(string fullName, params string[] probePaths)
    //    {
    //        Assembly[] assemblies = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies();

    //        Assembly ret = assemblies.FirstOrDefault(a => a.FullName == fullName);

    //        if (ret == null)
    //        {
    //            try
    //            {
    //                ret = Assembly.ReflectionOnlyLoad(fullName);
    //            }
    //            catch (FileNotFoundException)
    //            {
    //                foreach (string asmPath in probePaths)
    //                {
    //                    IEnumerable<string> allFiles;
    //                    allFiles = Directory.EnumerateFiles(asmPath, "*.dll");
    //                    allFiles = allFiles.Concat(Directory.EnumerateFiles(asmPath, "*.exe"));
    //                    allFiles = allFiles.Concat(this._locations.SelectMany(loc => Directory.EnumerateFiles(loc, "*.dll"))); 
    //                    allFiles = allFiles.Concat(this._locations.SelectMany(loc => Directory.EnumerateFiles(loc, "*.exe")));

    //                    foreach (string fileName in allFiles)
    //                    {
    //                        if (AssemblyName.GetAssemblyName(fileName).FullName == fullName)
    //                        {
    //                            ret = Assembly.ReflectionOnlyLoadFrom(fileName);
    //                            break;
    //                        }
    //                    }

    //                    if (ret != null)
    //                        break;
    //                }
    //            }
    //        }

    //        this.RegisterAssembly(ret, direct: false);

    //        return ret;
    //    }

    //    private Assembly OnFailedAssemblyResolve(object sender, ResolveEventArgs args)
    //    {
    //        var asm = this.LoadAssemblyInternal(args.Name, Path.GetDirectoryName(args.RequestingAssembly.Location));

    //        var obj = this._cache.Get(args.RequestingAssembly.FullName);
    //        if (asm != null && obj != null)
    //        {
    //            Assembly[] assemblies = (Assembly[]) obj;
    //            if (Array.IndexOf(assemblies, asm) < 0)
    //            {
    //                Array.Resize(ref assemblies, assemblies.Length + 1);
    //                assemblies[assemblies.Length - 1] = asm;
    //                this._cache.Add(args.RequestingAssembly.FullName, assemblies, ObjectCache.InfiniteAbsoluteExpiration);
    //            }
    //        }
    //        return asm;
    //    }

    //    public void Dispose()
    //    {
    //        AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= this.OnFailedAssemblyResolve;
    //    }

    //    public IEnumerator<Assembly> GetEnumerator()
    //    {
    //        var clone = new HashSet<Assembly>(this._loadedAssemblies);
    //        var seen = new HashSet<Assembly>();
    //        do
    //        {
    //            foreach (Assembly assembly in clone)
    //            {
    //                yield return assembly;
    //            }

    //            seen.UnionWith(clone);
    //            clone = new HashSet<Assembly>(this._loadedAssemblies);
    //            clone.ExceptWith(seen);

    //        } while (clone.Count > 0);
    //    }

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return this.GetEnumerator();
    //    }
    //}
}
