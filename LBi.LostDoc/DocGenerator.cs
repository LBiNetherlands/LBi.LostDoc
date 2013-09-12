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
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Enrichers;
using LBi.LostDoc.Filters;
using LBi.LostDoc.Reflection;

namespace LBi.LostDoc
{
    public class DocGenerator : IDisposable
    {
        private readonly List<string> _assemblyPaths;
        private readonly List<IEnricher> _enrichers;
        private readonly List<IAssetFilter> _filters;
        private readonly ObjectCache _cache;
        private readonly CompositionContainer _container;
        private Assembly[] _assemblies;

        public DocGenerator(CompositionContainer container)
        {
            this._assemblyPaths = new List<string>();
            this._filters = new List<IAssetFilter>();
            this._enrichers = new List<IEnricher>();
            this.Enrichers.Add(new AttributeDataEnricher());
            this.AssetFilters.Add(new EnumMetadataFilter());
            this._cache = new MemoryCache("DocGeneratorCache");
            this._container = container;
        }

        public List<IAssetFilter> AssetFilters
        {
            get { return this._filters; }
        }

        public List<IEnricher> Enrichers
        {
            get { return this._enrichers; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public void AddAssembly(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            this._assemblyPaths.Add(path);
        }

        public XDocument Generate()
        {
            TraceSources.GeneratorSource.TraceEvent(TraceEventType.Verbose, 0, "Enrichers:");

            for (int i = 0; i < this.Enrichers.Count; i++)
            {
                IEnricher enricher = this.Enrichers[i];
                TraceSources.GeneratorSource.TraceEvent(TraceEventType.Verbose,
                                                        0,
                                                        "[{0}] {1}",
                                                        i,
                                                        enricher.GetType().FullName);
            }

            TraceSources.GeneratorSource.TraceEvent(TraceEventType.Verbose, 0, "Filter:");
            for (int i = 0; i < this.AssetFilters.Count; i++)
            {
                TraceSources.GeneratorSource.TraceEvent(TraceEventType.Verbose,
                                                        0,
                                                        "[{0}] {1}",
                                                        i,
                                                        this.AssetFilters[i].GetType().FullName);
            }

            XDocument ret = new XDocument();

            IAssemblyLoader assemblyLoader = new ReflectionOnlyAssemblyLoader(
                this._cache,
                this._assemblyPaths.Select(Path.GetDirectoryName));
            
            this._assemblies = this._assemblyPaths.Select(assemblyLoader.LoadFrom).ToArray();

            XNamespace defaultNs = string.Empty;
            // pass in assemblyLoader instead
            IAssetResolver assetResolver = new AssetResolver(assemblyLoader);

            // collect phase zero assets
            List<AssetIdentifier> assets = this.DiscoverAssets(assetResolver, this._assemblies).ToList();

            // initiate output document creation
            ret.Add(new XElement(defaultNs + "bundle"));


            IProcessingContext pctx = new ProcessingContext(this._cache,
                                                            this._container,
                                                            this._filters,
                                                            assemblyLoader,
                                                            assetResolver,
                                                            ret.Root,
                                                            null,
                                                            -1);

            foreach (IEnricher enricher in this._enrichers)
                enricher.RegisterNamespace(pctx);

            // asset related classes
            HashSet<AssetIdentifier> emittedAssets = new HashSet<AssetIdentifier>();

            TraceSources.GeneratorSource.TraceEvent(TraceEventType.Start, 0, "Generating document");

            int phase = 0;
            HashSet<AssetIdentifier> referencedAssets = new HashSet<AssetIdentifier>();

            long lastProgressOutput = Stopwatch.GetTimestamp();
            // main output loop
            while (assets.Count > 0)
            {
                int phaseAssetCount = 0;
                using (TraceSources.GeneratorSource.TraceActivity("Phase {0} ({1:N0} assets)", phase, assets.Count))
                {
                    foreach (AssetIdentifier asset in assets)
                    {
                        // skip already emitted assets
                        if (!emittedAssets.Add(asset))
                            continue;

                        phaseAssetCount++;

                        if (((Stopwatch.GetTimestamp() - lastProgressOutput)/(double) Stopwatch.Frequency) > 5.0)
                        {
                            TraceSources.GeneratorSource.TraceEvent(TraceEventType.Information, 0,
                                                                    "Phase {0} progress {1:P1} ({2:N0}/{3:N0})",
                                                                    phase,
                                                                    phaseAssetCount/(double) assets.Count,
                                                                    phaseAssetCount,
                                                                    assets.Count);

                            lastProgressOutput = Stopwatch.GetTimestamp();
                        }

                        TraceSources.GeneratorSource.TraceEvent(TraceEventType.Verbose, 0, "Generating {0}", asset);

                        // get hierarchy
                        LinkedList<AssetIdentifier> hierarchy = new LinkedList<AssetIdentifier>();

                        foreach (AssetIdentifier assetIdentifier in assetResolver.GetAssetHierarchy(asset))
                            hierarchy.AddFirst(assetIdentifier);

                        if (hierarchy.First != null)
                        {
                            this.BuildHierarchy(assetResolver,
                                                ret.Root,
                                                hierarchy.First,
                                                asset,
                                                referencedAssets,
                                                emittedAssets,
                                                phase);
                        }
                    }


                    ++phase;
                    assets.Clear();
                    referencedAssets.ExceptWith(emittedAssets);
                    assets = referencedAssets.ToList();
                    referencedAssets.Clear();
                }
            }

            TraceSources.GeneratorSource.TraceEvent(TraceEventType.Stop, 0, "Generating document");
            return ret;
        }


        private IEnumerable<AssetIdentifier> DiscoverAssets(IAssetResolver assetResolver,
                                                            IEnumerable<Assembly> assemblies)
        {
            TraceSources.GeneratorSource.TraceEvent(TraceEventType.Start, 0, "Discovering assets");
            HashSet<AssetIdentifier> distinctSet = new HashSet<AssetIdentifier>();
            IFilterContext filterContext = new FilterContext(this._cache, this._container, assetResolver, FilterState.Discovery);

            // find and filter all types from all assemblies 
            foreach (Assembly asm in assemblies)
            {
                foreach (Type t in asm.GetTypes())
                {
                    // check if type survives filtering
                    AssetIdentifier typeAsset = AssetIdentifier.FromMemberInfo(t);

                    if (this.IsFiltered(filterContext, typeAsset)) 
                        continue;

                    /* type was not filtered */
                    TraceSources.GeneratorSource.TraceEvent(TraceEventType.Information, 0, "{0}", typeAsset.AssetId);

                    // generate namespace hierarchy
                    if (!string.IsNullOrEmpty(t.Namespace))
                    {
                        Version nsVersion = t.Module.Assembly.GetName().Version;

                        string[] fragments = t.Namespace.Split('.');
                        for (int i = fragments.Length; i > 0; i--)
                        {
                            string ns = string.Join(".", fragments, 0, i);
                            AssetIdentifier nsAsset = AssetIdentifier.FromNamespace(ns, nsVersion);
                            if (distinctSet.Add(nsAsset))
                                yield return nsAsset;
                        }
                    }

                    if (distinctSet.Add(typeAsset))
                        yield return typeAsset;

                    MemberInfo[] members = t.GetMembers(BindingFlags.Instance |
                                                        BindingFlags.Static |
                                                        BindingFlags.Public |
                                                        BindingFlags.NonPublic);

                    foreach (MemberInfo member in members)
                    {
                        AssetIdentifier memberAsset = AssetIdentifier.FromMemberInfo(member);
                        if (this.IsFiltered(filterContext, memberAsset)) 
                            continue;

                        TraceSources.GeneratorSource.TraceEvent(TraceEventType.Information,
                                                                0,
                                                                "{0}",
                                                                memberAsset.AssetId);
                        if (distinctSet.Add(memberAsset))
                            yield return memberAsset;
                    }
                }

                yield return AssetIdentifier.FromAssembly(asm);
            }

            TraceSources.GeneratorSource.TraceEvent(TraceEventType.Stop, 0, "Discovering assets");
        }

        private bool IsFiltered(IFilterContext filterContext, AssetIdentifier typeAsset)
        {
            bool filtered = false;
            foreach (IAssetFilter filter in this.AssetFilters)
            {
                if (filter.Filter(filterContext, typeAsset))
                {
                    filtered = true;
                    TraceSources.GeneratorSource.TraceEvent(TraceEventType.Verbose,
                                                            0,
                                                            "{0} - Filtered by {1}",
                                                            typeAsset.AssetId, filter);

                    break;
                }
            }
            return filtered;
        }

        private void BuildHierarchy(IAssetResolver assetResolver,
                                    XElement parentNode,
                                    LinkedListNode<AssetIdentifier> hierarchy,
                                    AssetIdentifier asset,
                                    HashSet<AssetIdentifier> references,
                                    HashSet<AssetIdentifier> emittedAssets,
                                    int phase)
        {
            if (hierarchy == null)
                return;

            IAssemblyLoader assemblyLoader =
                new ReflectionOnlyAssemblyLoader(this._cache,
                                                 this._assemblyPaths.Select(Path.GetDirectoryName));


            AssetIdentifier aid = hierarchy.Value;
            IProcessingContext pctx = new ProcessingContext(this._cache,
                                                            this._container,
                                                            this._filters,
                                                            assemblyLoader,
                                                            assetResolver,
                                                            parentNode,
                                                            references,
                                                            phase);

            XElement newElement;

            // add asset to list of generated assets
            emittedAssets.Add(aid);

            // dispatch depending on type
            switch (aid.Type)
            {
                case AssetType.Namespace:
                    newElement = parentNode.XPathSelectElement(string.Format("namespace[@assetId = '{0}']", aid));
                    if (newElement == null)
                        newElement = this.GenerateNamespaceElement(pctx, aid);
                    break;
                case AssetType.Type:
                    newElement = parentNode.XPathSelectElement(string.Format("*[@assetId = '{0}']", aid));
                    if (newElement == null)
                        newElement = this.GenerateTypeElement(pctx, aid);
                    break;
                case AssetType.Method:
                    newElement = parentNode.XPathSelectElement(string.Format("*[@assetId = '{0}']", aid));
                    if (newElement == null)
                        newElement = this.GenerateMethodElement(pctx, aid);
                    break;
                case AssetType.Field:
                    newElement = parentNode.XPathSelectElement(string.Format("field[@assetId = '{0}']", aid));
                    if (newElement == null)
                        newElement = this.GenerateFieldElement(pctx, aid);
                    break;
                case AssetType.Event:
                    newElement = parentNode.XPathSelectElement(string.Format("event[@assetId = '{0}']", aid));
                    if (newElement == null)
                        newElement = this.GenerateEventElement(pctx, aid);
                    break;
                case AssetType.Property:
                    newElement = parentNode.XPathSelectElement(string.Format("property[@assetId = '{0}']", aid));
                    if (newElement == null)
                        newElement = this.GeneratePropertyElement(pctx, aid);
                    break;
                case AssetType.Assembly:
                    newElement = parentNode.XPathSelectElement(string.Format("assembly[@assetId = '{0}']", aid));
                    if (newElement == null)
                        newElement = this.GenerateAssemblyElement(pctx, aid);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            this.BuildHierarchy(assetResolver, newElement, hierarchy.Next, asset, references, emittedAssets, phase);
        }


        public static void GenerateTypeRef(IProcessingContext context, Type pType, string attrName = null)
        {
            if (pType.IsArray)
            {
                var arrayElem = new XElement("arrayOf", new XAttribute("rank", pType.GetArrayRank()));
                context.Element.Add(arrayElem);
                GenerateTypeRef(context.Clone(arrayElem), pType.GetElementType());
            }
            else
            {
                if (pType.IsGenericParameter)
                    context.Element.Add(new XAttribute("param", pType.Name));
                else if (pType.IsGenericType)
                {
                    AssetIdentifier aid = AssetIdentifier.FromType(pType.GetGenericTypeDefinition());
                    context.AddReference(aid);

                    context.Element.Add(new XAttribute(attrName ?? "type", aid));
                    foreach (Type genArg in pType.GetGenericArguments())
                    {
                        XElement argElem = new XElement("with");
                        GenerateTypeRef(context.Clone(argElem), genArg);
                        context.Element.Add(argElem);
                    }
                }
                else
                {
                    AssetIdentifier aid = AssetIdentifier.FromMemberInfo(pType);
                    context.AddReference(aid);

                    context.Element.Add(new XAttribute(attrName ?? "type", aid));
                }
            }
        }

        private XElement GenerateAssemblyElement(IProcessingContext context, AssetIdentifier assetId)
        {
            Assembly asm = (Assembly)context.AssetResolver.Resolve(assetId);

            IEnumerable<XElement> references =
                asm.GetReferencedAssemblies()
                   .Select(an => new XElement("references",
                                              new XAttribute("assembly",
                                                             AssetIdentifier.FromAssembly(context.AssemblyLoader.Load(an.FullName)))));

            XElement ret = new XElement("assembly",
                                        new XAttribute("name", asm.GetName().Name),
                                        new XAttribute("filename", asm.ManifestModule.Name),
                                        new XAttribute("assetId", assetId),
                                        new XAttribute("phase", context.Phase),
                                        references);

            context.Element.Add(ret);

            foreach (IEnricher enricher in this._enrichers)
                enricher.EnrichAssembly(context.Clone(ret), asm);

            return ret;
        }

        private XElement GenerateNamespaceElement(IProcessingContext context, AssetIdentifier assetId)
        {
            string ns = (string)context.AssetResolver.Resolve(assetId);
            var ret = new XElement("namespace",
                                   new XAttribute("name", ns),
                                   new XAttribute("assetId", assetId),
                                   new XAttribute("phase", context.Phase));

            context.Element.Add(ret);

            foreach (IEnricher enricher in this._enrichers)
                enricher.EnrichNamespace(context.Clone(ret), ns);

            return ret;
        }

        private XElement GenerateTypeElement(IProcessingContext context, AssetIdentifier assetId)
        {
            XElement ret;
            Type type = (Type)context.AssetResolver.Resolve(assetId);


            string elemName;

            if (type.IsClass)
                elemName = "class";
            else if (type.IsEnum)
                elemName = "enum";
            else if (type.IsValueType)
                elemName = "struct";
            else if (type.IsInterface)
                elemName = "interface";
            else
                throw new ArgumentException("Unknown asset type: " + assetId.Type.ToString(), "assetId");

            ret = new XElement(elemName,
                               new XAttribute("name", type.Name),
                               new XAttribute("assetId", assetId),
                               new XAttribute("phase", context.Phase));

            if (type.IsEnum)
            {
                AssetIdentifier aid = AssetIdentifier.FromType(type.GetEnumUnderlyingType());
                ret.Add(new XAttribute("underlyingType", aid));
                context.AddReference(aid);
            }


            if (!type.IsInterface && type.IsAbstract)
                ret.Add(new XAttribute("isAbstract", XmlConvert.ToString(type.IsAbstract)));

            if (!type.IsVisible || type.IsNested && type.IsNestedAssembly)
                ret.Add(new XAttribute("isInternal", XmlConvert.ToString(true)));

            if (type.IsPublic || type.IsNested && type.IsNestedPublic)
                ret.Add(new XAttribute("isPublic", XmlConvert.ToString(true)));

            if (type.IsNested && type.IsNestedPrivate)
                ret.Add(new XAttribute("isPrivate", XmlConvert.ToString(true)));

            if (type.IsNested && type.IsNestedFamily)
                ret.Add(new XAttribute("isProtected", XmlConvert.ToString(true)));

            if (type.IsNested && type.IsNestedFamANDAssem)
                ret.Add(new XAttribute("isProtectedAndInternal", XmlConvert.ToString(true)));

            if (type.IsNested && type.IsNestedFamORAssem)
                ret.Add(new XAttribute("isProtectedOrInternal", XmlConvert.ToString(true)));

            if (type.IsClass && type.IsSealed)
                ret.Add(new XAttribute("isSealed", XmlConvert.ToString(true)));

            if (type.BaseType != null)
            {
                AssetIdentifier baseAid = AssetIdentifier.FromType(type.BaseType);
                if (!context.IsFiltered(baseAid))
                {
                    var inheritsElem = new XElement("inherits");
                    ret.Add(inheritsElem);
                    GenerateTypeRef(context.Clone(inheritsElem), type.BaseType);
                }
            }

            if (type.ContainsGenericParameters)
            {
                Type[] typeParams = type.GetGenericArguments();
                foreach (Type tp in typeParams)
                {
                    this.GenerateTypeParamElement(context.Clone(ret), type, tp);
                }
            }

            if (type.IsClass)
            {
                foreach (Type interfaceType in type.GetInterfaces())
                {
                    InterfaceMapping mapping = type.GetInterfaceMap(interfaceType);
                    if (mapping.TargetType == type)
                    {
                        AssetIdentifier interfaceAssetId = AssetIdentifier.FromType(interfaceType.IsGenericType
                                                                                        ? interfaceType.GetGenericTypeDefinition()
                                                                                        : interfaceType);
                        if (!context.IsFiltered(interfaceAssetId))
                        {
                            var implElement = new XElement("implements");
                            ret.Add(implElement);
                            GenerateTypeRef(context.Clone(implElement), interfaceType, "interface");
                        }
                    }
                }
            }


            foreach (IEnricher enricher in this._enrichers)
                enricher.EnrichType(context.Clone(ret), type);


            context.Element.Add(ret);

            return ret;
        }

        private void GenerateTypeParamElement(IProcessingContext context, MemberInfo mInfo, Type tp)
        {
            var tpElem = new XElement("typeparam", new XAttribute("name", tp.Name));

            context.Element.Add(tpElem);

            foreach (Type constraint in tp.GetGenericParameterConstraints())
            {
                var ctElement = new XElement("constraint");
                tpElem.Add(ctElement);
                GenerateTypeRef(context.Clone(ctElement), constraint);
            }

            // enrich typeparam
            foreach (IEnricher enricher in this.Enrichers)
                enricher.EnrichTypeParameter(context.Clone(tpElem), tp);
        }

        private bool IsOperator(MethodInfo method)
        {
            if (!method.IsSpecialName || !method.Name.StartsWith("op_", StringComparison.Ordinal))
                return false;

            switch (method.Name)
            {
                // unary ops
                case "op_Decrement":
                case "op_Increment":
                case "op_UnaryNegation":
                case "op_UnaryPlus":
                case "op_LogicalNot":
                case "op_True":
                case "op_False":
                case "op_AddressOf":
                case "op_OnesComplement":
                case "op_PointerDereference":

                // conversion
                case "op_Implicit":
                case "op_Explicit":

                // binary ops
                case "op_Addition":
                case "op_Subtraction":
                case "op_Multiply":
                case "op_Division":
                case "op_Modulus":
                case "op_ExclusiveOr":
                case "op_BitwiseAnd":
                case "op_BitwiseOr":
                case "op_LogicalAnd":
                case "op_LogicalOr":
                case "op_Assign":
                case "op_LeftShift":
                case "op_RightShift":
                case "op_SignedRightShift":
                case "op_UnsignedRightShift":
                case "op_Equality":
                case "op_GreaterThan":
                case "op_LessThan":
                case "op_Inequality":
                case "op_GreaterThanOrEqual":
                case "op_LessThanOrEqual":
                case "op_UnsignedRightShiftAssignment":
                case "op_MemberSelection":
                case "op_RightShiftAssignment":
                case "op_MultiplicationAssignment":
                case "op_PointerToMemberSelection":
                case "op_SubtractionAssignment":
                case "op_ExclusiveOrAssignment":
                case "op_LeftShiftAssignment":
                case "op_ModulusAssignment":
                case "op_AdditionAssignment":
                case "op_BitwiseAndAssignment":
                case "op_BitwiseOrAssignment":
                case "op_Comma":
                case "op_DivisionAssignment":
                    return true;
            }

            return false;
        }

        private XElement GenerateMethodElement(IProcessingContext context, AssetIdentifier assetId)
        {
            MethodBase mBase = (MethodBase)context.AssetResolver.Resolve(assetId);

            if (mBase is ConstructorInfo)
                return this.GenerateConstructorElement(context, assetId);

            MethodInfo mInfo = (MethodInfo)mBase;

            string elemName;

            if (this.IsOperator(mInfo))
                elemName = "operator";
            else
                elemName = "method";


            XElement ret = new XElement(elemName,
                                        new XAttribute("name", mInfo.Name),
                                        new XAttribute("assetId", assetId),
                                        new XAttribute("phase", context.Phase));

            context.Element.Add(ret);
            Type declaringType = mInfo.DeclaringType;

            if (declaringType.IsGenericType && !declaringType.IsGenericTypeDefinition)
                declaringType = declaringType.GetGenericTypeDefinition();

            MethodInfo realMethodInfo =
                declaringType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                         BindingFlags.Static).Single(
                                                                     mi =>
                                                                     mi.MetadataToken == mInfo.MetadataToken &&
                                                                     mi.Module == mInfo.Module);

            AssetIdentifier declaredAs = AssetIdentifier.FromMemberInfo(realMethodInfo);

            if (declaringType != mInfo.ReflectedType)
            {
                ret.Add(new XAttribute("declaredAs", declaredAs));
                context.AddReference(declaredAs);
            }
            else if (realMethodInfo.GetBaseDefinition() != realMethodInfo)
            {
                MethodInfo baseMethod = realMethodInfo.GetBaseDefinition();
                if (baseMethod.ReflectedType.IsGenericType)
                {
                    Type realTypeBase = baseMethod.ReflectedType.GetGenericTypeDefinition();
                    MethodInfo[] allMethods = realTypeBase.GetMethods(BindingFlags.Public |
                                                                      BindingFlags.NonPublic |
                                                                      BindingFlags.Instance |
                                                                      BindingFlags.Static);

                    baseMethod = allMethods.Single(m => m.Module == baseMethod.Module &&
                                                        m.MetadataToken == baseMethod.MetadataToken);
                }

                declaredAs = AssetIdentifier.FromMemberInfo(baseMethod);
                ret.Add(new XAttribute("overrides", declaredAs));
                context.AddReference(declaredAs);
            }

            this.GenerateImplementsElement(context.Clone(ret), mInfo);

            this.GenerateAccessModifiers(ret, mInfo);

            if (mInfo.ContainsGenericParameters)
            {
                Type[] typeParams = mInfo.GetGenericArguments();
                foreach (Type tp in typeParams)
                    this.GenerateTypeParamElement(context.Clone(ret), mInfo, tp);
            }

            foreach (IEnricher item in this.Enrichers)
                item.EnrichMethod(context.Clone(ret), mInfo);

            ParameterInfo[] methodParams = mInfo.GetParameters();
            this.GenerateParameterElements(context.Clone(ret), methodParams);

            if (mInfo.ReturnType != typeof(void))
            {
                XElement retElem = new XElement("returns");

                GenerateTypeRef(context.Clone(retElem), mInfo.ReturnType);

                foreach (IEnricher item in this.Enrichers)
                    item.EnrichReturnValue(context.Clone(retElem), mInfo);

                ret.Add(retElem);
            }

            return ret;
        }

        private void GenerateImplementsElement(IProcessingContext context, MemberInfo mInfo)
        {
            Type declaringType = mInfo.DeclaringType;

            if (declaringType.IsGenericType && !declaringType.IsGenericTypeDefinition)
                declaringType = declaringType.GetGenericTypeDefinition();

            if (!declaringType.IsInterface)
            {
                Type[] interfaces = declaringType.GetInterfaces();
                foreach (Type ifType in interfaces)
                {
                    if (context.IsFiltered(AssetIdentifier.FromMemberInfo(ifType)))
                        continue;

                    InterfaceMapping ifMap = declaringType.GetInterfaceMap(ifType);
                    if (ifMap.TargetType != declaringType)
                        continue;

                    var targetMethod = ifMap.TargetMethods.SingleOrDefault(mi => mi.MetadataToken == mInfo.MetadataToken &&
                                                                                 mi.Module == mInfo.Module);

                    if (targetMethod != null)
                    {
                        int mIx = Array.IndexOf(ifMap.TargetMethods, targetMethod);

                        AssetIdentifier miAid;
                        if (ifMap.InterfaceMethods[mIx].DeclaringType.IsGenericType)
                        {
                            Type declType = ifMap.InterfaceMethods[mIx].DeclaringType.GetGenericTypeDefinition();
                            MethodInfo[] allMethods = declType.GetMethods(BindingFlags.Public |
                                                                          BindingFlags.NonPublic |
                                                                          BindingFlags.Instance |
                                                                          BindingFlags.Static);

                            MethodInfo memberInfo = allMethods.Single(mi =>
                                                                      mi.MetadataToken == ifMap.InterfaceMethods[mIx].MetadataToken &&
                                                                      mi.Module == ifMap.InterfaceMethods[mIx].Module);
                            miAid = AssetIdentifier.FromMemberInfo(memberInfo);
                        }
                        else
                        {
                            miAid = AssetIdentifier.FromMemberInfo(ifMap.InterfaceMethods[mIx]);
                        }

                        context.Element.Add(new XElement("implements", new XAttribute("member", miAid)));
                        context.AddReference(miAid);
                    }
                }
            }
        }

        private void GenerateParameterElements(IProcessingContext context, ParameterInfo[] methodParams)
        {
            foreach (ParameterInfo item in methodParams)
            {
                XElement pElem = new XElement("param", new XAttribute("name", item.Name));

                Type pType;
                if (item.ParameterType.IsByRef)
                    pType = item.ParameterType.GetElementType();
                else
                    pType = item.ParameterType;

                if (item.ParameterType.IsByRef && item.IsOut && !item.IsIn)
                    pElem.Add(new XAttribute("isOut", true));
                else if (item.ParameterType.IsByRef)
                    pElem.Add(new XAttribute("isRef", true));


                GenerateTypeRef(context.Clone(pElem), pType);

                foreach (IEnricher enricher in this.Enrichers)
                    enricher.EnrichParameter(context.Clone(pElem), item);

                context.Element.Add(pElem);
            }
        }

        private void GenerateAccessModifiers(XElement ret, MethodBase methodInfo)
        {
            if (!methodInfo.DeclaringType.IsInterface)
            {
                if (methodInfo.IsAbstract)
                    ret.Add(new XAttribute("isAbstract", XmlConvert.ToString(methodInfo.IsAbstract)));

                if (methodInfo.IsVirtual && !methodInfo.IsFinal)
                    ret.Add(new XAttribute("isVirtual", XmlConvert.ToString(methodInfo.IsVirtual)));

                if (methodInfo.IsStatic)
                    ret.Add(new XAttribute("isStatic", XmlConvert.ToString(methodInfo.IsStatic)));

                if (methodInfo.IsPublic)
                    ret.Add(new XAttribute("isPublic", XmlConvert.ToString(methodInfo.IsPublic)));

                if (methodInfo.IsPrivate)
                    ret.Add(new XAttribute("isPrivate", XmlConvert.ToString(methodInfo.IsPrivate)));

                if (methodInfo.IsFamily)
                    ret.Add(new XAttribute("isProtected", XmlConvert.ToString(methodInfo.IsFamily)));

                if (methodInfo.IsAssembly)
                    ret.Add(new XAttribute("isInternal", XmlConvert.ToString(methodInfo.IsAssembly)));

                if (methodInfo.IsFamilyOrAssembly)
                    ret.Add(new XAttribute("isProtectedOrInternal", XmlConvert.ToString(methodInfo.IsFamilyOrAssembly)));

                if (methodInfo.IsFamilyAndAssembly)
                    ret.Add(new XAttribute("isProtectedAndInternal", XmlConvert.ToString(methodInfo.IsFamilyAndAssembly)));

                if (methodInfo.IsFinal)
                    ret.Add(new XAttribute("isSealed", XmlConvert.ToString(methodInfo.IsFinal)));

                if (methodInfo.IsSpecialName)
                    ret.Add(new XAttribute("isSpecialName", XmlConvert.ToString(methodInfo.IsSpecialName)));
            }
        }

        private XElement GenerateConstructorElement(IProcessingContext context, AssetIdentifier assetId)
        {
            ConstructorInfo constructorInfo = (ConstructorInfo)context.AssetResolver.Resolve(assetId);
            XElement ret = new XElement("constructor",
                                        new XAttribute("assetId", assetId),
                                        new XAttribute("phase", context.Phase));

            if (constructorInfo.IsStatic)
                ret.Add(new XAttribute("isStatic", XmlConvert.ToString(constructorInfo.IsStatic)));

            if (constructorInfo.IsPublic)
                ret.Add(new XAttribute("isPublic", XmlConvert.ToString(constructorInfo.IsPublic)));

            if (constructorInfo.IsPrivate)
                ret.Add(new XAttribute("isPrivate", XmlConvert.ToString(constructorInfo.IsPrivate)));

            if (constructorInfo.IsFamily)
                ret.Add(new XAttribute("isProtected", XmlConvert.ToString(constructorInfo.IsFamily)));

            context.Element.Add(ret);

            foreach (IEnricher item in this.Enrichers)
                item.EnrichConstructor(context.Clone(ret), constructorInfo);

            ParameterInfo[] methodParams = constructorInfo.GetParameters();
            this.GenerateParameterElements(context.Clone(ret), methodParams);

            return ret;
        }

        private XElement GenerateFieldElement(IProcessingContext context, AssetIdentifier assetId)
        {
            object resolve = context.AssetResolver.Resolve(assetId);
            FieldInfo fieldInfo = (FieldInfo)resolve;
            XElement ret = new XElement("field",
                                        new XAttribute("name", fieldInfo.Name),
                                        new XAttribute("assetId", assetId),
                                        new XAttribute("phase", context.Phase));

            if (fieldInfo.IsStatic)
                ret.Add(new XAttribute("isStatic", XmlConvert.ToString(fieldInfo.IsStatic)));

            if (fieldInfo.IsPublic)
                ret.Add(new XAttribute("isPublic", XmlConvert.ToString(fieldInfo.IsPublic)));

            if (fieldInfo.IsPrivate)
                ret.Add(new XAttribute("isPrivate", XmlConvert.ToString(fieldInfo.IsPrivate)));

            if (fieldInfo.IsFamily)
                ret.Add(new XAttribute("isProtected", XmlConvert.ToString(fieldInfo.IsFamily)));

            if (fieldInfo.IsFamilyOrAssembly)
                ret.Add(new XAttribute("isProtectedOrInternal", XmlConvert.ToString(fieldInfo.IsFamilyOrAssembly)));

            if (fieldInfo.IsFamilyAndAssembly)
                ret.Add(new XAttribute("isProtectedAndInternal", XmlConvert.ToString(fieldInfo.IsFamilyAndAssembly)));

            if (fieldInfo.IsSpecialName)
                ret.Add(new XAttribute("isSpecialName", XmlConvert.ToString(fieldInfo.IsSpecialName)));


            GenerateTypeRef(context.Clone(ret), fieldInfo.FieldType);

            context.Element.Add(ret);

            foreach (IEnricher item in this.Enrichers)
                item.EnrichField(context.Clone(ret), fieldInfo);

            return ret;
        }

        private XElement GenerateEventElement(IProcessingContext context, AssetIdentifier assetId)
        {
            EventInfo eventInfo = (EventInfo)context.AssetResolver.Resolve(assetId);
            XElement ret = new XElement("event",
                                        new XAttribute("name", eventInfo.Name),
                                        new XAttribute("assetId", assetId),
                                        new XAttribute("phase", context.Phase));


            GenerateTypeRef(context.Clone(ret), eventInfo.EventHandlerType);

            MethodInfo addMethod = eventInfo.GetAddMethod(true);
            MethodInfo removeMethod = eventInfo.GetRemoveMethod(true);
            if (addMethod != null)
            {
                var addElem = new XElement("add");
                if (addMethod.IsPublic)
                    addElem.Add(new XAttribute("isPublic", XmlConvert.ToString(addMethod.IsPublic)));

                if (addMethod.IsPrivate)
                    addElem.Add(new XAttribute("isPrivate", XmlConvert.ToString(addMethod.IsPrivate)));

                if (addMethod.IsFamily)
                    addElem.Add(new XAttribute("isProtected", XmlConvert.ToString(addMethod.IsFamily)));

                ret.Add(addElem);
            }

            if (removeMethod != null)
            {
                var removeElem = new XElement("remove");
                if (removeMethod.IsPublic)
                    removeElem.Add(new XAttribute("isPublic", XmlConvert.ToString(removeMethod.IsPublic)));

                if (removeMethod.IsPrivate)
                    removeElem.Add(new XAttribute("isPrivate", XmlConvert.ToString(removeMethod.IsPrivate)));

                if (removeMethod.IsFamily)
                    removeElem.Add(new XAttribute("isProtected", XmlConvert.ToString(removeMethod.IsFamily)));

                ret.Add(removeElem);
            }

            context.Element.Add(ret);

            this.GenerateImplementsElement(context.Clone(ret), eventInfo);

            foreach (IEnricher item in this.Enrichers)
                item.EnrichEvent(context.Clone(ret), eventInfo);

            return ret;
        }

        private XElement GeneratePropertyElement(IProcessingContext context, AssetIdentifier assetId)
        {
            PropertyInfo propInfo = (PropertyInfo)context.AssetResolver.Resolve(assetId);
            XElement ret = new XElement("property",
                                        new XAttribute("name", propInfo.Name),
                                        new XAttribute("assetId", assetId),
                                        new XAttribute("phase", context.Phase));

            GenerateTypeRef(context.Clone(ret), propInfo.PropertyType);

            ParameterInfo[] pInfos = propInfo.GetIndexParameters();
            this.GenerateParameterElements(context.Clone(ret), pInfos);

            MethodInfo setMethod = propInfo.GetSetMethod(true);
            MethodInfo getMethod = propInfo.GetGetMethod(true);

            if ((setMethod ?? getMethod).IsAbstract)
                ret.Add(new XAttribute("isAbstract", XmlConvert.ToString(true)));

            if ((setMethod ?? getMethod).IsVirtual)
                ret.Add(new XAttribute("isVirtual", XmlConvert.ToString(true)));


            const int C_PUBLIC = 10;
            const int C_INTERNAL_OR_PROTECTED = 8;
            const int C_INTERNAL = 6;
            const int C_PROTECTED = 4;
            const int C_INTERNAL_AND_PROTECTED = 2;
            const int C_PRIVATE = 0;

            int leastRestrictiveAccessModifier;

            if (setMethod != null && setMethod.IsPublic || getMethod != null && getMethod.IsPublic)
            {
                ret.Add(new XAttribute("isPublic", XmlConvert.ToString(true)));
                leastRestrictiveAccessModifier = C_PUBLIC;
            }
            else if (setMethod != null && setMethod.IsFamilyOrAssembly ||
                     getMethod != null && getMethod.IsFamilyOrAssembly)
            {
                ret.Add(new XAttribute("isInternalOrProtected", XmlConvert.ToString(true)));
                leastRestrictiveAccessModifier = C_INTERNAL_OR_PROTECTED;
            }
            else if (setMethod != null && setMethod.IsAssembly || getMethod != null && getMethod.IsAssembly)
            {
                ret.Add(new XAttribute("isInternal", XmlConvert.ToString(true)));
                leastRestrictiveAccessModifier = C_INTERNAL;
            }
            else if (setMethod != null && setMethod.IsFamily || getMethod != null && getMethod.IsFamily)
            {
                ret.Add(new XAttribute("isProtected", XmlConvert.ToString(true)));
                leastRestrictiveAccessModifier = C_PROTECTED;
            }
            else if (setMethod != null && setMethod.IsFamilyAndAssembly ||
                     getMethod != null && getMethod.IsFamilyAndAssembly)
            {
                ret.Add(new XAttribute("isInternalAndProtected", XmlConvert.ToString(true)));
                leastRestrictiveAccessModifier = C_INTERNAL_AND_PROTECTED;
            }
            else if (setMethod != null && setMethod.IsPrivate || getMethod != null && getMethod.IsPrivate)
            {
                ret.Add(new XAttribute("isPrivate", XmlConvert.ToString(true)));
                leastRestrictiveAccessModifier = C_PRIVATE;
            }
            else
            {
                throw new InvalidOperationException("What the hell happened here?");
            }

            if (setMethod != null)
            {
                var setElem = new XElement("set");

                if (leastRestrictiveAccessModifier > C_INTERNAL_OR_PROTECTED && setMethod.IsFamilyOrAssembly)
                    setElem.Add(new XAttribute("isInternalOrProtected",
                                               XmlConvert.ToString(setMethod.IsFamilyOrAssembly)));

                if (leastRestrictiveAccessModifier > C_INTERNAL && setMethod.IsAssembly)
                    setElem.Add(new XAttribute("isInternal", XmlConvert.ToString(setMethod.IsAssembly)));

                if (leastRestrictiveAccessModifier > C_PROTECTED && setMethod.IsFamily)
                    setElem.Add(new XAttribute("isProtected", XmlConvert.ToString(setMethod.IsFamily)));

                if (leastRestrictiveAccessModifier > C_INTERNAL_AND_PROTECTED && setMethod.IsFamilyAndAssembly)
                    setElem.Add(new XAttribute("isInternalAndProtected",
                                               XmlConvert.ToString(setMethod.IsFamilyAndAssembly)));

                if (leastRestrictiveAccessModifier > C_PRIVATE && setMethod.IsPrivate)
                    setElem.Add(new XAttribute("isPrivate", XmlConvert.ToString(setMethod.IsPrivate)));

                ret.Add(setElem);
            }

            if (getMethod != null)
            {
                var getElem = new XElement("get");
                if (leastRestrictiveAccessModifier > C_INTERNAL_OR_PROTECTED && getMethod.IsFamilyOrAssembly)
                    getElem.Add(new XAttribute("isInternalOrProtected",
                                               XmlConvert.ToString(getMethod.IsFamilyOrAssembly)));

                if (leastRestrictiveAccessModifier > C_INTERNAL && getMethod.IsAssembly)
                    getElem.Add(new XAttribute("isInternal", XmlConvert.ToString(getMethod.IsAssembly)));

                if (leastRestrictiveAccessModifier > C_PROTECTED && getMethod.IsFamily)
                    getElem.Add(new XAttribute("isProtected", XmlConvert.ToString(getMethod.IsFamily)));

                if (leastRestrictiveAccessModifier > C_INTERNAL_AND_PROTECTED && getMethod.IsFamilyAndAssembly)
                    getElem.Add(new XAttribute("isInternalAndProtected",
                                               XmlConvert.ToString(getMethod.IsFamilyAndAssembly)));

                if (leastRestrictiveAccessModifier > C_PRIVATE && getMethod.IsPrivate)
                    getElem.Add(new XAttribute("isPrivate", XmlConvert.ToString(getMethod.IsPrivate)));

                ret.Add(getElem);
            }


            if (propInfo.IsSpecialName)
                ret.Add(new XAttribute("isSpecialName", XmlConvert.ToString(propInfo.IsSpecialName)));

            context.Element.Add(ret);

            this.GenerateImplementsElement(context.Clone(ret), propInfo);

            foreach (IEnricher item in this.Enrichers)
                item.EnrichProperty(context.Clone(ret), propInfo);

            return ret;
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
            }
        }

        ~DocGenerator()
        {
            this.Dispose(false);
        }
    }
}
