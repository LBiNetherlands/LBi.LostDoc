using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Cci;

namespace LBi.LostDoc.Cci
{
    public class AssetHierarchyBuilder : MetadataVisitor
    {
        private ICciProcessingContext _context;

        public XElement Result { get; protected set; }

        public void SetContext(ICciProcessingContext context)
        {
            this._context = context;
        }

        //private void BuildHierarchy(IAssetResolver assetResolver, XElement parentNode, LinkedListNode<IDefinition> hierarchy, HashSet<IDefinition> references, HashSet<IDefinition> emittedAssets, int phase)
        //{
        //    if (hierarchy == null)
        //        return;


        //    var asset = hierarchy.Value;
        //    ICciProcessingContext pctx = new CciProcessingContext(this.AssetFilters, parentNode, phase);

        //    XElement newElement;

        //    // add asset to list of generated assets
        //    emittedAssets.Add(asset);

        //    // dispatch depending on type
        //    switch (aid.Type)
        //    {
        //        case AssetType.Namespace:
        //            newElement = parentNode.XPathSelectElement(string.Format("namespace[@assetId = '{0}']", aid));
        //            if (newElement == null)
        //                newElement = this.GenerateNamespaceElement(pctx, aid);
        //            break;
        //        case AssetType.Type:
        //            newElement = parentNode.XPathSelectElement(string.Format("*[@assetId = '{0}']", aid));
        //            if (newElement == null)
        //                newElement = this.GenerateTypeElement(pctx, aid);
        //            break;
        //        case AssetType.Method:
        //            newElement = parentNode.XPathSelectElement(string.Format("*[@assetId = '{0}']", aid));
        //            if (newElement == null)
        //                newElement = this.GenerateMethodElement(pctx, aid);
        //            break;
        //        case AssetType.Field:
        //            newElement = parentNode.XPathSelectElement(string.Format("field[@assetId = '{0}']", aid));
        //            if (newElement == null)
        //                newElement = this.GenerateFieldElement(pctx, aid);
        //            break;
        //        case AssetType.Event:
        //            newElement = parentNode.XPathSelectElement(string.Format("event[@assetId = '{0}']", aid));
        //            if (newElement == null)
        //                newElement = this.GenerateEventElement(pctx, aid);
        //            break;
        //        case AssetType.Property:
        //            newElement = parentNode.XPathSelectElement(string.Format("property[@assetId = '{0}']", aid));
        //            if (newElement == null)
        //                newElement = this.GeneratePropertyElement(pctx, aid);
        //            break;
        //        case AssetType.Assembly:
        //            newElement = parentNode.XPathSelectElement(string.Format("assembly[@assetId = '{0}']", aid));
        //            if (newElement == null)
        //                newElement = this.GenerateAssemblyElement(pctx, aid);
        //            break;
        //        default:
        //            throw new ArgumentOutOfRangeException();
        //    }

        //    this.BuildHierarchy(assetResolver, newElement, hierarchy.Next, references, emittedAssets, phase);
        //}

        public override void Visit(IAssembly assembly)
        {
            AssetIdentifier aid = AssetIdentifier.FromDefinition(assembly);
            XElement element = this._context.Element.XPathSelectElement(string.Format("assembly[@assetId = '{0}']", aid));
            if (element == null)
                element = this.GenerateAssemblyElement(aid, assembly);

            this.Result = element;
        }

        public override void Visit(INamespaceTypeDefinition namedTypeDefinition)
        {
        }

        public override void Visit(INestedTypeDefinition nestedTypeDefinition)
        {

        }

        public override void Visit(INamespaceDefinition namespaceDefinition)
        {
        }

        public override void Visit(INestedUnitNamespace nestedUnitNamespace)
        {
            AssetIdentifier aid = AssetIdentifier.FromDefinition(nestedUnitNamespace);
            XElement element = this._context.Element.XPathSelectElement(string.Format("namespace[@assetId = '{0}']", aid));
            if (element == null)
                element = this.GenerateNamespaceElement(aid, nestedUnitNamespace);

            this.Result = element;
        }

        public override void Visit(IRootUnitNamespace rootUnitNamespace)
        {
            AssetIdentifier aid = AssetIdentifier.FromDefinition(rootUnitNamespace);
            XElement element = this._context.Element.XPathSelectElement(string.Format("namespace[@assetId = '{0}']", aid));
            if (element == null)
                element = this.GenerateNamespaceElement(aid, rootUnitNamespace);

            this.Result = element;
        }

        public override void Visit(IMethodDefinition method)
        {
        }

        public static void GenerateTypeRef(ICciProcessingContext context, ITypeDefinition pType, string attrName = null)
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


        private XElement GenerateAssemblyElement(AssetIdentifier assetId, IAssembly assembly)
        {
            XElement ret = new XElement("assembly",
                                        new XAttribute("name", assembly.Name),
                                        new XAttribute("filename", assembly.Files.Single().FileName),
                                        new XAttribute("assetId", assetId),
                                        new XAttribute("phase", this._context.Phase));

            foreach (IAssemblyReference assemblyReference in assembly.AssemblyReferences)
            {
                IAssembly referencedAssembly = assemblyReference.ResolvedAssembly;
                ret.Add(new XElement("references", new XAttribute("assembly", AssetIdentifier.FromDefinition(referencedAssembly))));
            }

            this._context.Element.Add(ret);

            //foreach (IEnricher enricher in this._enrichers)
            //    enricher.EnrichAssembly(context.Clone(ret), asm);

            return ret;
        }

        private XElement GenerateNamespaceElement(AssetIdentifier assetId, INamespaceDefinition ns)
        {
            var ret = new XElement("namespace",
                                   new XAttribute("name", ns.Name),
                                   new XAttribute("assetId", assetId),
                                   new XAttribute("phase", this._context.Phase));

            this._context.Element.Add(ret);

            //foreach (IEnricher enricher in this._enrichers)
            //    enricher.EnrichNamespace(context.Clone(ret), ns);

            return ret;
        }

        private XElement GenerateTypeElement(AssetIdentifier assetId, ITypeDefinition type)
        {
            XElement ret;

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
                               new XAttribute("name", TypeHelper.GetTypeName(type)),
                               new XAttribute("assetId", assetId),
                               new XAttribute("phase", this._context.Phase));

            if (type.IsEnum)
            {
                AssetIdentifier aid = AssetIdentifier.FromDefinition(type.UnderlyingType);
                ret.Add(new XAttribute("underlyingType", aid));
                this._context.AddReference(type.UnderlyingType);
            }


            if (!type.IsInterface && type.IsAbstract)
                ret.Add(new XAttribute("isAbstract", XmlConvert.ToString(type.IsAbstract)));

            INestedTypeDefinition nestedType = type as INestedTypeDefinition;

            if (nestedType != null)
            {
                if (nestedType.Visibility.HasFlag(TypeMemberVisibility.Private))
                    ret.Add(new XAttribute("isPrivate", XmlConvert.ToString(true)));

                if (nestedType.Visibility.HasFlag(TypeMemberVisibility.Family))
                    ret.Add(new XAttribute("isProtected", XmlConvert.ToString(true)));

                if (nestedType.Visibility.HasFlag(TypeMemberVisibility.FamilyAndAssembly))
                    ret.Add(new XAttribute("isProtectedAndInternal", XmlConvert.ToString(true)));

                if (nestedType.Visibility.HasFlag(TypeMemberVisibility.FamilyOrAssembly))
                    ret.Add(new XAttribute("isProtectedOrInternal", XmlConvert.ToString(true)));

                if (nestedType.Visibility.HasFlag(TypeMemberVisibility.Public))
                    ret.Add(new XAttribute("isPublic", XmlConvert.ToString(true)));
            }
            else
            {
                INamespaceTypeDefinition namespaceType = (INamespaceTypeDefinition)type;
                if (namespaceType.IsPublic)
                    ret.Add(new XAttribute("isPublic", XmlConvert.ToString(true)));
            }

            if (type.IsClass && type.IsSealed)
                ret.Add(new XAttribute("isSealed", XmlConvert.ToString(true)));

            foreach (ITypeReference baseClassRef in type.BaseClasses)
            {
                if (!this._context.IsFiltered(baseClassRef.ResolvedType))
                {
                    var inheritsElem = new XElement("inherits");
                    ret.Add(inheritsElem);
                    GenerateTypeRef(this._context.Clone(inheritsElem), baseClassRef.ResolvedType);
                }
            }

            if (type.GenericParameterCount > 0)
            {
                foreach (IGenericTypeParameter tp in type.GenericParameters)
                {
                    this.GenerateTypeParamElement(this._context.Clone(ret), type, tp);
                }
            }

            if (type.IsClass)
            {
                foreach (ITypeReference interfaceType in type.Interfaces)
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


            //foreach (IEnricher enricher in this._enrichers)
            //    enricher.EnrichType(context.Clone(ret), type);


            this._context.Element.Add(ret);

            return ret;
        }
    }




    public class TypeReferenceBuilder : MetadataVisitor
    {
        public static void Create(ICciProcessingContext context, ITypeReference typeRef)
        {
            TypeReferenceBuilder builder = new TypeReferenceBuilder();
            builder.SetContext(context);
            typeRef.DispatchAsReference(builder);

        }

        public override void Visit(IArrayTypeReference arrayTypeReference)
        {

        }

        public override void Visit(ITypeReference typeReference)
        {
        }
    }
}