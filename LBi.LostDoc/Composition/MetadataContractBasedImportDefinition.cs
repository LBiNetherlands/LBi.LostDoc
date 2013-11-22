/*
 * Copyright 2013 DigitasLBi Netherlands B.V.
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
using System.ComponentModel.Composition.Primitives;
using System.Linq;

namespace LBi.LostDoc.Composition
{
    public class MetadataContractBasedImportDefinition : ContractBasedImportDefinition
    {
        public MetadataContractBasedImportDefinition(Type contractType,
                                                     string requiredTypeIdentity,
                                                     IEnumerable<Tuple<string, object, IEqualityComparer>> requiredMetadata,
                                                     ImportCardinality cardinality,
                                                     bool isRecomposable,
                                                     bool isPrerequisite,
                                                     CreationPolicy requiredCreationPolicy)
            : base(AttributedModelServices.GetContractName(contractType),
                   requiredTypeIdentity,
                   requiredMetadata.Select(t => new KeyValuePair<string, Type>(t.Item1, t.Item2.GetType())),
                   cardinality,
                   isRecomposable,
                   isPrerequisite,
                   requiredCreationPolicy)
        {
            this.RequiredMetadataValues = requiredMetadata.ToArray();
        }

        public IEnumerable<Tuple<string, object, IEqualityComparer>> RequiredMetadataValues { get; protected set; }

        public override bool IsConstraintSatisfiedBy(ExportDefinition exportDefinition)
        {
            bool ret = base.IsConstraintSatisfiedBy(exportDefinition);

            if (ret)
            {
                foreach (var kvp in this.RequiredMetadataValues)
                {
                    object value;
                    if (!exportDefinition.Metadata.TryGetValue(kvp.Item1, out value) || !kvp.Item3.Equals(value, kvp.Item2))
                    {
                        ret = false;
                        break;
                    }
                }
            }

            return ret;
        }
    }
}