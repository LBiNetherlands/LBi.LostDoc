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
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Templating.IO;

namespace LBi.LostDoc.Templating
{
    public class StylesheetApplication : UnitOfWork
    {
        public StylesheetApplication(int ordinal,
                                     string stylesheetName,
                                     Uri stylesheet,
                                     Uri input,
                                     Uri output,
                                     XNode inputNode,
                                     IEnumerable<KeyValuePair<string, object>> xsltParams,
                                     IEnumerable<AssetIdentifier> assetIdentifiers,
                                     IEnumerable<AssetSection> sections)
            : base(output, ordinal)
        {
            Contract.Requires<ArgumentNullException>(stylesheet != null);
            Contract.Requires<ArgumentNullException>(input != null);
            Contract.Requires<ArgumentNullException>(input.IsAbsoluteUri, "Input Uri must be absolute.");
            Contract.Requires<ArgumentNullException>(inputNode != null);
            Contract.Requires<ArgumentNullException>(xsltParams != null);
            Contract.Requires<ArgumentNullException>(assetIdentifiers != null);
            Contract.Requires<ArgumentNullException>(sections != null);

            this.Stylesheet = stylesheet;
            this.InputNode = inputNode;
            this.XsltParams = xsltParams.ToArray();
            this.AssetIdentifiers = assetIdentifiers.ToArray();
            this.StylesheetName = stylesheetName;
            this.Sections = sections.ToArray();
            this.Input = input;
        }

        public KeyValuePair<string, object>[] XsltParams { get; protected set; }

        public XNode InputNode { get; protected set; }

        public AssetIdentifier[] AssetIdentifiers { get; protected set; }

        public string StylesheetName { get; protected set; }

        public AssetSection[] Sections { get; protected set; }

        public Uri Input { get; protected set; }

        public Uri Stylesheet { get; protected set; }

        public override void Execute(ITemplatingContext context, Stream outputStream)
        {
            // register xslt params
            XsltArgumentList argList = new XsltArgumentList();
            foreach (KeyValuePair<string, object> kvp in this.XsltParams)
                argList.AddParam(kvp.Key, string.Empty, kvp.Value);

            argList.XsltMessageEncountered += (s, e) => TraceSources.TemplateSource.TraceInformation("Message: {0}.", e.Message);

            // and custom extensions
            argList.AddExtensionObject(Namespaces.Template, new TemplateXsltExtensions(context, this.Output, this.Ordinal));

            using (XmlWriter writer = XmlWriter.Create(outputStream, new XmlWriterSettings { Encoding = Encoding.UTF8, CloseOutput = false }))
            {
                long tickStart = Stopwatch.GetTimestamp();

                var transform = this.LoadStylesheet(context);

                XPathDocument inputDocument = context.Cache.GetXPathDocument(this.Input, this.Ordinal);

                if (inputDocument == null)
                {
                    Stream inputStream = context.DependencyProvider.GetDependency(this.Input, this.Ordinal);
                    inputDocument = new XPathDocument(inputStream);
                    context.Cache.AddXPathDocument(this.Input, this.Ordinal, inputDocument);
                }

                transform.Transform(inputDocument.CreateNavigator(),
                                    argList,
                                    writer,
                                    new XmlFileProviderResolver(Storage.UriSchemeTemporary, context.StorageResolver, context.DependencyProvider, this.Ordinal));

                double duration = ((Stopwatch.GetTimestamp() - tickStart) / (double)Stopwatch.Frequency) * 1000;

                TraceSources.TemplateSource.TraceVerbose("{0} ({1:N0} ms)", this.Output, duration);

                writer.Close();
                outputStream.Close();
            }
        }

        protected virtual XslCompiledTransform LoadStylesheet(ITemplatingContext context)
        {
            // TODO this key is probably not good enough
            XslCompiledTransform ret = context.Cache.Get(this.Stylesheet.ToString()) as XslCompiledTransform;

            if (ret == null)
            {
                ret = new XslCompiledTransform(true);
                FileReference stylesheet = context.StorageResolver.Resolve(this.Stylesheet);
                using (Stream str = stylesheet.GetStream())
                {
                    XmlReader reader = XmlReader.Create(str, new XmlReaderSettings { CloseInput = true });
                    XsltSettings settings = new XsltSettings(true, true);
                    XmlResolver resolver = new XmlFileProviderResolver(Storage.UriSchemeTemplate, context.StorageResolver, context.DependencyProvider, this.Ordinal);
                    ret.Load(reader, settings, resolver);
                }

                context.Cache.Add(this.Stylesheet.ToString(), ret, new CacheItemPolicy { Priority = CacheItemPriority.Default });
            }

            return ret;
        }
    }
}