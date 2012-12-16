using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace LBi.LostDoc.Core.Enrichers
{
    public class ExternalNamespaceDocEnricher : IEnricher
    {
        private readonly XslCompiledTransform _xslTransform;
        private XDocument _doc;

        public ExternalNamespaceDocEnricher()
        {
            this._xslTransform = new XslCompiledTransform();
            using (
                Stream resource =
                    Assembly.GetExecutingAssembly().GetManifestResourceStream(
                                                                              "LBi.LostDoc.Core.Enrichers.enrich-doc-comments.xslt")
                )
            {
                XmlReader reader = XmlReader.Create(resource);
                this._xslTransform.Load(reader);
            }
        }

        #region IEnricher Members

        public void EnrichType(IProcessingContext context, Type type)
        {
        }

        public void EnrichConstructor(IProcessingContext context, ConstructorInfo ctor)
        {
        }

        public void EnrichAssembly(IProcessingContext context, Assembly asm)
        {
        }

        public void RegisterNamespace(IProcessingContext context)
        {
        }

        public void EnrichMethod(IProcessingContext context, MethodInfo mInfo)
        {
        }

        public void EnrichField(IProcessingContext context, FieldInfo fieldInfo)
        {
        }

        public void EnrichProperty(IProcessingContext context, PropertyInfo propertyInfo)
        {
        }

        public void EnrichReturnValue(IProcessingContext context, MethodInfo methodInfo)
        {
        }

        public void EnrichParameter(IProcessingContext context, ParameterInfo item)
        {
        }


        public void EnrichTypeParameter(IProcessingContext context, Type typeParameter)
        {
        }

        public void EnrichNamespace(IProcessingContext context, string ns)
        {
            XElement element = this._doc.XPathSelectElement(string.Format("/doc/namespace[@name = '{0}']", ns));
            if (element != null)
            {
                element = this.EnrichXml(context, element);
                XNamespace xns = "urn:doc";
                context.Element.Add(new XElement(xns + "summary", element.Nodes()));
            }
        }


        public void EnrichEvent(IProcessingContext clone, EventInfo eventInfo)
        {
        }

        #endregion

        private XElement EnrichXml(IProcessingContext context, XElement nodes)
        {
            XmlReader nodeReader = nodes.CreateReader();
            XDocument ret = new XDocument();
            XmlWriter nodeWriter = ret.CreateWriter();

            XsltArgumentList argList = new XsltArgumentList();
            argList.AddExtensionObject("urn:lostdoc-core", new AssetVersionResolver(context));
            this._xslTransform.Transform(nodeReader, argList, nodeWriter);
            nodeWriter.Close();
            return ret.Root;
        }

        public void Load(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (!File.Exists(path))
                throw new FileNotFoundException("File not found: " + path, path);

            this._doc = XDocument.Load(path);
        }
    }
}