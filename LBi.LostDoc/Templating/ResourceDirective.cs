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
using System.Runtime.Remoting.Contexts;
using LBi.LostDoc.Composition;
using LBi.LostDoc.Extensibility;
using LBi.LostDoc.Templating.IO;

namespace LBi.LostDoc.Templating
{
    public class ResourceDirective : ITemplateDirective<ResourceDeployment>
    {
        public ResourceDirective(int order,
                                 string conditional,
                                 XPathVariable[] variables,
                                 string inputExpression,
                                 string outputExpression,
                                 ResourceTransform[] transformers)
        {
            this.Order = order;
            this.ConditionExpression = conditional;
            this.InputExpression = inputExpression;
            this.OutputExpression = outputExpression;
            this.Variables = variables;
            this.Transforms = transformers;
        }

        public int Order { get; private set; }
        public string ConditionExpression { get; private set; }
        public string InputExpression { get; private set; }
        public string OutputExpression { get; private set; }
        public XPathVariable[] Variables { get; private set; }
        public ResourceTransform[] Transforms { get; private set; }

        public IEnumerable<ResourceDeployment> DiscoverWork(ITemplateContext context)
        {
            context.XsltContext.PushVariableScope(context.Document.Root, this.Variables);

            if (context.Document.Root.EvaluateCondition(this.ConditionExpression, context.XsltContext))
            {
                Uri expandedInput = new Uri(context.Document.Root.EvaluateValue(this.InputExpression, context.XsltContext), UriKind.RelativeOrAbsolute);
                Uri expandedOutput = new Uri(context.Document.Root.EvaluateValue(this.OutputExpression, context.XsltContext), UriKind.RelativeOrAbsolute);

                // set default storage scheme if none specified
                if (!expandedInput.IsAbsoluteUri)
                    expandedInput = expandedInput.AddScheme(Storage.UriSchemeTemplate);

                if (!expandedOutput.IsAbsoluteUri)
                    expandedOutput = expandedOutput.AddScheme(Storage.UriSchemeOutput);

                List<IResourceTransform> transforms = new List<IResourceTransform>();

                // TODO XXXXX THIS HAS TO BE DEFERRED TO EXEUCTION TIME
                foreach (var resourceTransform in this.Transforms)
                {
                    using (CompositionContainer localContainer = new CompositionContainer(context.Catalog))
                    {
                        var exportMetadata = new Dictionary<string, object>();

                        exportMetadata.Add(CompositionConstants.ExportTypeIdentityMetadataName,
                                           AttributedModelServices.GetTypeIdentity(typeof(IFileProvider)));

                        exportMetadata.Add(CompositionConstants.PartCreationPolicyMetadataName,
                                           CreationPolicy.Shared);

                        CompositionBatch batch = new CompositionBatch();
                        string dirName = Path.GetDirectoryName(expandedInput.ToString());
                        batch.AddExport(new Export(ContractNames.ResourceFileProvider,
                                                   exportMetadata,
                                                   () => new ScopedFileProvider(context.TemplateFileProvider, dirName)));

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

                yield return new ResourceDeployment(expandedInput,
                                                    expandedOutput,
                                                    this.Order,
                                                    transforms.ToArray());
            }
            context.XsltContext.PopVariableScope();
        }
    }
}
