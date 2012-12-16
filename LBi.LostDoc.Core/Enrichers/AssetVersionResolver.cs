using System;
using System.Linq;
using System.Reflection;

namespace LBi.LostDoc.Core.Enrichers
{
    internal class AssetVersionResolver
    {
        private IProcessingContext _context;

        public AssetVersionResolver(IProcessingContext context)
        {
            this._context = context;
        }

        public string getVersionedId(string assetId)
        {
            AssetIdentifier aid = AssetIdentifier.Parse(assetId);
            if (aid.Type == AssetType.Assembly)
            {
                Assembly asm = (Assembly)this._context.AssetResolver.Resolve(aid);
                aid = AssetIdentifier.FromAssembly(asm);
            }
            else if (aid.Type == AssetType.Namespace)
            {
                string ns = aid.AssetId.Substring(aid.TypeMarker.Length + 1);
                Version[] groups =
                    this._context.AssetResolver.Context.SelectMany(a => a.GetTypes())
                        .Where(t => ns.Equals(t.Namespace, StringComparison.Ordinal))
                        .Select(t => t.Assembly)
                        .Distinct()
                        .GroupBy(a => a.GetName().Version, (v, g) => v).ToArray();

                if (groups.Length > 1)
                    throw new AmbiguousMatchException();

                aid = AssetIdentifier.FromNamespace(ns, groups[0]);
            }
            else
            {
                object obj = this._context.AssetResolver.Resolve(aid);
                aid = AssetIdentifier.FromMemberInfo((MemberInfo)obj);
            }

            this._context.AddReference(aid);

            return aid.ToString();
        }
    }
}