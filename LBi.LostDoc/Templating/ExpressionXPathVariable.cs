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

using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace LBi.LostDoc.Templating
{
    public class ExpressionXPathVariable : XPathVariable
    {
        public ExpressionXPathVariable(string name, string expression) : base(name)
        {
            this.ValueExpression = expression;
        }

        public string ValueExpression { get; protected set; }

        public override IXsltContextVariable Evaluate(XNode scope, IXmlNamespaceResolver resolver)
        {
            return new ConstantXPathVariable(this.Name, scope.XPathEvaluate(this.ValueExpression, resolver));
        }
    }
}