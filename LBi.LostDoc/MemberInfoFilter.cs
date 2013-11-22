/*
 * Copyright 2012 DigitasLBi Netherlands B.V.
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

using System.Reflection;

namespace LBi.LostDoc
{
    /// <summary>
    /// Base class for <see cref="MemberInfo"/> based filters.
    /// </summary>
    public abstract class MemberInfoFilter : IAssetFilter
    {
        #region IAssetFilter Members

        public bool Filter(IFilterContext context, AssetIdentifier asset)
        {
            MemberInfo mi = context.AssetResolver.Resolve(asset) as MemberInfo;
            if (mi != null)
                return Filter(context, mi);

            return false;
        }

        #endregion

        public abstract bool Filter(IFilterContext context, MemberInfo m);
    }
}
