/*
 * Copyright 2012-2014 DigitasLBi Netherlands B.V.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{
    public class StylesheetDirective : ITemplateDirective<StylesheetApplication>
    {
        public string ConditionExpression { get; set; }
        public string Name { get; set; }
        public string SelectExpression { get; set; }
        public string InputExpression { get; set; }
        public string OutputExpression { get; set; }
        public XPathVariable[] XsltParams { get; set; }
        public XPathVariable[] Variables { get; set; }
        public SectionRegistration[] Sections { get; set; }
        public AssetRegistration[] AssetRegistrations { get; set; }
        public FileReference Stylesheet { get; set; }

        protected virtual XslCompiledTransform LoadStylesheet(ObjectCache cache)
        {
            string cacheKey = "template://" + this.Stylesheet.Path;

            XslCompiledTransform ret = cache.Get(cacheKey) as XslCompiledTransform;

            if (ret == null)
            {
                ret = new XslCompiledTransform(true);
                using (Stream str = this.Stylesheet.GetStream())
                {
                    XmlReader reader = XmlReader.Create(str, new XmlReaderSettings { CloseInput = true, });
                    XsltSettings settings = new XsltSettings(true, true);
                    XmlResolver resolver = new XmlFileProviderResolver(this.Stylesheet.FileProvider);
                    ret.Load(reader, settings, resolver);
                }

                cache.Add(cacheKey, ret, new CacheItemPolicy { Priority = CacheItemPriority.Default });
            }

            return ret;
        }

        public virtual IEnumerable<StylesheetApplication> DiscoverWork(ITemplateContext context)
        {
            TraceSources.TemplateSource.TraceInformation("Processing stylesheet instructions: {0}",
                                                         (object)this.Name);

            IEnumerable<XNode> inputNodes = XPathServices.ToNodeSequence(context.Document.XPathEvaluate(this.SelectExpression, context.XsltContext));

            foreach (XNode inputNode in inputNodes)
            {
                context.XsltContext.PushVariableScope(inputNode, this.Variables); // 1

                string input = null;
                if (this.InputExpression != null)
                    input = XPathServices.ResultToString(inputNode.XPathEvaluate(this.InputExpression, context.XsltContext));

                string outputPath = XPathServices.ResultToString(inputNode.XPathEvaluate(this.OutputExpression, context.XsltContext));

                List<AssetIdentifier> assetIdentifiers = new List<AssetIdentifier>();
                List<AssetSection> sections = new List<AssetSection>();

                // eval condition, shortcut and log instead of wrapping entire loop in if
                if (!inputNode.EvaluateCondition(this.ConditionExpression, context.XsltContext))
                {
                    TraceSources.TemplateSource.TraceVerbose("Condition not met: {0}", outputPath);
                    context.XsltContext.PopVariableScope(); // 1
                    continue;
                }

                Uri newUri = new Uri(outputPath, UriKind.RelativeOrAbsolute);

                // ensure url is unique
                context.EnsureUniqueUri(ref newUri);

                // asset identifiers
                foreach (AssetRegistration assetRegistration in this.AssetRegistrations)
                {
                    XElement[] assetInputElements = inputNode.XPathSelectElements(assetRegistration.SelectExpression, context.XsltContext).ToArray();

                    foreach (XElement assetInputElement in assetInputElements)
                    {
                        context.XsltContext.PushVariableScope(assetInputElement, assetRegistration.Variables); // 2

                        string version = XPathServices.ResultToString(assetInputElement.XPathEvaluate(assetRegistration.VersionExpression, context.XsltContext));
                        string assetId = XPathServices.ResultToString(assetInputElement.XPathEvaluate(assetRegistration.AssetIdExpression, context.XsltContext));

                        // eval condition
                        if (assetInputElement.EvaluateCondition(assetRegistration.ConditionExpression, context.XsltContext))
                        {
                            
                            AssetIdentifier assetIdentifier = new AssetIdentifier(assetId, version);
                            context.RegisterAssetUri(assetIdentifier, newUri);
                            assetIdentifiers.Add(assetIdentifier);
                            TraceSources.TemplateSource.TraceVerbose("{0}, {1} => {2}",
                                                                     assetId,
                                                                     version,
                                                                     newUri.ToString());
                        }
                        else
                        {
                            TraceSources.TemplateSource.TraceVerbose("{0}, {1} => Condition not met",
                                                                     assetId,
                                                                     version);
                        }
                        context.XsltContext.PopVariableScope(); // 2
                    }
                }

                // sections
                foreach (SectionRegistration section in this.Sections)
                {
                    XElement[] sectionInputElements = inputNode.XPathSelectElements(section.SelectExpression, context.XsltContext).ToArray();

                    foreach (XElement sectionInputElement in sectionInputElements)
                    {
                        context.XsltContext.PushVariableScope(sectionInputElement, section.Variables); // 3

                        string sectionName = XPathServices.ResultToString(sectionInputElement.XPathEvaluate(section.NameExpression, context.XsltContext));
                        string sectionVersion = XPathServices.ResultToString(sectionInputElement.XPathEvaluate(section.VersionExpression, context.XsltContext));
                        string sectionAssetId = XPathServices.ResultToString(sectionInputElement.XPathEvaluate(section.AssetIdExpression, context.XsltContext));

                        // eval condition
                        if (sectionInputElement.EvaluateCondition(section.ConditionExpression, context.XsltContext))
                        {
                            Uri sectionUri = new Uri(newUri + "#" + sectionName, UriKind.Relative);
                            AssetIdentifier assetIdentifier = new AssetIdentifier(sectionAssetId, sectionVersion);
                            context.RegisterAssetUri(assetIdentifier, sectionUri);
                            TraceSources.TemplateSource.TraceVerbose("{0}, {1}, (Section: {2}) => {3}",
                                                                     sectionAssetId,
                                                                     sectionVersion,
                                                                     sectionName,
                                                                     sectionUri.ToString());

                            sections.Add(new AssetSection(assetIdentifier, sectionName, sectionUri));
                        }
                        else
                        {
                            TraceSources.TemplateSource.TraceVerbose("{0}, {1}, (Section: {2}) => Condition not met",
                                                                       sectionAssetId,
                                                                       sectionVersion,
                                                                       sectionName);
                        }

                        context.XsltContext.PopVariableScope(); // 3
                    }
                }

                List<KeyValuePair<string, object>> xsltParams = new List<KeyValuePair<string, object>>();

                foreach (XPathVariable param in this.XsltParams)
                {
                    IXsltContextVariable contextVariable = param.Evaluate(inputNode, context.XsltContext);
                    object val = contextVariable.Evaluate(context.XsltContext);
                    xsltParams.Add(new KeyValuePair<string, object>(param.Name, val));
                }

                context.XsltContext.PopVariableScope(); // 1

                yield return new StylesheetApplication(outputPath,
                                                       inputNode,
                                                       xsltParams,
                                                       assetIdentifiers.ToArray(),
                                                       this.Name,
                                                       sections,
                                                       input,
                                                       this.LoadStylesheet(context.Cache),
                                                       null);
            }
        }
    }
}
