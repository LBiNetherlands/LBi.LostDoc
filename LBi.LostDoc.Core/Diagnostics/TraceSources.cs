using System.Diagnostics;

namespace LBi.LostDoc.Core.Diagnostics
{
    public static class TraceSources
    {
        public static readonly TraceSource TemplateSource;
        public static readonly TraceSource AssetResolveSource;
        public static readonly TraceSource GeneratorSource;

        static TraceSources()
        {
            TemplateSource = new TraceSource("LostDoc.Core.Template", SourceLevels.All);
            AssetResolveSource = new TraceSource("LostDoc.Core.Template.AssetResolver", SourceLevels.All);
            GeneratorSource = new TraceSource("LostDoc.Core.DocGenerator", SourceLevels.All);
        }
    }
}