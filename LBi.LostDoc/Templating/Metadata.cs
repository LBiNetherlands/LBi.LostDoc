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

using System.Xml.Xsl;

namespace LBi.LostDoc.Templating
{
    public class Metadata
    {
        public Metadata(string name,
                        XPathVariable[] variables,
                        string selectExpression,
                        string conditionExpression,
                        XPathVariable[] xsltParams,
                        XslCompiledTransform transform)
        {
            this.XsltParams = xsltParams;
            this.Variables = variables;
            this.Transform = transform;
            this.ConditionExpression = conditionExpression;
            this.SelectExpression = selectExpression;
            this.Name = name;
        }

        public string Name { get; private set; }

        public XPathVariable[] Variables { get; private set; }
        
        public string SelectExpression { get; private set; }

        public string ConditionExpression { get; private set; }
        
        public XPathVariable[] XsltParams { get; private set; }
        
        public XslCompiledTransform Transform { get; private set; }
    }
}