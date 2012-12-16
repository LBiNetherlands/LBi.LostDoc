using System.Xml.Linq;

namespace LBi.LostDoc.Core.Templating
{
    public interface ITemplatingContext : IContextBase
    {
        string OutputDir { get; set; }
        XDocument InputDocument { get; set; }
        VersionComponent? IgnoredVersionComponent { get; set; }
        AssetRedirectCollection AssetRedirects { get; set; }
    }
}