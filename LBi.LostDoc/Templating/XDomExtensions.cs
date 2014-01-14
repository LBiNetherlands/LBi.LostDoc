﻿/*
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

using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using LBi.LostDoc.Templating.XPath;

namespace LBi.LostDoc.Templating
{
    public static class XDomExtensions
    {
        public static string GetAttributeValueOrDefault(this XElement elem, string attName, string defaultValue = null)
        {
            var attr = elem.Attribute(attName);

            if (attr == null)
                return defaultValue;

            return attr.Value;
        }

        public static string GetAttributeValue(this XElement elem, string attName)
        {
            var attr = elem.Attribute(attName);

            if (attr == null)
                throw new MissingNodeException(elem, XmlNodeType.Attribute, attName);

            return attr.Value;
        }

        public static bool EvaluateCondition(this XNode contextNode, string condition, XsltContext customContext)
        {
            bool shouldApply;
            if (string.IsNullOrWhiteSpace(condition))
                shouldApply = true;
            else
            {
                object value = contextNode.XPathEvaluate(condition, customContext);
                shouldApply = XPathServices.ResultToBool(value);
            }
            return shouldApply;
        }

        public static string EvaluateValue(this XNode contextNode, string valueOrExpression, XsltContext customContext)
        {
            // TODO this is quick and dirty
            if (valueOrExpression.StartsWith("{") && valueOrExpression.EndsWith("}"))
            {
                string expression = valueOrExpression.Substring(1, valueOrExpression.Length - 2);
                object value = contextNode.XPathEvaluate(expression, customContext);
                return XPathServices.ResultToString(value);
            }

            return valueOrExpression;
        }
    }
}