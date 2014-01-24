using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace LBi.LostDoc.Primitives
{
    public abstract class MethodAsset : MemberAsset
    {
        protected MethodAsset(AssetIdentifier id, object target) : base(id, target)
        {
            Contract.Requires<ArgumentException>(id.Type == AssetType.Method, "Invalid AssetIdentifier for MethodAsset");
        }

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitMethod(this);
        }

        public abstract TypeAsset Returns { get; }

        public abstract IEnumerable<Parameter> Parameters { get; }
        
        public abstract bool IsAbstract { get; }

        public abstract bool IsVirtual { get; }

        public abstract bool IsPublic { get; }

        public abstract bool IsPrivate { get;  }

        public abstract bool IsAssembly { get; }

        public abstract bool IsFamilyAndAssembly { get; }

        public abstract bool IsFamilyOrAssembly { get; }
        
        public abstract bool IsFamily { get; }
    }
}