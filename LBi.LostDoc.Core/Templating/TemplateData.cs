using System.Xml.Linq;

namespace LBi.LostDoc.Core.Templating
{
    public class TemplateData
    {
        public VersionComponent? IgnoredVersionComponent { get; set; }
        public AssetRedirectCollection AssetRedirects { get; set; }
        public XDocument Document { get; set; }
    }
}