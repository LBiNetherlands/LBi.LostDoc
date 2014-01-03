/*
 * Copyright 2014 DigitasLBi Netherlands B.V.
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

namespace LBi.LostDoc.Templating
{
    public class DefaultUniqueUriFactory : IUniqueUriFactory
    {
        private readonly HashSet<Uri> _seenUris;

        public DefaultUniqueUriFactory()
        {
            this._seenUris = new HashSet<Uri>();
        }

        public void EnsureUnique(ref Uri uri)
        {
            Uri origUri = uri;
            int i = 0;
            while (!this._seenUris.Add(uri))
            {
                string uriStr = origUri.ToString();
                int ix = uriStr.LastIndexOf('.');
                if (ix >= 0)
                {
                    uriStr = string.Format("{0}-{1}{2}", uriStr.Substring(0, ix), ++i, uriStr.Substring(ix));
                    uri = new Uri(uriStr, UriKind.RelativeOrAbsolute);
                }
                else
                    throw new Exception("uri doesn't contains a dot: " + uriStr);
            }
        }

        public void Clear()
        {
            this._seenUris.Clear();
        }
    }
}