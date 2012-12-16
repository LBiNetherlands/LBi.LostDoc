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

using System;

namespace LBi.LostDoc.Core.Templating
{
    public class AssetSection
    {
        public AssetSection(AssetIdentifier aid, string name, Uri uri)
        {
            this.AssetIdentifier = aid;
            this.Name = name;
            this.Uri = uri;
        }

        public AssetIdentifier AssetIdentifier { get; protected set; }
        public string Name { get; protected set; }
        public Uri Uri { get; protected set; }
    }
}
