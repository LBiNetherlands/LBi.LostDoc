using System.IO;
using System.Text;
using dotless.Core.configuration;

namespace LBi.LostDoc.Templating.Transforms.Less
{
    [ExportTransform("less")]
    public class LessTransform : IResourceTransform
    {
        public Stream Transform(Stream input)
        {
            using (StreamReader reader = new StreamReader(input, true))
            {
                string lessDoc = dotless.Core.Less.Parse(reader.ReadToEnd(),
                                                         new DotlessConfiguration
                                                             {
                                                                 CacheEnabled = false,
                                                                 Debug = false,
                                                             });
                return new MemoryStream(Encoding.UTF8.GetBytes(lessDoc)) {Position = 0};
            }
        }
    }
}
