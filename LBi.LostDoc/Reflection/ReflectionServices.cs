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
using System.Reflection;

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

        public static Asset GetAsset(MemberInfo member)
        {
            return new Asset(AssetIdentifier.FromMemberInfo(member), member);
        }
    }
}