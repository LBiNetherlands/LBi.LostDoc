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
            var template = LoadTemplate("MultipleVersionsOfTempFile");

            TemporaryFileProvider tempFiles = new TemporaryFileProvider();
            TemplateOutput output = template.Generate(new XDocument(new XElement("root")),
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

        [Fact]
        public void EnrichInputDocument()
        {
            var template = LoadTemplate("EnrichInputDocument");

            TemporaryFileProvider tempFiles = new TemporaryFileProvider();
            TemplateOutput output = template.Generate(new XDocument(new XElement("root")),
                                                      new TemplateSettings
                                                      {
                                                          OutputFileProvider = tempFiles,
                                                      });

            Assert.Equal(1, tempFiles.GetFiles("").Count());
            Assert.Contains("out.xml", tempFiles.GetFiles(""));

            using (var file = tempFiles.OpenFile("out.xml", FileMode.Open))
            {
                Assert.Equal("data", XDocument.Load(file).Root.Name);
            }

            tempFiles.Delete();
        }

        [Fact]
        public void AlternateStaticInput()
        {
            var template = LoadTemplate("AlternateStaticInput");

            TemporaryFileProvider tempFiles = new TemporaryFileProvider();
            TemplateOutput output = template.Generate(new XDocument(new XElement("input")),
                                                      new TemplateSettings
                                                      {
                                                          OutputFileProvider = tempFiles,
                                                      });

            Assert.Equal(1, tempFiles.GetFiles("").Count());
            Assert.Contains("out.xml", tempFiles.GetFiles(""));

            using (var file = tempFiles.OpenFile("out.xml", FileMode.Open))
            {
                Assert.Equal("example", XDocument.Load(file).Root.Name);
            }

            tempFiles.Delete();
        }

        [Fact]
        public void AlternateGeneratedInput()
        {
            var template = LoadTemplate("AlternateGeneratedInput");

            TemporaryFileProvider tempFiles = new TemporaryFileProvider();
            TemplateOutput output = template.Generate(new XDocument(new XElement("input")),
                                                      new TemplateSettings
                                                      {
                                                          OutputFileProvider = tempFiles,
                                                      });

            Assert.Equal(1, tempFiles.GetFiles("").Count());
            Assert.Contains("out.xml", tempFiles.GetFiles(""));

            using (var file = tempFiles.OpenFile("out.xml", FileMode.Open))
            {
                Assert.Equal("input", XDocument.Load(file).Root.Name);
            }

            tempFiles.Delete();
        }


        private static Template LoadTemplate(string name)
        {
            IFileProvider templateProvider = new ResourceFileProvider("LBi.LostDoc.Test.Templating",
                                                                      Assembly.GetExecutingAssembly());

            TemplateInfo tInfo = new TemplateInfo(templateProvider,
                                                  name + "/template.xml",
                                                  name,
                                                  new TemplateParameterInfo[0],
                                                  null);

            Template template = tInfo.Load();
            return template;
        }
    }
}

