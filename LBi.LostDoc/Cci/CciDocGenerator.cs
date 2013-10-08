using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using LBi.LostDoc.Diagnostics;
using Microsoft.Cci;

namespace LBi.LostDoc.Cci
{
    public class CciDocGenerator : IDocGenerator
    {
        private readonly List<string> _assemblyPaths;
        private readonly List<ICciEnricher> _enrichers;
        private readonly List<ICciAssetFilter> _filters;
        //private readonly CompositionContainer _container;
        //private IAssembly[] _assemblies;
        private readonly ICciAssemblyLoader _assemblyLoader;
        private IMetadataHost _metadataHost;


        public CciDocGenerator(ICciAssemblyLoader assemblyLoader)
        {
            this._assemblyPaths = new List<string>();
            this._assemblyLoader = assemblyLoader;
            this._filters = new List<ICciAssetFilter>();
            this._enrichers = new List<ICciEnricher>();
            this._metadataHost = this._assemblyLoader.Host;
            //this.Enrichers.Add(new AttributeDataEnricher());
            //this.AssetFilters.Add(new EnumMetadataFilter());
            //this._cache = new MemoryCache("DocGeneratorCache");
            //this._container = container;
        }

        public List<ICciAssetFilter> AssetFilters
        {
            get { return this._filters; }
        }

        public List<ICciEnricher> Enrichers
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
                ICciEnricher enricher = this.Enrichers[i];
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

            ICciAssemblyLoader assemblyLoader = new PeAssemblyLoader();

            XDocument ret = new XDocument();

            IAssembly[] assemblies = this._assemblyPaths.Select(assemblyLoader.LoadFrom).ToArray();

            XNamespace defaultNs = string.Empty;
            // pass in assemblyLoader instead
            //IAssetResolver assetResolver = new AssetResolver(assemblyLoader);

            // collect phase zero assets
            List<IDefinition> assets = this.DiscoverAssets(assemblies).ToList();

            // initiate output document creation
            ret.Add(new XElement(defaultNs + "bundle"));


            ICciProcessingContext pctx = new CciProcessingContext(this.AssetFilters, ret.Root, -1);

            foreach (ICciEnricher enricher in this._enrichers)
                enricher.RegisterNamespace(pctx);

            // asset related classes
            HashSet<IDefinition> emittedAssets = new HashSet<IDefinition>();

            TraceSources.GeneratorSource.TraceEvent(TraceEventType.Start, 0, "Generating document");

            int phase = 0;
            HashSet<IDefinition> referencedAssets = new HashSet<IDefinition>();

            AssetHierarchyCollector hierarchyCollector = new AssetHierarchyCollector();

            // main output loop
            while (assets.Count > 0)
            {
                ICciProcessingContext context = pctx.Clone(phase);

                long lastProgressOutput = Stopwatch.GetTimestamp();

                int phaseAssetCount = 0;
                using (TraceSources.GeneratorSource.TraceActivity("Phase {0} ({1:N0} assets)", phase, assets.Count))
                {
                    foreach (IDefinition asset in assets)
                    {
                        // skip already emitted assets
                        if (!emittedAssets.Add(asset))
                            continue;

                        phaseAssetCount++;

                        if (((Stopwatch.GetTimestamp() - lastProgressOutput) / (double)Stopwatch.Frequency) > 5.0)
                        {
                            TraceSources.GeneratorSource.TraceEvent(TraceEventType.Information, 0,
                                                                    "Phase {0} progress {1:P1} ({2:N0}/{3:N0})",
                                                                    phase,
                                                                    phaseAssetCount / (double)assets.Count,
                                                                    phaseAssetCount,
                                                                    assets.Count);

                            lastProgressOutput = Stopwatch.GetTimestamp();
                        }

                        TraceSources.GeneratorSource.TraceEvent(TraceEventType.Verbose, 0, "Generating {0}", asset);

                        // get hierarchy
                        asset.Dispatch(hierarchyCollector);

                        this.BuildHierarchy(context, hierarchyCollector);

                        hierarchyCollector.Clear();
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

        private void BuildHierarchy(ICciProcessingContext context, IEnumerable<IDefinition> hierarchy)
        {
            foreach (IDefinition asset in hierarchy)
            {
                XElement assetElement;
                
                AssetHierarchyBuilder hierarchyBuilder = new AssetHierarchyBuilder();
                hierarchyBuilder.SetContext(context);
                asset.Dispatch(hierarchyBuilder);
                assetElement = hierarchyBuilder.Result;

                context = context.Clone(assetElement);
            }
        }

        private IEnumerable<IDefinition> DiscoverAssets(IEnumerable<IAssembly> assemblies)
        {
            TraceSources.GeneratorSource.TraceEvent(TraceEventType.Start, 0, "Discovering assets");

            CciFilterContext filterContext = new CciFilterContext(FilterState.Discovery);
            TypeMemberAssetCollector assetCollector = new TypeMemberAssetCollector();
          
            MetadataTraverser traverser = new MetadataTraverser
                                          {
                                              PreorderVisitor = assetCollector,
                                              TraverseIntoMethodBodies = false
                                          };

            foreach (IAssembly assembly in assemblies)
            {
                traverser.Traverse(assembly);
            }

            foreach (ITypeDefinitionMember typeDefinitionMember in assetCollector)
            {
                if (!this.IsFiltered(filterContext, typeDefinitionMember))
                    yield return typeDefinitionMember;
            }

            TraceSources.GeneratorSource.TraceEvent(TraceEventType.Stop, 0, "Discovering assets");
        }

        private bool IsFiltered(ICciFilterContext filterContext, IDefinition asset)
        {
            AssetIdentifier assetId = AssetIdentifier.FromDefinition(asset);
            bool filtered = false;
            foreach (ICciAssetFilter filter in this.AssetFilters)
            {
                if (filter.Filter(filterContext, asset))
                {

                    filtered = true;
                    TraceSources.GeneratorSource.TraceEvent(TraceEventType.Verbose,
                                                            0,
                                                            "{0} - Filtered by {1}",
                                                            assetId.AssetId,
                                                            filter);

                    break;
                }
            }
            return filtered;
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

        ~CciDocGenerator()
        {
            this.Dispose(false);
        }
    }
}
