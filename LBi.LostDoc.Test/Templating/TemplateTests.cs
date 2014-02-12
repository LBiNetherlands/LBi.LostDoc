using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using LBi.LostDoc.Templating;
using LBi.LostDoc.Templating.IO;
using Xunit;

namespace LBi.LostDoc.Test.Templating
{
    public class TemplateTests
    {
        [Fact]
        public void MultipleVersionsOfTempFile()
        {
            IFileProvider templateProvider = new ResourceFileProvider("LBi.LostDoc.Test.Templating", Assembly.GetExecutingAssembly());

            TemplateInfo tInfo = new TemplateInfo(templateProvider,
                                                  "MultipleVersionsOfTempFile/template.xml",
                                                  "MultipleVersionsOfTempFile",
                                                  new TemplateParameterInfo[0],
                                                  null);

            Template template = tInfo.Load();

            TemporaryFileProvider tempFiles = new TemporaryFileProvider();
            TemplateOutput output = template.Generate(new XDocument(),
                                                      new TemplateSettings
                                                      {
                                                          OutputFileProvider = tempFiles,
                                                      });

            Assert.Equal(3, tempFiles.GetFiles("").Count());
            Assert.Contains("Test1.xml", tempFiles.GetFiles(""));
            Assert.Contains("Test2.xml", tempFiles.GetFiles(""));
            Assert.Contains("Test3.xml", tempFiles.GetFiles(""));

            using (var file = tempFiles.OpenFile("Test1.xml", FileMode.Open))
            {
                Assert.Equal("one", XDocument.Load(file).Root.Value);
            }

            using (var file = tempFiles.OpenFile("Test2.xml", FileMode.Open))
            {
                Assert.Equal("two", XDocument.Load(file).Root.Value);
            }

            using (var file = tempFiles.OpenFile("Test3.xml", FileMode.Open))
            {
                Assert.Equal("three", XDocument.Load(file).Root.Value);
            }

            tempFiles.Delete();
        }
    }
}

