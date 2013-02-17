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

using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace LBi.LostDoc.Templating
{
    public class ConstantXPathVariable : XPathVariable, IXsltContextVariable
    {
        public ConstantXPathVariable(string name, object value) : base(name)
        {
            this.Value = value;
        }

        public object Value { get; protected set; }


        public override IXsltContextVariable Evaluate(XNode scope, IXmlNamespaceResolver resolver)
        {
            return this;
        }

        object IXsltContextVariable.Evaluate(XsltContext xsltContext)
        {
            object ret = this.Value;
            if (!(ret is string) && ret is System.Collections.IEnumerable)
            {
                object[] data = ((System.Collections.IEnumerable)ret).Cast<object>().ToArray();

                if (data.Length == 1)
                {
                    if (data[0] is XAttribute)
                        ret = ((XAttribute)data[0]).Value;
                    else if (data[0] is XCData)
                        ret = ((XCData)data[0]).Value;
                    else if (data[0] is XText)
                        ret = ((XText)data[0]).Value;
                    else if (data[0] is XNode)
                        ret = ((XNode)data[0]).CreateNavigator();
                }
                else
                    ret = data.Cast<XNode>().Select(n => n.CreateNavigator()).ToArray();
            }

            return ret;
        }

        bool IXsltContextVariable.IsLocal
        {
            get { return true; }
        }

        bool IXsltContextVariable.IsParam
        {
            get { return false; }
        }

        XPathResultType IXsltContextVariable.VariableType
        {
            get
            {
                switch (Type.GetTypeCode(this.Value.GetType()))
                {
                    case TypeCode.Empty:
                    case TypeCode.Object:
                    case TypeCode.DBNull:
                        return XPathResultType.Any;
                    case TypeCode.Boolean:
                        return XPathResultType.Boolean;
                    case TypeCode.Char:
                        return XPathResultType.String;
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return XPathResultType.Number;
                    case TypeCode.DateTime:
                        return XPathResultType.String;
                    case TypeCode.String:
                        return XPathResultType.String;
                    default:
                        return XPathResultType.Error;
                }
            }
        }


    }
}