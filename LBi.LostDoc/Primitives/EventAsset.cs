using System;
using System.Diagnostics.Contracts;

namespace LBi.LostDoc.Primitives
{
    public abstract class EventAsset : MemberAsset
    {
        protected EventAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Event, "Invalid AssetIdentifier for EventAsset");
        }

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitEvent(this);
        }
    }
}