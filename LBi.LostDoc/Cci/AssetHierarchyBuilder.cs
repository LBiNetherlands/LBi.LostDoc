using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        }

        public override void Visit(IRootUnitNamespace rootUnitNamespace)
        {
        }

        public override void Visit(IMethodDefinition method)
        {
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
    }
}