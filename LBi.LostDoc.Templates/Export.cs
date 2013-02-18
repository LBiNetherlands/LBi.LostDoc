using System.ComponentModel.Composition;
using System.Reflection;
using LBi.LostDoc.Templating;
using LBi.LostDoc.Templating.FileProviders;

namespace LBi.LostDoc.Templates
{
    public class Export
    {
        [Export("TemplateProvider", typeof(IReadOnlyFileProvider))]
        public IReadOnlyFileProvider Provider { get { return new ResourceFileProvider("LBi.LostDoc.Templates", Assembly.GetExecutingAssembly()); } }
    }
}
