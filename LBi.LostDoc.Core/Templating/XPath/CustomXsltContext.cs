using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace LBi.LostDoc.Core.Templating.XPath
{
    public class CustomXsltContext : System.Xml.Xsl.XsltContext
    {
        private Dictionary<string, IXsltContextFunction> functions = new Dictionary<string, IXsltContextFunction>();

        public CustomXsltContext()
        {
        }

        public CustomXsltContext(NameTable nameTable)
            : base(nameTable)
        {
        }

        public override bool Whitespace
        {
            get { return false; }
        }

        public override string LookupNamespace(string prefix)
        {
            return base.LookupNamespace(prefix);
        }

        public override int CompareDocument(string baseUri, string nextbaseUri)
        {
            return string.CompareOrdinal(baseUri, nextbaseUri);
        }

        public override bool PreserveWhitespace(XPathNavigator node)
        {
            return false;
        }

        public void RegisterFunction(string prefix, string name, IXsltContextFunction function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            if (name == null)
                throw new ArgumentNullException("name");

            this.functions[prefix + ":" + name] = function;
        }

        public event Func<string, object> OnResolveVariable;

        public override IXsltContextFunction ResolveFunction(string prefix, string name,
                                                             XPathResultType[] argTypes)
        {
            IXsltContextFunction function = null;

            if (this.functions.TryGetValue(prefix + ":" + name, out function))
            {
                return function;
            }

            return null;
        }

        public override IXsltContextVariable ResolveVariable(string prefix, string name)
        {
            if (this.OnResolveVariable != null)
            {
                object val = this.OnResolveVariable(name);
                return new XPathArg(val);
            }

            throw new InvalidOperationException("No OnResolveVariable event listener found.");
        }

        internal static string GetValue(object v)
        {
            if (v == null)
                return null;

            if (v is System.Xml.XPath.XPathNodeIterator)
            {
                foreach (XPathNavigator n in v as System.Xml.XPath.XPathNodeIterator)
                    return n.Value;
            }

            return Convert.ToString(v);
        }

        #region Nested type: XPathArg

        private class XPathArg : IXsltContextVariable
        {
            private object _value;

            public XPathArg(object val)
            {
                if (!(val is string) && val is IEnumerable)
                {
                    object[] data = ((IEnumerable)val).Cast<object>().ToArray();

                    if (data.Length == 1)
                    {
                        if (data[0] is XAttribute)
                            val = ((XAttribute)data[0]).Value;
                        else if (data[0] is XText)
                            val = ((XText)data[0]).Value;
                        else if (data[0] is XCData)
                            val = ((XCData)data[0]).Value;
                        else if (data[0] is XNode)
                            val = ((XNode)data[0]).CreateNavigator();
                    }
                    else
                        val = data.Cast<XNode>().Select(n => n.CreateNavigator()).ToArray();
                }

                this._value = val;
            }

            #region IXsltContextVariable Members

            /// <summary>
            /// Evaluates the variable at runtime and returns an object that represents the value of the variable.
            /// </summary>
            /// <returns>
            /// An <see cref="T:System.Object"/> representing the value of the variable. Possible return types include number, string, Boolean, document fragment, or node set. 
            /// </returns>
            /// <param name="xsltContext">
            /// An <see cref="T:System.Xml.Xsl.XsltContext"/> representing the execution context of the variable. 
            /// </param>
            public object Evaluate(XsltContext xsltContext)
            {
                return this._value;
            }

            /// <summary>
            ///   Gets a value indicating whether the variable is local.
            /// </summary>
            /// <returns> true if the variable is a local variable in the current context; otherwise, false. </returns>
            public bool IsLocal
            {
                get { return true; }
            }

            /// <summary>
            ///   Gets a value indicating whether the variable is an Extensible Stylesheet Language Transformations (XSLT) parameter. This can be a parameter to a style sheet or a template.
            /// </summary>
            /// <returns> true if the variable is an XSLT parameter; otherwise, false. </returns>
            public bool IsParam
            {
                get { return false; }
            }

            /// <summary>
            ///   Gets the <see cref="T:System.Xml.XPath.XPathResultType" /> representing the XML Path Language (XPath) type of the variable.
            /// </summary>
            /// <returns> The <see cref="T:System.Xml.XPath.XPathResultType" /> representing the XPath type of the variable. </returns>
            public XPathResultType VariableType
            {
                get
                {
                    switch (Type.GetTypeCode(this._value.GetType()))
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

            #endregion
        }

        #endregion
    }
}