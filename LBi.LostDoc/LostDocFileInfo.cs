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

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace LBi.LostDoc
{
    public class LostDocFileInfo : IEnumerable<AssemblyInfo>
    {
        private readonly List<AssemblyInfo> _assemblies;

        public LostDocFileInfo(string path)
        {
            this._assemblies = new List<AssemblyInfo>();
            this.Path = path;
            XDocument xDoc = XDocument.Load(path);
            foreach (XElement asmElement in xDoc.Root.Elements("assembly"))
            {
               
                // name="Company.Project.AnotherLibrary" filename="Company.Project.AnotherLibrary.dll" assetId="{A:Company.Project.AnotherLibrary, V:2.1.3.5670}"
                this._assemblies.Add(new AssemblyInfo
                                         {
                                             Name = asmElement.Attribute("name").Value,
                                             Filename = asmElement.Attribute("filename").Value,
                                             AssetId = AssetIdentifier.Parse(asmElement.Attribute("assetId").Value),
                                             Phase = int.Parse(asmElement.Attribute("phase").Value, CultureInfo.InvariantCulture),
                                         });
            }

        }

        public string Path { get; protected set; }
        public IEnumerator<AssemblyInfo> GetEnumerator()
        {
            return this.Assemblies.GetEnumerator();
        }

        public AssemblyInfo PrimaryAssembly { get { return this._assemblies.Single(a => a.Phase == 0); } }

        public IEnumerable<AssemblyInfo> Assemblies
        {
            get { return this._assemblies; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}