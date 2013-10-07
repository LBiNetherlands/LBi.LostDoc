using System.Collections;
using System.Collections.Generic;
using Microsoft.Cci;

namespace LBi.LostDoc.Cci
{
    public class AssetHierarchyBuilder : MetadataVisitor, IEnumerable<IDefinition>
    {
        private readonly List<IDefinition> _hierachy;

        public AssetHierarchyBuilder()
        {
            this._hierachy = new List<IDefinition>();
        }

        public override void Visit(IAssembly assembly)
        {
            this._hierachy.Add(assembly);
        }

        public override void Visit(INamespaceTypeDefinition namedTypeDefinition)
        {
            this._hierachy.Add(namedTypeDefinition);
            namedTypeDefinition.ContainingNamespace.Dispatch(this);
        }

        public override void Visit(INestedTypeDefinition nestedTypeDefinition)
        {
            this._hierachy.Add(nestedTypeDefinition);
            nestedTypeDefinition.ContainingTypeDefinition.Dispatch(this);
        }

        public override void Visit(INamespaceDefinition namespaceDefinition)
        {
            this._hierachy.Add(namespaceDefinition);
            namespaceDefinition.RootOwner.NamespaceRoot.Dispatch(this);
        }

        public override void Visit(INestedUnitNamespace nestedUnitNamespace)
        {
            this._hierachy.Add(nestedUnitNamespace);
            nestedUnitNamespace.ContainingUnitNamespace.Dispatch(this);
        }

        public override void Visit(IRootUnitNamespace rootUnitNamespace)
        {
            this._hierachy.Add(rootUnitNamespace);
            rootUnitNamespace.Unit.Dispatch(this);
        }

        public override void Visit(IMethodDefinition method)
        {
            this._hierachy.Add(method);
            method.ContainingType.Dispatch(this);
        }

        public void Clear()
        {
            this._hierachy.Clear();
        }

        public IEnumerator<IDefinition> GetEnumerator()
        {
            return this._hierachy.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}