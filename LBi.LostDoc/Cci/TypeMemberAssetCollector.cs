using System.Collections;
using System.Collections.Generic;
using Microsoft.Cci;

namespace LBi.LostDoc.Cci
{
    public class TypeMemberAssetCollector : MetadataVisitor, IEnumerable<ITypeDefinitionMember>
    {
        private readonly List<ITypeDefinitionMember> _assets;

        public TypeMemberAssetCollector()
        {
            this._assets = new List<ITypeDefinitionMember>();
        }

        public override void Visit(ITypeDefinitionMember typeMember)
        {
            this._assets.Add(typeMember);
        }

        public void Clear()
        {
            this._assets.Clear();
        }

        public IEnumerator<ITypeDefinitionMember> GetEnumerator()
        {
            return this._assets.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}