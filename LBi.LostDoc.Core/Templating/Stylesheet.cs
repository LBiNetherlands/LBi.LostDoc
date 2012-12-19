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

using System.Xml.Xsl;

namespace LBi.LostDoc.Core.Templating
{
    public class Stylesheet
    {
        public string ConditionExpression { get; set; }
        public string VersionExpression { get; set; }
        public XslCompiledTransform Transform { get; set; }
        public string Name { get; set; }
        public string SelectExpression { get; set; }
        public string AssetIdExpression { get; set; }
        public string OutputExpression { get; set; }
        public XPathVariable[] XsltParams { get; set; }
        public XPathVariable[] Variables { get; set; }
        public SectionRegistration[] Sections { get; set; }
        public AliasRegistration[] AssetAliases { get; set; }
        public string Source { get; set; }
    }
}
