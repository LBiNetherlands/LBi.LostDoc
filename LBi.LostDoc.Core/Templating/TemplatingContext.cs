using System.Xml.Linq;

namespace LBi.LostDoc.Core.Templating
{
    public class TemplatingContext : ITemplatingContext
    {
        #region ITemplatingContext Members

        public string OutputDir { get; set; }

        public XDocument InputDocument { get; set; }

        public VersionComponent? IgnoredVersionComponent { get; set; }

        public AssetRedirectCollection AssetRedirects { get; set; }

        #endregion
    }
}