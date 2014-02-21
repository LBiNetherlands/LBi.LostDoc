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
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{
    public class StylesheetDirective : ITemplateDirective
    {
        public StylesheetDirective(int ordinal)
        {
            Contract.Requires<ArgumentOutOfRangeException>(ordinal >= 0);

            this.Ordinal = ordinal;
        }

        public int Ordinal { get; private set; }

        public string ConditionExpression { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// This gets evaluated against the <see cref="XDocument"/> resolved from <see cref="InputExpression"/>
        /// </summary>
        public string SelectExpression { get; set; }

        /// <summary>
        /// This gets evaluated against the <see cref="ITemplateContext.Document"/>.
        /// </summary>
        public string InputExpression { get; set; }

        /// <summary>
        /// This gets evaluated against the <see cref="XNode"/> resolved from <see cref="SelectExpression"/>
        /// </summary>
        public string OutputExpression { get; set; }

        /// <summary>
        /// These gets evaluated against the <see cref="XNode"/> selected by the <see cref="SelectExpression"/>
        /// </summary>
        public XPathVariable[] XsltParams { get; set; }

        /// <summary>
        /// These gets evaluated against the <see cref="XNode"/> selected by the <see cref="SelectExpression"/>
        /// </summary>
        public XPathVariable[] Variables { get; set; }

        public SectionRegistration[] Sections { get; set; }

        public AssetRegistration[] AssetRegistrations { get; set; }

        /// <summary>
        /// This gets evaluated against the <see cref="XNode"/> selected by the <see cref="SelectExpression"/>
        /// </summary>
        public string StylesheetExpression { get; set; }

        public virtual IEnumerable<UnitOfWork> DiscoverWork(ITemplateContext context)
        {
            TraceSources.TemplateSource.TraceInformation("Processing stylesheet instructions: {0}", (object)this.Name);

            Uri inputUri;
            if (this.InputExpression != null)
            {
                string inputValue = context.Document.EvaluateValue(this.InputExpression, context.XsltContext);
                inputUri = new Uri(inputValue, UriKind.RelativeOrAbsolute);
                if (!inputUri.IsAbsoluteUri)
                    inputUri = inputUri.AddScheme(Storage.UriSchemeTemplate);
            }
            else
                inputUri = Storage.InputDocumentUri;


            XDocument inputDocument = context.Cache.GetDocument(inputUri, this.Ordinal);

            if (inputDocument == null)
            {
                Stream inputStream = context.DependencyProvider.GetDependency(inputUri, this.Ordinal);
                inputDocument = XDocument.Load(inputStream, LoadOptions.SetLineInfo);
                context.Cache.AddDocument(inputUri, this.Ordinal, inputDocument);
            }

            IEnumerable<XNode> inputNodes = XPathServices.ToNodeSequence(inputDocument.XPathEvaluate(this.SelectExpression, context.XsltContext));

            foreach (XNode inputNode in inputNodes)
            {
                context.XsltContext.PushVariableScope(inputNode, this.Variables); // 1

                Uri stylesheetUri = new Uri(inputDocument.Root.EvaluateValue(this.StylesheetExpression, context.XsltContext), UriKind.RelativeOrAbsolute);

                // set default storage scheme if none specified
                if (!stylesheetUri.IsAbsoluteUri)
                    stylesheetUri = stylesheetUri.AddScheme(Storage.UriSchemeTemplate);

                Uri outputUri;
                if (this.OutputExpression != null)
                {
                    string outputValue = XPathServices.ResultToString(inputNode.XPathEvaluate(this.OutputExpression, context.XsltContext));
                    outputUri = new Uri(outputValue, UriKind.RelativeOrAbsolute);
                    if (!outputUri.IsAbsoluteUri)
                        outputUri = outputUri.AddScheme(Storage.UriSchemeOutput);
                }
                else
                    outputUri = Storage.InputDocumentUri;

                List<AssetIdentifier> assetIdentifiers = new List<AssetIdentifier>();
                List<AssetSection> sections = new List<AssetSection>();

                // eval condition, shortcut and log instead of wrapping entire loop in if
                if (!inputNode.EvaluateCondition(this.ConditionExpression, context.XsltContext))
                {
                    TraceSources.TemplateSource.TraceVerbose("Condition not met: {0}", outputUri);
                    context.XsltContext.PopVariableScope(); // 1
                    continue;
                }

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
                            context.RegisterAssetUri(assetIdentifier, outputUri);
                            assetIdentifiers.Add(assetIdentifier);
                            TraceSources.TemplateSource.TraceVerbose("{0}, {1} => {2}",
                                                                     assetId,
                                                                     version,
                                                                     outputUri.ToString());
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
                            Uri sectionUri = new Uri(outputUri.OriginalString + "#" + sectionName, UriKind.Absolute);
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

                yield return new StylesheetApplication(this.Ordinal,
                                                       this.Name,
                                                       stylesheetUri,
                                                       inputUri,
                                                       outputUri,
                                                       inputNode,
                                                       xsltParams,
                                                       assetIdentifiers.ToArray(),
                                                       sections);
            }
        }
    }
}
