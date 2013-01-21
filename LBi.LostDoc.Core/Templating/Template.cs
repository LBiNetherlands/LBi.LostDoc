/*
 * Copyright 2012,2013 LBi Netherlands B.V.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using LBi.LostDoc.Core.Diagnostics;
using LBi.LostDoc.Core.Templating.AssetResolvers;
using LBi.LostDoc.Core.Templating.XPath;

namespace LBi.LostDoc.Core.Templating
{
    public class Template 
    {
        private readonly TemplateResolver _resolver;
        private readonly ObjectCache _cache;
        private string _basePath;
        private FileResolver _fileResolver;
        private List<IAssetUriResolver> _resolvers;

        private XDocument _templateDefinition;

        public event EventHandler<ProgressArgs> Progress;

        protected virtual void OnProgress(int percent)
        {
            EventHandler<ProgressArgs> handler = this.Progress;
            if (handler != null)
                handler(this, new ProgressArgs(percent));
        }

        public Template(TemplateResolver resolver)
        {
            this._cache = new MemoryCache("TemplateCache");
            this._resolver = resolver;
            this._fileResolver = new FileResolver();
            this._resolvers = new List<IAssetUriResolver>();
            this._resolvers.Add(this._fileResolver);
            this._resolvers.Add(new MsdnResolver());
        }

        #region LoadFrom Template

        public virtual void Load(string path)
        {
            using (Stream str = this._resolver.OpenFile(path))
                _templateDefinition = XDocument.Load(str);

            this._basePath = Path.GetDirectoryName(path);
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
                            ConditionExpression = GetAttributeValueOrDefault(elem, "condition"),
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

        private IEnumerable<XPathVariable> ParseParams(IEnumerable<XElement> elements)
        {
            foreach (XElement elem in elements)
            {
                if (elem.Name.LocalName == "with-param")
                {
                    yield return new XPathVariable
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
            using (Stream str = this._resolver.OpenFile(Path.Combine(this._basePath, name)))
            {
                XmlReader reader = XmlReader.Create(str, new XmlReaderSettings { CloseInput = true, });
                XsltSettings settings = new XsltSettings(false, true);
                XmlResolver resolver = new XmlFileProviderResolver(this._resolver, this._basePath);
                ret.Load(reader, settings, resolver);
            }

            return ret;
        }

        #endregion

        protected virtual IEnumerable<StylesheetApplication> DiscoverWork(TemplateData templateData, Stylesheet stylesheet)
        {
            TraceSources.TemplateSource.TraceInformation("Processing stylesheet instructions: {0}",
                                                         (object)stylesheet.Name);

            CustomXsltContext xpathContext = CreateCustomXsltContext(templateData.IgnoredVersionComponent);

            XElement[] inputElements =
                templateData.XDocument.XPathSelectElements(stylesheet.SelectExpression, xpathContext).ToArray();

            foreach (XElement inputElement in inputElements)
            {
                // create resolver
                // ReSharper disable AccessToModifiedClosure
                Func<string, object> ssResolver = v => EvalVariable(stylesheet.Variables, xpathContext, inputElement, v);
                // ReSharper restore AccessToModifiedClosure

                // attach resolver
                xpathContext.OnResolveVariable += ssResolver;

                string saveAs = ResultToString(inputElement.XPathEvaluate(stylesheet.OutputExpression, xpathContext));
                string version = ResultToString(inputElement.XPathEvaluate(stylesheet.VersionExpression, xpathContext));
                string assetId = ResultToString(inputElement.XPathEvaluate(stylesheet.AssetIdExpression, xpathContext));
                List<AssetIdentifier> aliases = new List<AssetIdentifier>();
                List<AssetSection> sections = new List<AssetSection>();

                // eval condition, shortcut and log instead of wrapping entire loop in if
                if (!EvalCondition(xpathContext, inputElement, stylesheet.ConditionExpression))
                {
                    TraceSources.TemplateSource.TraceVerbose("{0}, {1} => Condition not met", assetId, version);
                    //detach resolver
                    xpathContext.OnResolveVariable -= ssResolver;
                    continue;
                }

                Uri newUri = new Uri(saveAs, UriKind.RelativeOrAbsolute);

                // register url
                this._fileResolver.Add(assetId, new Version(version), ref newUri);

                TraceSources.TemplateSource.TraceVerbose("{0}, {1} => {2}", assetId, version, newUri.ToString());

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
                        // ReSharper disable AccessToForEachVariableInClosure
                        Func<string, object> aliasResolver =
                            v =>
                            {
                                if (HasVariable(alias, v))
                                    return EvalVariable(alias.Variables, xpathContext, aliasInputElement, v);

                                return EvalVariable(stylesheet.Variables, xpathContext, inputElement, v);
                            };
                        // ReSharper restore AccessToForEachVariableInClosure

                        xpathContext.OnResolveVariable += aliasResolver;

                        string aliasVersion =
                            ResultToString(aliasInputElement.XPathEvaluate(alias.VersionExpression, xpathContext));
                        string aliasAssetId =
                            ResultToString(aliasInputElement.XPathEvaluate(alias.AssetIdExpression, xpathContext));

                        // eval condition
                        if (EvalCondition(xpathContext, aliasInputElement, alias.ConditionExpression))
                        {
                            this._fileResolver.Add(aliasAssetId, new Version(aliasVersion), newUri);
                            aliases.Add(AssetIdentifier.Parse(aliasAssetId));
                            TraceSources.TemplateSource.TraceVerbose("{0}, {1} (Alias) => {2}", aliasAssetId,
                                                                     aliasVersion,
                                                                     newUri.ToString());
                        }
                        else
                        {
                            TraceSources.TemplateSource.TraceVerbose("{0}, {1} (Alias) => Condition not met",
                                                                     assetId,
                                                                     version);
                        }

                        xpathContext.OnResolveVariable -= aliasResolver;
                    }
                }

                // sections
                foreach (SectionRegistration section in stylesheet.Sections)
                {
                    xpathContext.OnResolveVariable += ssResolver;
                    XElement[] sectionInputElements = inputElement.XPathSelectElements(section.SelectExpression, xpathContext).ToArray();
                    xpathContext.OnResolveVariable -= ssResolver;

                    foreach (XElement sectionInputElement in sectionInputElements)
                    {
                        // ReSharper disable AccessToForEachVariableInClosure
                        // ReSharper disable AccessToModifiedClosure
                        Func<string, object> sectionResolver =
                            v =>
                            {

                                if (HasVariable(section, v))

                                    return EvalVariable(section.Variables, xpathContext, sectionInputElement, v);

                                return EvalVariable(stylesheet.Variables, xpathContext, inputElement, v);
                            };
                        // ReSharper enable AccessToModifiedClosure
                        // ReSharper restore AccessToForEachVariableInClosure

                        xpathContext.OnResolveVariable += sectionResolver;

                        string sectionName =
                            ResultToString(sectionInputElement.XPathEvaluate(section.NameExpression, xpathContext));
                        string sectionVersion =
                            ResultToString(sectionInputElement.XPathEvaluate(section.VersionExpression, xpathContext));
                        string sectionAssetId =
                            ResultToString(sectionInputElement.XPathEvaluate(section.AssetIdExpression, xpathContext));

                        // eval condition
                        if (EvalCondition(xpathContext, sectionInputElement, section.ConditionExpression))
                        {
                            Uri sectionUri = new Uri(newUri + "#" + sectionName, UriKind.Relative);
                            this._fileResolver.Add(sectionAssetId, new Version(sectionVersion), sectionUri);
                            TraceSources.TemplateSource.TraceVerbose("{0}, {1}, (Section: {2}) => {3}",
                                                                     sectionAssetId,
                                                                     sectionVersion,
                                                                     sectionName,
                                                                     sectionUri.ToString());

                            sections.Add(new AssetSection(AssetIdentifier.Parse(sectionAssetId), sectionName, sectionUri));
                        }
                        else
                        {
                            TraceSources.TemplateSource.TraceVerbose("{0}, {1}, (Section: {2}) => Condition not met",
                                                                       sectionAssetId,
                                                                       sectionVersion,
                                                                       sectionName);
                        }

                        xpathContext.OnResolveVariable -= sectionResolver;

                    }
                }


                xpathContext.OnResolveVariable += ssResolver;
                var xsltParams = ResolveXsltParams(stylesheet.XsltParams, inputElement, xpathContext).ToArray();
                xpathContext.OnResolveVariable -= ssResolver;

                yield return new StylesheetApplication
                                 {
                                     StylesheetName = stylesheet.Name,
                                     Asset = new AssetIdentifier(assetId, new Version(version)),
                                     Aliases = aliases, /* list of AssetIdentifiers */
                                     Sections = sections, /* list of AssetSection */
                                     SaveAs = newUri.ToString(),
                                     Transform = stylesheet.Transform,
                                     InputElement = inputElement,
                                     XsltParams = xsltParams
                                 };
            }
        }

        private static IEnumerable<KeyValuePair<string, object>> ResolveXsltParams(IEnumerable<XPathVariable> xsltParams, XElement contextElement, XsltContext xpathContext)
        {
            foreach (var param in xsltParams)
            {
                object val = contextElement.XPathEvaluate(param.ValueExpression, xpathContext);
                if (!(val is string) && val is IEnumerable)
                {
                    object[] data = ((IEnumerable)val).Cast<object>().ToArray();

                    if (data.Length == 1)
                    {
                        if (data[0] is XAttribute)
                            val = ((XAttribute)data[0]).Value;
                        else if (data[0] is XCData)
                            val = ((XCData)data[0]).Value;
                        else if (data[0] is XText)
                            val = ((XText)data[0]).Value;
                        else if (data[0] is XNode)
                            val = ((XNode)data[0]).CreateNavigator();
                    }
                    else
                        val = data.Cast<XNode>().Select(n => n.CreateNavigator()).ToArray();
                }

                yield return new KeyValuePair<string, object>(param.Name, val);
            }
        }

        private static bool HasVariable(AliasRegistration alias, string variable)
        {
            return alias.Variables.SingleOrDefault(v => v.Name.Equals(variable, StringComparison.Ordinal)) != null;
        }

        private static object EvalVariable(IEnumerable<XPathVariable> variables,
                                           CustomXsltContext xpathContext,
                                           XElement inputElement,
                                           string variable)
        {
            XPathVariable xpathVar = variables.Single(v => v.Name.Equals(variable, StringComparison.Ordinal));
            return inputElement.XPathEvaluate(xpathVar.ValueExpression, xpathContext);
        }


        protected virtual ParsedTemplate PrepareTemplate(TemplateData templateData)
        {
            // clone orig doc
            XDocument workingDoc = new XDocument(this._templateDefinition);

            XAttribute templateInheritsAttr = workingDoc.Root.Attribute("inherits");
            if (templateInheritsAttr != null)
            {
                // load 
            }

            // start by loading any parameters as they are needed for meta-template evaluation
            Dictionary<string, XPathVariable> globalParams = new Dictionary<string, XPathVariable>();

            XElement[] paramNodes = workingDoc.Root.Elements("parameter").ToArray();
            foreach (XElement paramNode in paramNodes)
            {
                var tmplVar = new XPathVariable
                                  {
                                      Name = paramNode.Attribute("name").Value,
                                      ValueExpression =
                                          paramNode.Attribute("select") == null
                                              ? null
                                              : paramNode.Attribute("select").Value,
                                  };
                globalParams.Add(tmplVar.Name, tmplVar);
            }


            CustomXsltContext customContext = new CustomXsltContext();
            Func<string, object> onFailedResolve =
                s =>
                {
                    throw new InvalidOperationException(
                        String.Format("Parameter '{0}' could not be resolved.", s));
                };

            customContext.OnResolveVariable +=
                s =>
                {
                    XPathVariable var;

                    // if it's defined
                    if (globalParams.TryGetValue(s, out var))
                    {
                        // see if the user provided a value
                        object value;
                        if (templateData.Arguments.TryGetValue(s, out value))
                            return value;

                        // evaluate default value
                        if (!String.IsNullOrWhiteSpace(var.ValueExpression))
                            return workingDoc.XPathEvaluate(var.ValueExpression,
                                                            customContext);
                    }

                    return onFailedResolve(s);
                };

            // check for meta-template directives and expand
            XElement metaNode = workingDoc.Root.Elements("meta-template").FirstOrDefault();

            // we're going to need this later
            XmlFileProviderResolver fileResolver = new XmlFileProviderResolver(this._resolver, this._basePath);

            while (metaNode != null)
            {
                if (EvalCondition(customContext, metaNode, this.GetAttributeValueOrDefault(metaNode, "condition")))
                {
                    #region Debug conditional

#if DEBUG
                    const bool debugEnabled = true;
#else
                    const bool debugEnabled = false;
#endif

                    #endregion

                    XslCompiledTransform metaTransform = this.LoadStylesheet(metaNode.Attribute("stylesheet").Value);

                    XsltArgumentList xsltArgList = new XsltArgumentList();

                    // TODO this is a quick fix/hack
                    xsltArgList.AddExtensionObject("urn:lostdoc-core", new TemplateXsltExtensions(null, null));

                    var metaParamNodes = metaNode.Elements("with-param");

                    foreach (XElement paramNode in metaParamNodes)
                    {
                        string pName = paramNode.Attribute("name").Value;
                        string pExpr = paramNode.Attribute("select").Value;

                        xsltArgList.AddParam(pName,
                                             string.Empty,
                                             workingDoc.XPathEvaluate(pExpr, customContext));
                    }

                    XDocument outputDoc = new XDocument();
                    using (XmlWriter outputWriter = outputDoc.CreateWriter())
                    {
                        metaTransform.Transform(workingDoc.CreateNavigator(),
                                                xsltArgList,
                                                outputWriter,
                                                fileResolver);
                    }


                    TraceSources.TemplateSource.TraceVerbose("Template after transformation by {0}",
                                                             metaNode.Attribute("stylesheet").Value);

                    TraceSources.TemplateSource.TraceData(TraceEventType.Verbose, 1, outputDoc.CreateNavigator());

                    workingDoc = outputDoc;
                }
                else
                {
                    // didn't process, so remove it
                    metaNode.Remove();
                }


                // select next template
                metaNode = workingDoc.Root.Elements("meta-template").FirstOrDefault();
            }

            // loading template
            List<Stylesheet> stylesheets = new List<Stylesheet>();
            List<Resource> resources = new List<Resource>();
            List<Index> indices = new List<Index>();
            foreach (XElement elem in workingDoc.Root.Elements())
            {
                // we alread proessed the parameters
                if (elem.Name.LocalName == "parameter")
                    continue;

                if (elem.Name.LocalName == "apply-stylesheet")
                {
                    stylesheets.Add(this.ParseStylesheet(stylesheets, elem));
                }
                else if (elem.Name.LocalName == "index")
                {
                    indices.Add(this.ParseIndex(elem));
                }
                else if (elem.Name.LocalName == "include-resource")
                {
                    resources.Add(new Resource
                                      {
                                          Path = elem.Attribute("path").Value,
                                          ConditionExpression = this.GetAttributeValueOrDefault(elem, "condition")
                                      });
                }
                else
                {
                    throw new Exception("Unknown element: " + elem.Name.LocalName);
                }
            }

            return new ParsedTemplate
                       {
                           Resources = resources.ToArray(),
                           Stylesheets = stylesheets.ToArray(),
                           Indices = indices.ToArray()
                       };
        }

        protected virtual Index ParseIndex(XElement elem)
        {
            return new Index(elem.Attribute("name").Value,
                             elem.Attribute("match").Value,
                             elem.Attribute("key").Value);
        }

        private static bool EvalCondition(CustomXsltContext customContext, XNode contextNode, string condition)
        {
            bool shouldApply;
            if (string.IsNullOrWhiteSpace(condition))
                shouldApply = true;
            else
            {
                object value = contextNode.XPathEvaluate(condition, customContext);
                shouldApply = ResultToBool(value);
            }
            return shouldApply;
        }

        private Stylesheet ParseStylesheet(IEnumerable<Stylesheet> stylesheets, XElement elem)
        {

            IEnumerable<XAttribute> variableAttrs =
                elem.Attributes()
                    .Where(a => a.Name.NamespaceName == "urn:lost-doc:template.variable");

            string name = elem.Attribute("name") == null
                              ? Path.GetFileNameWithoutExtension(elem.Attribute("stylesheet").Value)
                              : elem.Attribute("name").Value;
            if (elem.Attribute("name") != null)
                TraceSources.TemplateSource.TraceInformation("Loading stylesheet: {0} ({1})", name, elem.Attribute("stylesheet").Value);
            else
                TraceSources.TemplateSource.TraceInformation("Loading stylesheet: {0}", name);

            string src = elem.Attribute("stylesheet").Value;
            XslCompiledTransform transform;

            Stylesheet match = stylesheets.FirstOrDefault(s => String.Equals(s.Source, src, StringComparison.Ordinal));
            if (match != null)
                transform = match.Transform;
            else
                transform = this.LoadStylesheet(src);


            return new Stylesheet
                       {
                           Source = src,
                           Transform = transform,
                           SelectExpression = elem.Attribute("select").Value,
                           AssetIdExpression = elem.Attribute("assetId").Value,
                           OutputExpression = elem.Attribute("output").Value,
                           VersionExpression = elem.Attribute("version").Value,
                           XsltParams = this.ParseParams(elem.Elements("with-param")).ToArray(),
                           Variables = this.ParseVariables(variableAttrs).ToArray(),
                           Name = name,
                           Sections = this.ParseSectionRegistration(elem.Elements("register-section")).ToArray(),
                           AssetAliases = this.ParseAliasRegistration(elem.Elements("register-alias")).ToArray(),
                           ConditionExpression = GetAttributeValueOrDefault(elem, "condition")
                       };
        }

        private string GetAttributeValueOrDefault(XElement elem, string attName, string defaultValue = null)
        {
            var attr = elem.Attribute(attName);

            if (attr == null)
                return defaultValue;

            return attr.Value;
        }



        /// <summary>
        /// Applies the loaded templates to <paramref name="templateData"/>.
        /// </summary>
        /// <param name="templateData">
        /// Instance of <see cref="TemplateData"/> containing the various input data needed. 
        /// </param>
        public virtual TemplateOutput Generate(TemplateData templateData)
        {
            Stopwatch timer = Stopwatch.StartNew();

            ParsedTemplate tmpl = this.PrepareTemplate(templateData);


            // collect all work that has to be done
            List<UnitOfWork> work = new List<UnitOfWork>();

            // resource work units
            work.AddRange(this.DiscoverWork(templateData, tmpl.Resources));

            // stylesheet work units
            {
                List<StylesheetApplication> stylesheetApplications = new List<StylesheetApplication>();
                foreach (Stylesheet stylesheet in tmpl.Stylesheets)
                {
                    stylesheetApplications.AddRange(this.DiscoverWork(templateData, stylesheet));
                }

                var duplicates =
                    stylesheetApplications.GroupBy(sa => sa.SaveAs, StringComparer.OrdinalIgnoreCase)
                                          .Where(g => g.Count() > 1);

                foreach (var group in duplicates)
                {
                    TraceSources.TemplateSource.TraceError("Duplicate work unit target ({0}) generated from: {1}",
                                                           group.Key,
                                                           string.Join(", ",
                                                                       group.Select(sa => '\'' + sa.StylesheetName + '\'')));

                    foreach (var workunit in group.Skip(1))
                    {
                        stylesheetApplications.Remove(workunit);
                    }
                }

                work.AddRange(stylesheetApplications);
            }

            TraceSources.TemplateSource.TraceInformation("Generating {0:N0} documents from {1:N0} stylesheets.",
                                                         work.Count, tmpl.Stylesheets.Length);

            ConcurrentBag<WorkUnitResult> results = new ConcurrentBag<WorkUnitResult>();

            // create context
            ITemplatingContext context = new TemplatingContext(this._cache,
                                                               this._basePath,
                                                               templateData,
                                                               this._resolvers,
                                                               this._resolver);


            // fill indices
            using (TraceSources.TemplateSource.TraceActivity("Indexing input document"))
            {
                var customXsltContext = CreateCustomXsltContext(templateData.IgnoredVersionComponent);
                foreach (var index in tmpl.Indices)
                {
                    TraceSources.TemplateSource.TraceVerbose("Adding index {0} (match: '{1}', key: '{1}')",
                                                             index.Name,
                                                             index.MatchExpr,
                                                             index.KeyExpr);
                    context.DocumentIndex.AddKey(index.Name, index.MatchExpr, index.KeyExpr, customXsltContext);
                }

                TraceSources.TemplateSource.TraceInformation("Indexing...");
                context.DocumentIndex.BuildIndexes();
            }


            int totalCount = work.Count;
            long lastProgress = Stopwatch.GetTimestamp();
            int processed = 0;
            // process all units of work
            ParallelOptions parallelOptions = new ParallelOptions
                                                  {
                                                      //MaxDegreeOfParallelism = 1
                                                  };
            Parallel.ForEach(work,
                             parallelOptions,
                             uow =>
                             {
                                 results.Add(uow.Execute(context));
                                 int c = Interlocked.Increment(ref processed);
                                 long lp = Interlocked.Read(ref lastProgress);
                                 if ((Stopwatch.GetTimestamp() - lp) / (double)Stopwatch.Frequency > 5.0)
                                 {
                                     if (Interlocked.CompareExchange(ref lastProgress,
                                                                     Stopwatch.GetTimestamp(),
                                                                     lp) == lp)
                                     {
                                         TraceSources.TemplateSource.TraceInformation(
                                             "Progress: {0:P1} ({1:N0}/{2:N0})",
                                             c / (double)totalCount,
                                             c,
                                             totalCount);
                                     }
                                 }
                             });

            // stop timing
            timer.Stop();

            // prepare stats
            Dictionary<Type, WorkUnitResult[]> resultGroups =
                results.GroupBy(ps => ps.WorkUnit.GetType()).ToDictionary(g => g.Key, g => g.ToArray());



            var stylesheetStats =
                resultGroups[typeof(StylesheetApplication)]
                .GroupBy(r => ((StylesheetApplication)r.WorkUnit).StylesheetName);

            foreach (var statGroup in stylesheetStats)
            {
                TraceSources.TemplateSource.TraceInformation("Applied stylesheet '{0}' {1:N0} times in {2:N0} ms (min: {3:N0}, mean {4:N0}, max {5:N0}, avg: {6:N0})",
                                                             statGroup.Key,
                                                             statGroup.Count(),
                                                             statGroup.Sum(ps => ps.Duration) / 1000.0,
                                                             statGroup.Min(ps => ps.Duration) / 1000.0,
                                                             statGroup.Skip(statGroup.Count() / 2).Take(1).Single().Duration / 1000.0,
                                                             statGroup.Max(ps => ps.Duration) / 1000.0,
                                                             statGroup.Average(ps => ps.Duration) / 1000.0);
            }

            var resourceStats = resultGroups[typeof(ResourceDeployment)];

            foreach (var statGroup in resourceStats)
            {
                TraceSources.TemplateSource.TraceInformation("Deployed resource '{0}' in {1:N0} ms",
                                                             ((ResourceDeployment)statGroup.WorkUnit).ResourcePath,
                                                             statGroup.Duration);
            }


            TraceSources.TemplateSource.TraceInformation("Documentation generated in {0:N1} seconds (processing time: {1:N1} seconds)",
                                                         timer.Elapsed.TotalSeconds,
                                                         results.Sum(ps => ps.Duration) / 1000000.0);


            return new TemplateOutput(results.ToArray());
        }

        private IEnumerable<UnitOfWork> DiscoverWork(TemplateData templateData, Resource[] resources)
        {
            for (int i = 0; i < resources.Length; i++)
            {
                CustomXsltContext xpathContext = CreateCustomXsltContext(templateData.IgnoredVersionComponent);
                if (EvalCondition(xpathContext, templateData.XDocument.Root, resources[i].ConditionExpression))
                    yield return new ResourceDeployment(resources[i].Path);
            }

        }


        private static CustomXsltContext CreateCustomXsltContext(VersionComponent? ignoredVersionComponent)
        {
            CustomXsltContext xpathContext = new CustomXsltContext();
            xpathContext.RegisterFunction(string.Empty, "get-id", new XsltContextAssetIdGetter());
            xpathContext.RegisterFunction(string.Empty, "get-version", new XsltContextAssetVersionGetter());
            xpathContext.RegisterFunction(string.Empty, "substring-before-last", new XsltContextSubstringBeforeLastFunction());
            xpathContext.RegisterFunction(string.Empty, "substring-after-last", new XsltContextSubstringAfterLastFunction());
            xpathContext.RegisterFunction(string.Empty, "iif", new XsltContextTernaryOperator());
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


        /*
         * a number is true if and only if it is neither positive or negative zero nor NaN
         * a node-set is true if and only if it is non-empty
         * a string is true if and only if its length is non-zero
         * an object of a type other than the four basic types is converted to a boolean in a way that is dependent on that type
         */
        protected internal static bool ResultToBool(object res)
        {
            bool ret;
            if (res == null)
                ret = false;
            else if (res is string)
                ret = ((string)res).Length > 0;
            else if (res is bool)
                ret = (bool)res;
            else if (res is double)
            {
                double d = (double)res;
                ret = (d > 0.0 || d < 0.0) && !double.IsNaN(d);
            }
            else if (res is IEnumerable)
            {
                object first = ((IEnumerable)res).Cast<object>().FirstOrDefault();
                ret = ResultToBool(first);
            }
            else
                ret = false;


            return ret;
        }
    }
}
