/*
 * Copyright 2013 LBi Netherlands B.V.
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

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Linq;
using System.Reflection;
using LBi.LostDoc.Repository.Web.Extensibility;

namespace LBi.LostDoc.Repository.Web.Configuration.Composition
{
    public class SettingsExportProvider : ExportProvider
    {
        private readonly ISettingsProvider _settingsProvider;

        public SettingsExportProvider(ISettingsProvider settingsProvider)
        {
            this._settingsProvider = settingsProvider;
        }

        protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition atomicComposition)
        {
            var contractName = definition.ContractName;

            if (contractName != SettingsConstants.SettingsContract)
                yield break;

            if (definition.Cardinality == ImportCardinality.ZeroOrMore)
                yield break;

            // TODO can't figure out how to get data injected into the Metadata collection
            //string settingsKey = definition.Metadata[SettingsConstants.SettingsMetadataKey] as string;

            LazyMemberInfo lazyMember = ReflectionModelServices.GetImportingMember(definition);
            MemberInfo member = lazyMember.GetAccessors().First();

            MethodInfo getterOrSetter = (MethodInfo)member;
            
            // HACK this is pretty evil
            PropertyInfo propInfo = getterOrSetter.DeclaringType.GetProperty(getterOrSetter.Name.Substring(4));

            ImportSettingAttribute settingsAttr = propInfo.GetCustomAttribute<ImportSettingAttribute>();

            if (settingsAttr == null)
                yield break;

            object value;
            if (!this._settingsProvider.TryGetValue(settingsAttr.SettingsKey, out value))
                yield break;

            yield return new Export(SettingsConstants.SettingsContract, () => value);
        }
    }
}
