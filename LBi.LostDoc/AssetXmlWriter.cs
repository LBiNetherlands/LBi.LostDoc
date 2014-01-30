using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using LBi.LostDoc.Primitives;

namespace LBi.LostDoc
{
    public class AssetXmlWriter : IVisitor
    {
        private XElement _current;
        private IProcessingContext _context;
        private IEnricher[] _enrichers;

        public AssetXmlWriter(IEnumerable<IEnricher> enrichers)
        {
            this._enrichers = enrichers.ToArray();
        }


        public XElement Writer(IProcessingContext context, Asset asset)
        {
            this._current = null;
            this._context = context;

            asset.Visit(this);
            XElement ret = this._current;

            this._context = null;
            this._current = null;

            return ret;
        }

        protected virtual IEnumerable<XAttribute> GetCommonAttributes(IAsset asset)
        {
            yield return new XAttribute("name", asset.Name);
            yield return new XAttribute("assetId", asset.Id);
            yield return new XAttribute("phase", this._context.Phase);
        }

        void IVisitor.VisitAssembly(IAssembly asset)
        {
            this._current = new XElement("assembly",
                                         new XAttribute("filename", asset.Filename),
                                         this.GetCommonAttributes(asset));

            IEnumerable<Asset> references = this._context.AssetExplorer.GetReferences(asset);

            foreach (Asset reference in references)
            {
                this._current.Add(new XElement("references", new XAttribute("assembly", reference.Id)));
            }

            this._context.Element.Add(this._current);

            foreach (IEnricher enricher in this._enrichers)
                enricher.EnrichAssembly(this._context.Clone(this._current), asset);
        }

        void IVisitor.VisitNamespace(INamespace asset)
        {
            this._current = new XElement("namespace",
                                         this.GetCommonAttributes(asset));

            this._context.Element.Add(this._current);

            foreach (IEnricher enricher in this._enrichers)
                enricher.EnrichNamespace(this._context.Clone(this._current), asset);
        }

        private void VisitType(IType asset)
        {
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


            if (type.ContainsGenericParameters)
            {
                Type[] typeParams = type.GetGenericArguments();
                if (type.IsNested && type.DeclaringType.ContainsGenericParameters)
                {
                    Type[] inheritedTypeParams = type.DeclaringType.GetGenericArguments();

                    Debug.Assert(typeParams.Length >= inheritedTypeParams.Length);

                    for (int paramPos = 0; paramPos < inheritedTypeParams.Length; paramPos++)
                    {
                        Debug.Assert(typeParams[paramPos].Name == inheritedTypeParams[paramPos].Name);
                        Debug.Assert(typeParams[paramPos].GenericParameterAttributes == inheritedTypeParams[paramPos].GenericParameterAttributes);
                    }

                    Type[] declaredTypeParams = new Type[typeParams.Length - inheritedTypeParams.Length];

                    for (int paramPos = inheritedTypeParams.Length; paramPos < typeParams.Length; paramPos++)
                    {
                        declaredTypeParams[paramPos - inheritedTypeParams.Length] = typeParams[paramPos];
                    }

                    typeParams = declaredTypeParams;
                }

                foreach (Type tp in typeParams)
                {
                    this.GenerateTypeParamElement(context.Clone(ret), type, tp);
                }
            }

            foreach (IEnricher enricher in this._enrichers)
                enricher.EnrichType(this._context.Clone(this._current), asset);
        }

        void IVisitor.VisitField(IField asset)
        {
            throw new NotImplementedException();
        }

        void IVisitor.VisitEvent(IEvent asset)
        {
            this._current = new XElement("event",
                                         this.GetCommonAttributes(asset));

            GenerateTypeRef(this._context.Clone(this._current), asset.EventHandlerType);

            IMethod addMethod = asset.AddMethod;
            IMethod removeMethod = asset.RemoveMethod;
            if (addMethod != null)
            {
                var addElem = new XElement("add");
                if (addMethod.IsPublic)
                    addElem.Add(new XAttribute("isPublic", XmlConvert.ToString(addMethod.IsPublic)));

                if (addMethod.IsPrivate)
                    addElem.Add(new XAttribute("isPrivate", XmlConvert.ToString(addMethod.IsPrivate)));

                if (addMethod.IsFamily)
                    addElem.Add(new XAttribute("isProtected", XmlConvert.ToString(addMethod.IsFamily)));

                this._current.Add(addElem);
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

                this._current.Add(removeElem);
            }

            this.GenerateImplementsElement(this._context.Clone(this._current), asset);

            foreach (IEnricher item in this._enrichers)
                item.EnrichEvent(this._context.Clone(this._current), asset);
        }

        void IVisitor.VisitProperty(IProperty asset)
        {
            this._current = new XElement("property",
                                         this.GetCommonAttributes(asset));

            this.GenerateTypeRef(this._context.Clone(this._current), asset.Returns);

            this.GenerateParameterElements(this._context.Clone(this._current), asset.Parameters);

            IMethod setMethod = asset.SetMethod;
            IMethod getMethod = asset.GetMethod;

            if ((setMethod ?? getMethod).IsAbstract)
                this._current.Add(new XAttribute("isAbstract", XmlConvert.ToString(true)));

            if ((setMethod ?? getMethod).IsVirtual)
                this._current.Add(new XAttribute("isVirtual", XmlConvert.ToString(true)));


            const int C_PUBLIC = 10;
            const int C_INTERNAL_OR_PROTECTED = 8;
            const int C_INTERNAL = 6;
            const int C_PROTECTED = 4;
            const int C_INTERNAL_AND_PROTECTED = 2;
            const int C_PRIVATE = 0;

            int leastRestrictiveAccessModifier;

            if (setMethod != null && setMethod.IsPublic || getMethod != null && getMethod.IsPublic)
            {
                this._current.Add(new XAttribute("isPublic", XmlConvert.ToString(true)));
                leastRestrictiveAccessModifier = C_PUBLIC;
            }
            else if (setMethod != null && setMethod.IsFamilyOrAssembly ||
                     getMethod != null && getMethod.IsFamilyOrAssembly)
            {
                this._current.Add(new XAttribute("isInternalOrProtected", XmlConvert.ToString(true)));
                leastRestrictiveAccessModifier = C_INTERNAL_OR_PROTECTED;
            }
            else if (setMethod != null && setMethod.IsAssembly || getMethod != null && getMethod.IsAssembly)
            {
                this._current.Add(new XAttribute("isInternal", XmlConvert.ToString(true)));
                leastRestrictiveAccessModifier = C_INTERNAL;
            }
            else if (setMethod != null && setMethod.IsFamily || getMethod != null && getMethod.IsFamily)
            {
                this._current.Add(new XAttribute("isProtected", XmlConvert.ToString(true)));
                leastRestrictiveAccessModifier = C_PROTECTED;
            }
            else if (setMethod != null && setMethod.IsFamilyAndAssembly ||
                     getMethod != null && getMethod.IsFamilyAndAssembly)
            {
                this._current.Add(new XAttribute("isInternalAndProtected", XmlConvert.ToString(true)));
                leastRestrictiveAccessModifier = C_INTERNAL_AND_PROTECTED;
            }
            else if (setMethod != null && setMethod.IsPrivate || getMethod != null && getMethod.IsPrivate)
            {
                this._current.Add(new XAttribute("isPrivate", XmlConvert.ToString(true)));
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

                this._current.Add(setElem);
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

                this._current.Add(getElem);
            }


            if (asset.IsSpecialName)
                this._current.Add(new XAttribute("isSpecialName", XmlConvert.ToString(asset.IsSpecialName)));

            this.GenerateImplementsElement(this._context.Clone(this._current), asset);

            foreach (IEnricher item in this._enrichers)
                item.EnrichProperty(this._context.Clone(ret), asset);

        }

        void IVisitor.VisitUnknown(IAsset asset)
        {
            throw new NotImplementedException();
        }

        void IVisitor.VisitMethod(IMethod asset)
        {
            throw new NotImplementedException();
        }

        void IVisitor.VisitConstructor(IConstructor asset)
        {
            throw new NotImplementedException();
        }

        public void VistEnum(IEnum asset)
        {
            this._current = new XElement("enum",
                                         this.GetCommonAttributes(asset));

            IValueType underlyingType = asset.UnderlyingType;

            this._current.Add(new XAttribute("underlyingType", underlyingType.Id));
            this._context.AddReference(underlyingType);

            this.VisitType(asset);
        }

        public void VistInterface(IInterface asset)
        {
            this._current = new XElement("enum",
                                         this.GetCommonAttributes(asset));

            foreach (IInterface interfaceAsset in asset.DeclaredInterfaces)
            {
                if (!this._context.IsFiltered(interfaceAsset))
                {
                    var implElement = new XElement("implements");
                    this._current.Add(implElement);
                    this.GenerateTypeRef(this._context.Clone(implElement), interfaceAsset, "interface");
                }
            }

            this.VisitType(asset);
        }

        public void VistReferenceType(IReferenceType asset)
        {
            this._current = new XElement("class",
                                         this.GetCommonAttributes(asset));

            if (asset.IsSealed)
                this._current.Add(new XAttribute("isSealed", XmlConvert.ToString(true)));

            if (!this._context.IsFiltered(asset.BaseType))
            {
                var inheritsElem = new XElement("inherits");
                this._current.Add(inheritsElem);
                this.GenerateTypeRef(this._context.Clone(inheritsElem), asset.BaseType);
            }

            foreach (IInterface interfaceAsset in asset.DeclaredInterfaces)
            {
                if (!this._context.IsFiltered(interfaceAsset))
                {
                    var implElement = new XElement("implements");
                    this._current.Add(implElement);
                    this.GenerateTypeRef(this._context.Clone(implElement), interfaceAsset, "interface");
                }
            }

            this.VisitType(asset);
        }

        public void VistValueType(IValueType asset)
        {
            this._current = new XElement("struct",
                                         this.GetCommonAttributes(asset));

            foreach (IInterface interfaceAsset in asset.DeclaredInterfaces)
            {
                if (!this._context.IsFiltered(interfaceAsset))
                {
                    var implElement = new XElement("implements");
                    this._current.Add(implElement);
                    this.GenerateTypeRef(this._context.Clone(implElement), interfaceAsset, "interface");
                }
            }

            this.VisitType(asset);
        }

        public void VisitOperator(IOperator asset)
        {
            throw new NotImplementedException();
        }

        public void VisitDelegate(IDelegate asset)
        {
            throw new NotImplementedException();
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
                    AssetIdentifier ifAssetId = AssetIdentifier.FromMemberInfo(ifType);
                    Asset ifAsset = new Asset(ifAssetId, ifType);
                    if (context.IsFiltered(ifAsset))
                        continue;

                    InterfaceMapping ifMap = declaringType.GetInterfaceMap(ifType);
                    if (ifMap.TargetType != declaringType)
                        continue;

                    var targetMethod = ifMap.TargetMethods.SingleOrDefault(mi => mi.MetadataToken == mInfo.MetadataToken &&
                                                                                 mi.Module == mInfo.Module);

                    if (targetMethod != null)
                    {
                        int mIx = Array.IndexOf(ifMap.TargetMethods, targetMethod);

                        Asset miAsset;
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
                            miAsset = new Asset(AssetIdentifier.FromMemberInfo(memberInfo), memberInfo);
                        }
                        else
                        {
                            miAsset = new Asset(AssetIdentifier.FromMemberInfo(ifMap.InterfaceMethods[mIx]),
                                                ifMap.InterfaceMethods[mIx]);
                        }

                        context.Element.Add(new XElement("implements", new XAttribute("member", miAsset.Id)));
                        context.AddReference(miAsset);
                    }
                }
            }
        }

        private void GenerateParameterElements(IProcessingContext context, IEnumerable<Parameter> methodParams)
        {
            foreach (Parameter item in methodParams)
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

                foreach (IEnricher enricher in this._enrichers)
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

        public void GenerateTypeRef(IProcessingContext context, IType type, string attrName = null)
        {
            // TODO rethink how we generate the typerefs, probably ensure we always output a root element rather than just the attribute for param/type
            if (type.PrimitiveType == Primitive.Array)
            {
                // TODO arrayOf is the only capitalized element
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
                    Type typeDefinition = pType.GetGenericTypeDefinition();
                    AssetIdentifier aid = AssetIdentifier.FromType(typeDefinition);
                    context.AddReference(new Asset(aid, typeDefinition));

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
                    context.AddReference(new Asset(aid, pType));

                    context.Element.Add(new XAttribute(attrName ?? "type", aid));
                }
            }
        }
    }
}