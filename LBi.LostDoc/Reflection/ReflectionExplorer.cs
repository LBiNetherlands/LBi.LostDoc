/*
 * Copyright 2013 DigitasLBi Netherlands B.V.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using LBi.LostDoc.Diagnostics;

namespace LBi.LostDoc.Reflection
{
    public class ReflectionExplorer : IAssetExplorer
    {
        public IEnumerable<Asset> Discover(Asset root, IFilterContext filter)
        {
            if (root.Id.Type != AssetType.Assembly)
                throw new ArgumentException("Only AssetType.Assembly supported.", "root");

            ExceptionDispatchInfo exception = null;
            
            ConcurrentQueue<Asset> queue = new ConcurrentQueue<Asset>();
            using(BlockingCollection<Asset> blocking = new BlockingCollection<Asset>(queue))
            {
                Action<object> func =
                    a =>
                    {
                        try
                        {
                            Discover(((Asset)a).GetAssembly(), blocking, filter);
                        }
                        catch (Exception ex)
                        {
                            exception = ExceptionDispatchInfo.Capture(ex);
                        }
                        blocking.CompleteAdding();
                    };


                using(Task task = Task.Factory.StartNew(func, root))
                {
                    foreach (var asset in blocking.GetConsumingEnumerable())
                        yield return asset;

                    task.Wait();

                    if (exception != null)
                        exception.Throw();
                }
            }
        }

        private void Discover(Assembly assembly, BlockingCollection<Asset> collection, IFilterContext filter)
        {
            TraceSources.GeneratorSource.TraceEvent(TraceEventType.Start, 0, "Discovering assets");

            Asset assemblyAsset = ReflectionServices.GetAsset(assembly);
            if (filter.IsFiltered(assemblyAsset))
                return;

            collection.Add(assemblyAsset);

            HashSet<Asset> distinctSet = new HashSet<Asset>();
            foreach (Type type in assembly.GetTypes())
            {
                // check if type survives filtering
                AssetIdentifier typeAssetId = AssetIdentifier.FromMemberInfo(type);
                Asset typeAsset = new Asset(typeAssetId, type);

                if (filter.IsFiltered(typeAsset))
                    continue;

                /* type was not filtered */
                TraceSources.GeneratorSource.TraceEvent(TraceEventType.Information, 0, "{0}", typeAsset.Id.AssetId);

                // generate namespace hierarchy
                if (!string.IsNullOrEmpty(type.Namespace))
                {
                    Version nsVersion = type.Module.Assembly.GetName().Version;

                    string[] fragments = type.Namespace.Split('.');
                    for (int i = fragments.Length; i > 0; i--)
                    {
                        string ns = string.Join(".", fragments, 0, i);
                        AssetIdentifier nsAssetId = AssetIdentifier.FromNamespace(ns, nsVersion);
                        NamespaceInfo nsInfo = new NamespaceInfo(type.Assembly, ns);
                        Asset nsAsset = new Asset(nsAssetId, nsInfo);
                        if (distinctSet.Add(nsAsset))
                            collection.Add(nsAsset);
                    }
                }

                if (distinctSet.Add(typeAsset))
                    collection.Add(typeAsset);

                MemberInfo[] members = type.GetMembers(BindingFlags.Instance |
                                                       BindingFlags.Static |
                                                       BindingFlags.Public |
                                                       BindingFlags.NonPublic);

                foreach (MemberInfo member in members)
                {
                    Asset memberAsset = ReflectionServices.GetAsset(member);
                    if (filter.IsFiltered(memberAsset))
                        continue;

                    TraceSources.GeneratorSource.TraceEvent(TraceEventType.Information,
                                                            0,
                                                            "{0}",
                                                            memberAsset.Id.AssetId);
                    if (distinctSet.Add(memberAsset))
                        collection.Add(memberAsset);
                }
            }
            TraceSources.GeneratorSource.TraceEvent(TraceEventType.Stop, 0, "Discovering assets");
        }
    }
}