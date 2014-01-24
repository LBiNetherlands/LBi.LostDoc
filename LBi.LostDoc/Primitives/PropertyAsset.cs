using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace LBi.LostDoc.Primitives
{
    public abstract class PropertyAsset : MemberAsset
    {
        protected PropertyAsset(AssetIdentifier id, object target)
            : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Property, "Invalid AssetIdentifier for PropertyAsset");
        }

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitProperty(this);
        }

        public abstract TypeAsset PropertyType { get; }

        public abstract bool IsSpecialName { get; }

        public abstract IEnumerable<Parameter> Parameters { get; }
        
        public abstract MethodAsset SetMethod { get; }

        public abstract MethodAsset GetMethod { get; }
    }
}