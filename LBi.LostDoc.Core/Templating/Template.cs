using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using LBi.Diagnostics;
using LBi.LostDoc.Core.Diagnostics;
using LBi.LostDoc.Core.Templating.AssetResolvers;
using LBi.LostDoc.Core.Templating.XPath;

namespace LBi.LostDoc.Core.Templating
{
    public class Template
    {
        private readonly IFileProvider _fileProvider;
        private string _basePath;
        private FileResolver _fileResolver;
        private List<IAssetUriResolver> _resolvers;
        private string[] _resources;
        private Stylesheet[] _stylesheets;

        public Template(IFileProvider fileProvider)
        {
            this._fileProvider = fileProvider;
            this._fileResolver = new FileResolver();
            this._resolvers = new List<IAssetUriResolver>();
            this._resolvers.Add(this._fileResolver);
            this._resolvers.Add(new MsdnResolver());
        }

        #region Internal representation

        #region Nested type: AliasRegistration

        protected class AliasRegistration
        {
            public XPathVariable[] Variables { get; set; }
            public string SelectExpression { get; set; }
            public string AssetIdExpression { get; set; }
            public string VersionExpression { get; set; }
        }

        #endregion

        #region Nested type: ProcessingStatistics

        protected class ProcessingStatistics
        {
            public string StylesheetName { get; set; }

            public string SavedAs { get; set; }

            /// <summary>
            ///   Micro seconds.
            /// </summary>
            public long Duration { get; set; }
        }

        #endregion

        #region Nested type: SectionRegistration

        protected class SectionRegistration : AliasRegistration
        {
            public string NameExpression { get; set; }
        }

        #endregion

        #region Nested type: Stylesheet

        protected class Stylesheet
        {
            public string VersionExpression { get; set; }
            public XslCompiledTransform Transform { get; set; }
            public string Name { get; set; }
            public string SelectExpression { get; set; }
            public string AssetIdExpression { get; set; }
            public string OutputExpression { get; set; }
            public StylesheetParam[] XsltParams { get; set; }
            public XPathVariable[] Variables { get; set; }
            public SectionRegistration[] Sections { get; set; }
            public AliasRegistration[] AssetAliases { get; set; }
        }

        #endregion

        #region Nested type: StylesheetParam

        protected class StylesheetParam
        {
            public string Name { get; set; }
            public string ValueExpression { get; set; }
        }

        #endregion

        #region Nested type: UnitOfWork

        protected class UnitOfWork
        {
            public XslCompiledTransform Transform { get; set; }
            public IEnumerable<KeyValuePair<string, object>> XsltParams { get; set; }
            public string SaveAs { get; set; }

            public XElement InputElement { get; set; }

            public AssetIdentifier Asset { get; set; }

            public string StylesheetName { get; set; }
        }

        #endregion

        #region Nested type: XPathVariable

        protected class XPathVariable
        {
            public string Name { get; set; }
            public string ValueExpression { get; set; }
        }

        #endregion

        #endregion

        #region Load Template

        public void Load(string path)
        {
            XDocument doc;

            using (Stream str = this._fileProvider.OpenFile(path))
                doc = XDocument.Load(str);

            List<Stylesheet> stylesheets = new List<Stylesheet>();
            this._basePath = Path.GetDirectoryName(path);

            List<string> resources = new List<string>();
            foreach (XElement elem in doc.Root.Elements())
            {
                if (elem.Name.LocalName == "apply-stylesheet")
                {
                    stylesheets.Add(new Stylesheet
                                        {
                                            Transform = this.LoadStylesheet(elem.Attribute("stylesheet").Value),
                                            SelectExpression = elem.Attribute("select").Value,
                                            AssetIdExpression = elem.Attribute("assetId").Value,
                                            OutputExpression = elem.Attribute("output").Value,
                                            VersionExpression = elem.Attribute("version").Value,
                                            XsltParams = this.ParseParams(elem.Elements("with-param")).ToArray(),
                                            Variables =
                                                this.ParseVariables(
                                                                    elem.Attributes().Where(
                                                                                            a =>
                                                                                            a.Name.NamespaceName ==
                                                                                            "urn:lost-doc:template.variable"))
                                                .ToArray(),
                                            Name = elem.Attribute("name") == null
                                                       ? Path.GetFileNameWithoutExtension(
                                                                                          elem.Attribute("stylesheet").
                                                                                              Value)
                                                       : elem.Attribute("name").Value,
                                            Sections =
                                                this.ParseSectionRegistration(elem.Elements("register-section")).ToArray
                                                (),
                                            AssetAliases =
                                                this.ParseAliasRegistration(elem.Elements("register-alias")).ToArray()
                                        });
                }
                else if (elem.Name.LocalName == "include-resource")
                {
                    resources.Add(elem.Attribute("path").Value);
                }
                else
                {
                    throw new Exception("Unknown element: " + elem.Name.LocalName);
                }
            }

            this._stylesheets = stylesheets.ToArray();
            this._resources = resources.ToArray();
        }

        private IEnumerable<AliasRegistration> ParseAliasRegistration(IEnumerable<XElement> elements)
        {
            foreach (XElement elem in elements)
            {
                yield return
                    new AliasRegistration
                        {
                            Variables =
                                this.ParseVariables(
                                                    elem.Attributes().Where(
                                                                            a =>
                                                                            a.Name.NamespaceName ==
                                                                            "urn:lost-doc:template.variable")).ToArray(),
                            SelectExpression = elem.Attribute("select") == null ? "." : elem.Attribute("select").Value,
                            AssetIdExpression = elem.Attribute("assetId").Value,
                            VersionExpression = elem.Attribute("version").Value,
                        };
            }
        }

        private IEnumerable<SectionRegistration> ParseSectionRegistration(IEnumerable<XElement> elements)
        {
            foreach (XElement elem in elements)
            {
                yield return
                    new SectionRegistration
                        {
                            SelectExpression = elem.Attribute("select").Value,
                            NameExpression = elem.Attribute("name").Value,
                            AssetIdExpression = elem.Attribute("assetId").Value,
                            VersionExpression = elem.Attribute("version").Value,
                            Variables =
                                this.ParseVariables(
                                                    elem.Attributes().Where(
                                                                            a =>
                                                                            a.Name.NamespaceName ==
                                                                            "urn:lost-doc:template.variable")).ToArray(),
                        };
            }
        }

        private IEnumerable<XPathVariable> ParseVariables(IEnumerable<XAttribute> xAttributes)
        {
            return
                xAttributes.Select(
                                   xAttribute =>
                                   new XPathVariable
                                       {
                                           Name = xAttribute.Name.LocalName,
                                           ValueExpression = xAttribute.Value
                                       });
        }

        private IEnumerable<StylesheetParam> ParseParams(IEnumerable<XElement> elements)
        {
            foreach (XElement elem in elements)
            {
                if (elem.Name.LocalName == "with-param")
                {
                    yield return new StylesheetParam
                                     {
                                         Name = elem.Attribute("name").Value,
                                         ValueExpression = elem.Attribute("select").Value
                                     };
                }
                else
                {
                    throw new Exception("Unknown element:" + elem.Name.LocalName);
                }
            }
        }

        /// <summary>
        /// The load stylesheet.
        /// </summary>
        /// <param name="name">
        /// </param>
        /// <returns>
        /// </returns>
        private XslCompiledTransform LoadStylesheet(string name)
        {
            XslCompiledTransform ret = new XslCompiledTransform(true);
            using (Stream str = this._fileProvider.OpenFile(Path.Combine(this._basePath, name)))
            {
                XmlReader reader = XmlReader.Create(str, new XmlReaderSettings { CloseInput = true, });
                XsltSettings settings = new XsltSettings(false, true);
                XmlResolver resolver = new XmlFileProviderResolver(this._fileProvider, this._basePath);
                ret.Load(reader, settings, resolver);
            }

            return ret;
        }

        #endregion

        protected virtual IEnumerable<UnitOfWork> DiscoverWork(TemplateData templateData, Stylesheet stylesheet)
        {
            TraceSources.TemplateSource.TraceInformation("Processing stylesheet instructions: {0}",
                                                         (object)stylesheet.Name);

            CustomXsltContext xpathContext = CreateCustomXsltContext(templateData.IgnoredVersionComponent);
            XElement[] inputElements =
                templateData.Document.XPathSelectElements(stylesheet.SelectExpression, xpathContext).ToArray();

            foreach (XElement inputElement in inputElements)
            {
                // create resolver
                // ReSharper disable AccessToModifiedClosure
                Func<string, object> ssResolver = v => EvalStylesheetVariable(stylesheet, xpathContext, inputElement, v);
                // ReSharper restore AccessToModifiedClosure

                // attach resolver
                xpathContext.OnResolveVariable += ssResolver;

                string saveAs = ResultToString(inputElement.XPathEvaluate(stylesheet.OutputExpression, xpathContext));
                string version = ResultToString(inputElement.XPathEvaluate(stylesheet.VersionExpression, xpathContext));
                string assetId = ResultToString(inputElement.XPathEvaluate(stylesheet.AssetIdExpression, xpathContext));

                Uri newUri = new Uri(saveAs, UriKind.RelativeOrAbsolute);
                TraceSources.TemplateSource.TraceVerbose("{0}, {1} => {2}", assetId, version, saveAs);

                // register url
                this._fileResolver.Add(assetId, new Version(version), newUri);

                // detach resolver
                xpathContext.OnResolveVariable -= ssResolver;

                // aliases
                foreach (AliasRegistration alias in stylesheet.AssetAliases)
                {
                    xpathContext.OnResolveVariable += ssResolver;
                    XElement[] aliasInputElements =
                        inputElement.XPathSelectElements(alias.SelectExpression, xpathContext).ToArray();
                    xpathContext.OnResolveVariable -= ssResolver;

                    foreach (XElement aliasInputElement in aliasInputElements)
                    {
                        // ReSharper disable AccessToModifiedClosure
                        Func<string, object> aliasResolver =
                            v =>
                            {
                                if (HasVariable(alias, v))
                                    return EvalVariable(alias, xpathContext, aliasInputElement, v);

                                return EvalStylesheetVariable(stylesheet, xpathContext, inputElement, v);
                            };
                        // ReSharper restore AccessToModifiedClosure

                        xpathContext.OnResolveVariable += aliasResolver;

                        string aliasVersion =
                            ResultToString(aliasInputElement.XPathEvaluate(alias.VersionExpression, xpathContext));
                        string aliasAssetId =
                            ResultToString(aliasInputElement.XPathEvaluate(alias.AssetIdExpression, xpathContext));

                        xpathContext.OnResolveVariable -= aliasResolver;

                        this._fileResolver.Add(aliasAssetId, new Version(aliasVersion), newUri);
                        TraceSources.TemplateSource.TraceVerbose("{0}, {1} (Alias) => {2}", aliasAssetId, aliasVersion,
                                                                 saveAs);
                    }
                }

                // sections
                foreach (SectionRegistration section in stylesheet.Sections)
                {
                    xpathContext.OnResolveVariable += ssResolver;
                    XElement[] sectionInputElements =
                        inputElement.XPathSelectElements(section.SelectExpression, xpathContext).ToArray();
                    xpathContext.OnResolveVariable -= ssResolver;

                    foreach (XElement sectionInputElement in sectionInputElements)
                    {
                        // ReSharper disable AccessToModifiedClosure
                        Func<string, object> sectionResolver =
                            v =>
                            {
                                if (HasVariable(section, v))
                                    return EvalVariable(section, xpathContext, sectionInputElement, v);

                                return EvalStylesheetVariable(stylesheet, xpathContext, inputElement, v);
                            };
                        // ReSharper enable AccessToModifiedClosure

                        xpathContext.OnResolveVariable += sectionResolver;

                        string sectionName =
                            ResultToString(sectionInputElement.XPathEvaluate(section.NameExpression, xpathContext));
                        string sectionVersion =
                            ResultToString(sectionInputElement.XPathEvaluate(section.VersionExpression, xpathContext));
                        string sectionAssetId =
                            ResultToString(sectionInputElement.XPathEvaluate(section.AssetIdExpression, xpathContext));

                        xpathContext.OnResolveVariable -= sectionResolver;


                        this._fileResolver.Add(sectionAssetId, new Version(sectionVersion),
                                               new Uri(newUri + "#" + sectionName, UriKind.Relative));
                        TraceSources.TemplateSource.TraceVerbose("{0}, {1}, (Section: {2}) => {3}",
                                                                 sectionAssetId,
                                                                 sectionVersion,
                                                                 sectionName,
                                                                 saveAs);
                    }
                }


                List<KeyValuePair<string, object>> xsltParams = new List<KeyValuePair<string, object>>();
                xpathContext.OnResolveVariable += ssResolver;
                foreach (StylesheetParam param in stylesheet.XsltParams)
                {
                    object val = inputElement.XPathEvaluate(param.ValueExpression);
                    if (!(val is string) && val is IEnumerable)
                    {
                        object[] data = ((IEnumerable)val).Cast<object>().ToArray();

                        if (data.Length == 1)
                        {
                            if (data[0] is XAttribute)
                                val = ((XAttribute)data[0]).Value;
                            else if (data[0] is XText)
                                val = ((XText)data[0]).Value;
                            else if (data[0] is XCData)
                                val = ((XCData)data[0]).Value;
                            else if (data[0] is XNode)
                                val = ((XNode)data[0]).CreateNavigator();
                        }
                        else
                            val = data.Cast<XNode>().Select(n => n.CreateNavigator()).ToArray();
                    }

                    xsltParams.Add(new KeyValuePair<string, object>(param.Name, val));
                }

                xpathContext.OnResolveVariable -= ssResolver;

                yield return new UnitOfWork
                                 {
                                     StylesheetName = stylesheet.Name,
                                     Asset = new AssetIdentifier(assetId, new Version(version)),
                                     SaveAs = saveAs,
                                     Transform = stylesheet.Transform,
                                     InputElement = inputElement,
                                     XsltParams = xsltParams
                                 };
            }
        }

        private static bool HasVariable(AliasRegistration alias, string variable)
        {
            return alias.Variables.SingleOrDefault(v => v.Name.Equals(variable, StringComparison.Ordinal)) != null;
        }

        private static object EvalVariable(AliasRegistration alias, CustomXsltContext xpathContext,
                                           XElement inputElement, string variable)
        {
            XPathVariable xpathVar = alias.Variables.Single(v => v.Name.Equals(variable, StringComparison.Ordinal));
            return inputElement.XPathEvaluate(xpathVar.ValueExpression, xpathContext);
        }

        private static object EvalStylesheetVariable(Stylesheet stylesheet, CustomXsltContext xpathContext,
                                                     XElement inputElement, string variable)
        {
            XPathVariable xpathVar = stylesheet.Variables.Single(v => v.Name.Equals(variable, StringComparison.Ordinal));
            return inputElement.XPathEvaluate(xpathVar.ValueExpression, xpathContext);
        }

        /// <summary>
        /// Applies he loaded templates to <paramref name="templateData"/>.
        /// </summary>
        /// <param name="templateData">
        /// Instance of <see cref="TemplateData"/> containing the various input data needed. 
        /// </param>
        /// <param name="outputDir">
        /// The output directory, this is created if it doesn't exist. 
        /// </param>
        public void Generate(TemplateData templateData, string outputDir)
        {
            Stopwatch timer = Stopwatch.StartNew();

            List<UnitOfWork> work = new List<UnitOfWork>();

            // collect all work that has to be done
            foreach (Stylesheet stylesheet in this._stylesheets)
                work.AddRange(this.DiscoverWork(templateData, stylesheet));

            string rootPath = Path.GetFullPath(outputDir);

            // copy resources to output dir
            foreach (string resource in this._resources)
            {
                string target = Path.Combine(rootPath, resource);
                string targetDir = Path.GetDirectoryName(target);
                TraceSources.TemplateSource.TraceInformation("Copying resource: {0}", resource);

                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                using (Stream streamSrc = this._fileProvider.OpenFile(Path.Combine(this._basePath, resource)))
                using (Stream streamDest = File.Create(target))
                {
                    streamSrc.CopyTo(streamDest);
                    streamDest.Close();
                    streamSrc.Close();
                }
            }

            TraceSources.TemplateSource.TraceInformation("Generating {0:N0} documents from {1:N0} stylesheets.",
                                                         work.Count, this._stylesheets.Length);

            ConcurrentBag<ProcessingStatistics> statistcs = new ConcurrentBag<ProcessingStatistics>();
            ConcurrentDictionary<Uri, bool> savedUris = new ConcurrentDictionary<Uri, bool>();

            // process all units of work
           Parallel.ForEach(work,
                             uow =>
                             {
                                 Stopwatch localTimer = Stopwatch.StartNew();

                                 string targetPath = Path.Combine(outputDir, uow.SaveAs);

                                 string targetDir = Path.GetDirectoryName(targetPath);
                                 if (targetDir != null && !Directory.Exists(targetDir))
                                 {
                                     TraceSources.TemplateSource.TraceVerbose("Creating directory: {0}", targetDir);

                                     // noop if exists
                                     Directory.CreateDirectory(targetDir);
                                 }

                                 ITemplatingContext templatingContext = new TemplatingContext
                                                                            {
                                                                                OutputDir = targetPath,
                                                                                InputDocument =
                                                                                    templateData.Document,
                                                                                IgnoredVersionComponent =
                                                                                    templateData.
                                                                                    IgnoredVersionComponent,
                                                                                AssetRedirects =
                                                                                    templateData.AssetRedirects
                                                                            };

                                 Uri newUri = new Uri(uow.SaveAs, UriKind.RelativeOrAbsolute);

                                 if (savedUris.TryAdd(newUri, true))
                                 {
                                     // register xslt params
                                     XsltArgumentList argList = new XsltArgumentList();
                                     foreach (KeyValuePair<string, object> kvp in uow.XsltParams)
                                         argList.AddParam(kvp.Key, string.Empty, kvp.Value);

                                     // and custom extensions
                                     argList.AddExtensionObject("urn:lostdoc-core",
                                                                new TemplateXsltExtensions(templatingContext, newUri,
                                                                                           this._resolvers.ToArray()));

                                     using (FileStream stream = File.Create(targetPath))
                                     using (
                                         XmlWriter xmlWriter = XmlWriter.Create(stream,
                                                                                new XmlWriterSettings
                                                                                    {
                                                                                        CloseOutput = true,
                                                                                        Indent = true
                                                                                    }))
                                     {
                                         TraceSources.TemplateSource.TraceVerbose("{0}, {1} => {2}",
                                                                                  uow.Asset.AssetId,
                                                                                  uow.Asset.Version, uow.SaveAs);
                                         uow.Transform.Transform(templateData.Document.CreateNavigator(), argList,
                                                                 xmlWriter);
                                         xmlWriter.Close();
                                     }
                                 }
                                 else
                                 {
                                     TraceSources.TemplateSource.TraceWarning(
                                                                              "{0}, {1} => Skipped, already generated ({2})",
                                                                              uow.Asset.AssetId, uow.Asset.Version,
                                                                              newUri);
                                 }

                                 localTimer.Stop();
                                 statistcs.Add(new ProcessingStatistics
                                                   {
                                                       StylesheetName = uow.StylesheetName,
                                                       SavedAs = uow.SaveAs,
                                                       Duration =
                                                           (long)
                                                           Math.Round(localTimer.ElapsedTicks /
                                                                      (double)Stopwatch.Frequency * 1000000)
                                                   });
                             });

            IEnumerable<IGrouping<string, ProcessingStatistics>> statGroups = statistcs.GroupBy(ps => ps.StylesheetName);
            foreach (IGrouping<string, ProcessingStatistics> statGroup in statGroups)
            {
                TraceSources.TemplateSource.TraceInformation("Applied stylesheet '{0}' {1:N0} times in {2:N0} ms (min: {3:N0}, max {4:N0}, avg: {5:N0})",
                                                             statGroup.Key,
                                                             statGroup.Count(),
                                                             statGroup.Sum(ps => ps.Duration) / 1000.0,
                                                             statGroup.Min(ps => ps.Duration) / 1000.0,
                                                             statGroup.Max(ps => ps.Duration) / 1000.0,
                                                             statGroup.Average(ps => ps.Duration) / 1000.0);
            }

            timer.Stop();
            TraceSources.TemplateSource.TraceInformation("Documentation generated in {0:N1} seconds (processing time: {1:N1} seconds)",
                                                         timer.Elapsed.TotalSeconds,
                                                         statistcs.Sum(ps => ps.Duration) / 1000000.0);
        }

        private static CustomXsltContext CreateCustomXsltContext(VersionComponent? ignoredVersionComponent)
        {
            CustomXsltContext xpathContext = new CustomXsltContext();
            xpathContext.RegisterFunction(string.Empty, "get-id", new XsltContextAssetIdGetter());
            xpathContext.RegisterFunction(string.Empty, "get-version", new XsltContextAssetVersionGetter());
            xpathContext.RegisterFunction(string.Empty, "get-significant-version", new XsltContextAssetVersionGetter(ignoredVersionComponent));
            xpathContext.RegisterFunction(string.Empty, "coalesce", new XsltContextCoalesceFunction());
            xpathContext.RegisterFunction(string.Empty, "join", new XsltContextJoinFunction());
            return xpathContext;
        }

        private static string ResultToString(object res)
        {
            string ret = res as string;
            if (ret == null)
            {
                if (res is IEnumerable)
                {
                    object first = ((IEnumerable)res).Cast<object>().FirstOrDefault();
                    ret = first as string;
                    if (ret == null)
                    {
                        if (first is XAttribute)
                            ret = ((XAttribute)first).Value;
                        else if (first is XCData)
                            ret = ((XCData)first).Value;
                        else if (first is XText)
                            ret = ((XText)first).Value;
                        else if (first is XElement)
                            ret = ((XElement)first).Value;
                    }
                }
            }

            return ret;
        }
    }
}