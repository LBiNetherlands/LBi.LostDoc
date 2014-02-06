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

namespace LBi.LostDoc.Templating.IO
{
    public class StorageResolver
    {
        private readonly Dictionary<string, Tuple<IFileProvider, bool>> _providers;

        public StorageResolver()
        {
            this._providers = new Dictionary<string, Tuple<IFileProvider, bool>>(StringComparer.Ordinal);
        }

        public void Add(string uriScheme, IFileProvider fileProvider, bool stripScheme)
        {
            this._providers.Add(uriScheme, Tuple.Create(fileProvider, stripScheme));
        }

        public FileReference Resolve(Uri uri)
        {
            var provider = this._providers[uri.Scheme];
            if (provider.Item2)
                return new FileReference(0, provider.Item1, uri.Authority + uri.PathAndQuery);
            else
                return new FileReference(0, provider.Item1, uri.ToString());
        }
    }
}