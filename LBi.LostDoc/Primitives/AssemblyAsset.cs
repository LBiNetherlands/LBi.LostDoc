using System;
using System.Diagnostics.Contracts;

namespace LBi.LostDoc.Primitives
{
    public abstract class AssemblyAsset : Asset
    {
        protected AssemblyAsset(AssetIdentifier id, object target) : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Assembly, "Invalid AssetIdentifier for AssemblyAsset");

        }

        public abstract string Filename { get; }

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitAssembly(this);
        }
    }
}