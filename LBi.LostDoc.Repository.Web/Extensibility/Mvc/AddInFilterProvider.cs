using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Web.Mvc;

namespace LBi.LostDoc.Repository.Web.Extensibility.Mvc
{
    public class AddInFilterProvider : FilterAttributeFilterProvider
    {
        public AddInFilterProvider(CompositionContainer container)
        {
            this.Container = container;
        }

        public CompositionContainer Container { get; protected set; }

        protected override IEnumerable<FilterAttribute> GetControllerAttributes(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            var attributes = base.GetControllerAttributes(controllerContext, actionDescriptor).ToArray();
            this.Container.ComposeParts(attributes);
            return attributes;
        }
    }
}