using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Cci;

namespace LBi.LostDoc.Cci
{
    public class CciProcessingContext : ICciProcessingContext
    {
        private readonly HashSet<IReference> _references;
        private readonly ICciAssetFilter[] _filters;

        public CciProcessingContext(IEnumerable<ICciAssetFilter> filters, XElement element, int phase)
        {
            this._references = new HashSet<IReference>();
            this.Element = element;
            this.Phase = phase;
            this._filters = filters.ToArray();
        }

        protected CciProcessingContext(HashSet<IReference> references,
                                       ICciAssetFilter[] filters,
                                       XElement element,
                                       int phase)
        {
            this._references = references;
            this._filters = filters;
            this.Element = element;
            this.Phase = phase;
        }

        public XElement Element { get; protected set; }

        public int Phase { get; protected set; }

        public bool AddReference(IReference asset)
        {
            return this._references.Add(asset);
        }

        public IEnumerable<IReference> References
        {
            get { return this._references.AsEnumerable(); }
        }

        public ICciProcessingContext Clone(XElement newElement)
        {
            return new CciProcessingContext(this._references, this._filters, newElement, this.Phase);
        }

        public ICciProcessingContext Clone(int newPhase)
        {
            return new CciProcessingContext(this._references, this._filters, this.Element, newPhase);
        }

        public bool IsFiltered(IDefinition asset)
        {
            ICciFilterContext filterContext = new CciFilterContext(FilterState.Generating);
            return this._filters.Any(assetFilter => assetFilter.Filter(filterContext, asset));
        }
    }
}