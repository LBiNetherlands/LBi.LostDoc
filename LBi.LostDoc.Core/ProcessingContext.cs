using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using LBi.LostDoc.Core.Diagnostics;

namespace LBi.LostDoc.Core
{
    public class ProcessingContext : IProcessingContext
    {
        private readonly IAssetFilter[] _filters;
        private readonly HashSet<AssetIdentifier> _references;

        public ProcessingContext(IEnumerable<IAssetFilter> filters, IAssetResolver assetResolver, XElement element,
                                 HashSet<AssetIdentifier> references, int phase)
        {
            this._filters = filters.ToArray();
            this.AssetResolver = assetResolver;
            this.Element = element;
            this._references = references;
            this.Phase = phase;
        }

        #region IProcessingContext Members

        public XElement Element { get; private set; }

        public IEnumerable<AssetIdentifier> References
        {
            get { return this._references; }
        }

        public bool AddReference(string assetId)
        {
            if (string.IsNullOrEmpty(assetId))
                throw new ArgumentException("Argument cannot be null or empty.", "assetId");

            AssetIdentifier aid = AssetIdentifier.Parse(assetId);

            Debug.Assert(aid.ToString().Equals(assetId, StringComparison.Ordinal),
                         "AssetIdentifier '{0}' failed to roundtrip.", assetId);

            if (aid.AssetId[aid.AssetId.Length - 1] == ']')
                aid = new AssetIdentifier(aid.AssetId.Substring(0, aid.AssetId.LastIndexOf('[')),
                                          aid.Version);

            object resolve = this.AssetResolver.Resolve(aid);
            Debug.Assert(resolve != null);

            Debug.Assert(!string.IsNullOrWhiteSpace(aid.AssetId));

            if (this._references.Add(aid))
            {
                TraceSources.GeneratorSource.TraceEvent(TraceEventType.Verbose, 1, "Reference: {0}", aid);
                return true;
            }

            return false;
        }

        public IAssetResolver AssetResolver { get; protected set; }

        public int Phase { get; private set; }

        public IProcessingContext Clone(XElement newElement)
        {
            return new ProcessingContext(this._filters, this.AssetResolver, newElement, this._references, this.Phase);
        }

        public bool IsFiltered(AssetIdentifier assetId)
        {
            IFilterContext filterContext = new FilterContext(this.AssetResolver);
            for (int i = 0; i < this._filters.Length; i++)
            {
                if (this._filters[i].Filter(filterContext, assetId))
                    return true;
            }

            return false;
        }

        #endregion
    }
}