using System.ComponentModel.Composition;
using System.Reflection;
using LBi.LostDoc.Composition;
using LBi.LostDoc.Extensibility;
using LBi.LostDoc.Templating;
using LBi.LostDoc.Templating.FileProviders;

namespace LBi.LostDoc.Templates
{
    public class Export
    {
        [Export(ContractNames.TemplateProvider, typeof(IFileProvider))]
        public IFileProvider Provider { get { return new ResourceFileProvider("LBi.LostDoc.Templates", Assembly.GetExecutingAssembly()); } }
    }
}
