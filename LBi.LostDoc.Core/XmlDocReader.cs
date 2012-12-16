using System;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace LBi.LostDoc.Core
{
    public class XmlDocReader
    {
        private XDocument _doc;

        public void Load(XmlReader reader)
        {
            this._doc = XDocument.Load(reader, LoadOptions.None);
        }


        public XElement GetDocComments(MethodInfo methodInfo)
        {
            string sig = Naming.GetAssetId(methodInfo);
            Debug.WriteLine(sig);
            return this.GetMemberElement(sig);
        }


        public XElement GetDocComments(Type type)
        {
            return this.GetMemberElement(Naming.GetAssetId(type));
        }

        public XElement GetDocComments(ConstructorInfo ctor)
        {
            string sig = Naming.GetAssetId(ctor);
            Debug.WriteLine(sig);
            return this.GetMemberElement(sig);
        }

        internal XElement GetDocComments(ParameterInfo parameter)
        {
            string sig;
            if (parameter.Member is ConstructorInfo)
                sig = Naming.GetAssetId((ConstructorInfo)parameter.Member);
            else if (parameter.Member is PropertyInfo)
                sig = Naming.GetAssetId((PropertyInfo)parameter.Member);
            else
                sig = Naming.GetAssetId((MethodInfo)parameter.Member);

            Debug.WriteLine(sig);
            XElement elem = this.GetMemberElement(sig);
            if (elem != null)
                return elem.XPathSelectElement(string.Format("param[@name='{0}']", parameter.Name));
            return null;
        }

        internal XElement GetDocCommentsReturnParameter(ParameterInfo parameter)
        {
            string sig = Naming.GetAssetId((MethodInfo)parameter.Member);

            Debug.WriteLine(sig);
            XElement elem = this.GetMemberElement(sig);
            if (elem != null)
                return elem.XPathSelectElement("returns");
            return null;
        }

        internal XElement GetDocComments(FieldInfo fieldInfo)
        {
            string sig = Naming.GetAssetId(fieldInfo);
            Debug.WriteLine(sig);
            return this.GetMemberElement(sig);
        }

        internal XElement GetTypeParameterSummary(Type type, Type typeParameter)
        {
            string sig = Naming.GetAssetId(type);

            Debug.WriteLine(sig);
            XElement elem = this.GetMemberElement(sig);
            if (elem != null)
                return elem.XPathSelectElement(string.Format("typeparam[@name='{0}']", typeParameter.Name));
            return null;
        }

        internal XElement GetTypeParameterSummary(MethodInfo methodInfo, Type typeParameter)
        {
            string sig = Naming.GetAssetId(methodInfo);

            Debug.WriteLine(sig);
            XElement elem = this.GetMemberElement(sig);
            if (elem != null)
                return elem.XPathSelectElement(string.Format("typeparam[@name='{0}']", typeParameter.Name));
            return null;
        }

        private XElement GetMemberElement(string signature)
        {
            return this._doc.XPathSelectElement(string.Format("/doc/members/member[@name='{0}']", signature));
        }

        public XElement GetDocComments(PropertyInfo propertyInfo)
        {
            string sig = Naming.GetAssetId(propertyInfo);
            Debug.WriteLine(sig);
            return this.GetMemberElement(sig);
        }
    }
}