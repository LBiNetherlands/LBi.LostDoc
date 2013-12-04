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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace LBi.LostDoc.Reflection
{
    public static class ReflectionServices
    {
        public static Assembly GetAssembly(this Asset asset)
        {
            switch (asset.Id.Type)
            {
                case AssetType.Namespace:
                    return ((NamespaceInfo)asset.Target).Assembly;
                case AssetType.Type:
                    return ((Type)asset.Target).Assembly;
                case AssetType.Method:
                case AssetType.Field:
                case AssetType.Event:
                case AssetType.Property:
                    return ((MemberInfo)asset.Target).ReflectedType.Assembly;
                case AssetType.Assembly:
                    return (Assembly)asset.Target;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Type GetType(this Asset asset)
        {
            switch (asset.Id.Type)
            {
                case AssetType.Type:
                    return (Type)asset.Target;
                case AssetType.Method:
                case AssetType.Field:
                case AssetType.Event:
                case AssetType.Property:
                    return ((MemberInfo)asset.Target).ReflectedType;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Asset GetAsset(Assembly assembly)
        {
            return new Asset(AssetIdentifier.FromAssembly(assembly), assembly);
        }

        public static Asset GetAsset(Assembly assembly, string ns)
        {
            return new Asset(AssetIdentifier.FromNamespace(ns, assembly.GetName().Version),
                             new NamespaceInfo(assembly, ns));
        }
    }

    public class ReflectionExplorer : IAssetExplorer, IAssetVisitor
    {
        private IAssetFilter[] _filters;
        private BlockingCollection<Asset> _blocking;
        private ExceptionDispatchInfo _exception;

        public IEnumerable<Asset> Discover(Asset root, IAssetFilter[] filters)
        {
            this._filters = filters;
            ConcurrentQueue<Asset> queue = new ConcurrentQueue<Asset>();

            Action<object> func =
                a =>
                {
                    try
                    {
                        ((Asset)a).Visit(this);
                    }
                    catch (Exception ex)
                    {
                        this._exception = ExceptionDispatchInfo.Capture(ex);
                    }
                    this._blocking.CompleteAdding();
                };

            using (this._blocking = new BlockingCollection<Asset>(queue))
            using (Task task = Task.Factory.StartNew(func, root))
            {
                foreach (var asset in _blocking.GetConsumingEnumerable())
                    yield return asset;

                task.Wait();

                if (this._exception != null)
                    this._exception.Throw();
            }
            this._filters = null;
        }

        private IEnumerable<string> DiscoverNamespaces(Assembly assembly, string prefix = "")
        {
            Type[] types = assembly.GetTypes();
            var unique = types.Select(t => t.Namespace)
                              .Where(t => t.StartsWith(prefix) && t.Length > prefix.Length && t[prefix.Length] == '.')
                              .Select(t => t.Substring(prefix.Length + 1))
                              .Distinct(StringComparer.Ordinal);

            var rootNamespaces = unique.Select(n => n.Split('.')[0]).Distinct(StringComparer.Ordinal);

            return rootNamespaces.Select(ns => prefix + ns);
        }

        void IAssetVisitor.VisitAssembly(Asset asset)
        {
            Assembly assembly = asset.GetAssembly();

            foreach (string ns in this.DiscoverNamespaces(assembly))
            {
                this._blocking.Add(ReflectionServices.GetAsset(assembly, ns));
            }

        }

        void IAssetVisitor.VisitNamespace(Asset asset)
        {
            throw new NotImplementedException();
        }

        void IAssetVisitor.VisitType(Asset asset)
        {
            throw new NotImplementedException();
        }

        void IAssetVisitor.VisitField(Asset asset)
        {

        }

        void IAssetVisitor.VisitEvent(Asset asset)
        {

        }

        void IAssetVisitor.VisitProperty(Asset asset)
        {
            throw new NotImplementedException();
        }

        void IAssetVisitor.VisitUnknown(Asset asset)
        {
            throw new NotImplementedException();
        }

        void IAssetVisitor.VisitMethod(Asset asset)
        {
            throw new NotImplementedException();
        }
    }
}