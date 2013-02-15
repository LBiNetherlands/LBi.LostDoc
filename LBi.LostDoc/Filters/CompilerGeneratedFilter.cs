/*
 * Copyright 2012 LBi Netherlands B.V.
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
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LBi.LostDoc.Filters
{
    public class CompilerGeneratedFilter : MemberInfoFilter
    {
        public override bool Filter(IFilterContext context, MemberInfo m)
        {
            IList<CustomAttributeData> data = m.GetCustomAttributesData();
            foreach (CustomAttributeData attrData in data)
            {
                if (attrData.Constructor.DeclaringType.Equals(typeof(CompilerGeneratedAttribute)))
                    return true;
            }

            return false;
        }
    }
}
