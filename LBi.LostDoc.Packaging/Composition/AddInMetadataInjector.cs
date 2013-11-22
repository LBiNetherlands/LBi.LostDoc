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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Reflection.Context;

namespace LBi.LostDoc.Packaging.Composition
{
    public class AddInMetadataInjector : CustomReflectionContext
    {
        private readonly string _packageId;
        private readonly string _packageVersion;

        public AddInMetadataInjector(string packageId, string packageVersion)
        {
            this._packageId = packageId;
            this._packageVersion = packageVersion;
        }
        protected override IEnumerable<object> GetCustomAttributes(MemberInfo member, IEnumerable<object> declaredAttributes)
        {
            Attribute packageIdAttr = new ExportMetadataAttribute(AddInConstants.PackageIdMetadataName, this._packageId);
            Attribute packageVersionAttr = new ExportMetadataAttribute(AddInConstants.PackageVersionMetadataName, this._packageVersion);
            return declaredAttributes.Concat(new[] { packageIdAttr, packageVersionAttr });
        }

        protected override IEnumerable<object> GetCustomAttributes(ParameterInfo parameter, IEnumerable<object> declaredAttributes)
        {
            Attribute packageIdAttr = new ExportMetadataAttribute(AddInConstants.PackageIdMetadataName, this._packageId);
            Attribute packageVersionAttr = new ExportMetadataAttribute(AddInConstants.PackageVersionMetadataName, this._packageVersion);
            return declaredAttributes.Concat(new[] { packageIdAttr, packageVersionAttr });
        }
    }
}