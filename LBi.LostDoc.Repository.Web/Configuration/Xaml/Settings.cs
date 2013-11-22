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
using System.Linq;
using System.Windows.Markup;

namespace LBi.LostDoc.Repository.Web.Configuration.Xaml
{
    [ContentProperty("Entries")]
    public class Settings
    {
        public Settings()
        {
            this.Entries = new EntryCollection();
            this.Version = 1;
        }

        public int Version { get; set; }

        public EntryCollection Entries { get; private set; }

        public Dictionary<string, Entry> ToDictionary()
        {
            return this.Entries.ToDictionary(e => e.Key, StringComparer.Ordinal);
        }
    }
}