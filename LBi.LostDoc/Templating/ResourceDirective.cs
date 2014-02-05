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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using LBi.LostDoc.Composition;
using LBi.LostDoc.Extensibility;

namespace LBi.LostDoc.Templating
{
    public class ResourceDirective : ITemplateDirective<ResourceDeployment>
    {
        public ResourceDirective(string conditional,
                                 XPathVariable[] variables,
                                 IFileProvider fileProvider,
                                 string source,
                                 string output,
                                 ResourceTransform[] transformers)
        {
            this.ConditionExpression = conditional;
            this.FileProvider = fileProvider;
            this.Source = source;
            this.Output = output;
            this.Variables = variables;
            this.Transforms = transformers;
        }

        public IFileProvider FileProvider { get; private set; }
        public string ConditionExpression { get; private set; }
        public string Source { get; private set; }
        public string Output { get; private set; }
        public XPathVariable[] Variables { get; private set; }
        public ResourceTransform[] Transforms { get; private set; }

        public IEnumerable<ResourceDeployment> DiscoverWork(ITemplateContext context)
        {
            context.XsltContext.PushVariableScope(context.Document.Root, this.Variables);

            if (context.Document.Root.EvaluateCondition(this.ConditionExpression, context.XsltContext))
            {
                Uri expandedSource = new Uri(context.Document.Root.EvaluateValue(this.Source, context.XsltContext), UriKind.RelativeOrAbsolute);
                Uri expandedOutput = new Uri(context.Document.Root.EvaluateValue(this.Output, context.XsltContext), UriKind.RelativeOrAbsolute);

                List<IResourceTransform> transforms = new List<IResourceTransform>();

                foreach (var resourceTransform in this.Transforms)
                {
                    using (CompositionContainer localContainer = new CompositionContainer(context.Catalog))
                    {
                        string dirName = Path.GetDirectoryName(expandedSource.ToString());
                        CompositionBatch batch = new CompositionBatch();
                        var exportMetadata = new Dictionary<string, object>();

                        exportMetadata.Add(CompositionConstants.ExportTypeIdentityMetadataName,
                                           AttributedModelServices.GetTypeIdentity(typeof(IFileProvider)));

                        exportMetadata.Add(CompositionConstants.PartCreationPolicyMetadataName,
                                           CreationPolicy.Shared);

                        batch.AddExport(new Export(ContractNames.ResourceFileProvider,
                                                   exportMetadata,
                                                   () => new ScopedFileProvider(this.FileProvider, dirName)));

                        // TODO export resourceTransform.Parameters into localContainer using CompositionBatch

                        localContainer.Compose(batch);

                        var requiredMetadata = new[]
                                                   {
                                                       new Tuple<string, object, IEqualityComparer>("Name",
                                                                                                    resourceTransform.Name,
                                                                                                    StringComparer.OrdinalIgnoreCase)
                                                   };

                        ImportDefinition importDefinition =
                            new MetadataContractBasedImportDefinition(typeof(IResourceTransform),
                                                                      null,
                                                                      requiredMetadata,
                                                                      ImportCardinality.ExactlyOne,
                                                                      false,
                                                                      true,
                                                                      CreationPolicy.NonShared);

                        Export transformExport = localContainer.GetExports(importDefinition).Single();

                        transforms.Add((IResourceTransform)transformExport.Value);
                    }
                }

                yield return new ResourceDeployment(this.FileProvider,
                                                    expandedSource,
                                                    expandedOutput, // TODO this needs a 'writable' file provider
                                                    transforms.ToArray());
            }
            context.XsltContext.PopVariableScope();
        }
    }
}
