/*
 * Copyright 2012-2014 DigitasLBi Netherlands B.V.
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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using LBi.LostDoc.Primitives;
using LBi.LostDoc.Reflection;

namespace LBi.LostDoc.Enrichers
{
    public class AttributeDataEnricher : IEnricher
    {
        protected static readonly Regex InvalidCharacters = new Regex("[^\x09\x0A\x0D\x20-\uD7FF\uE000-\uFFFD\u10000-u10FFFF]",
                                                                      RegexOptions.Compiled);

        #region IEnricher Members

        public void EnrichType(IProcessingContext context, IType typeAsset)
        {
            Type type = (Type)typeAsset.Target;
            GenerateAttributeElements(context, type.GetCustomAttributesData());
        }

        public void EnrichConstructor(IProcessingContext context, ConstructorAsset ctorAsset)
        {
            ConstructorInfo ctor = (ConstructorInfo)ctorAsset.Target;
            GenerateAttributeElements(context, ctor.GetCustomAttributesData());
        }

        public void EnrichAssembly(IProcessingContext context, AssemblyAsset assemblyAsset)
        {
            Assembly asm = (Assembly)assemblyAsset.Target;
            GenerateAttributeElements(context, asm.GetCustomAttributesData());
        }

        public void RegisterNamespace(IProcessingContext context)
        {
        }

        public void EnrichMethod(IProcessingContext context, MethodAsset methodAsset)
        {
            MethodInfo mInfo = (MethodInfo)methodAsset.Target;
            GenerateAttributeElements(context, mInfo.GetCustomAttributesData());
        }

        public void EnrichField(IProcessingContext context, FieldAsset fieldAsset)
        {
            FieldInfo fieldInfo = (FieldInfo)fieldAsset.Target;
            GenerateAttributeElements(context, fieldInfo.GetCustomAttributesData());
        }

        public void EnrichProperty(IProcessingContext context, PropertyAsset propertyAsset)
        {
            PropertyInfo propertyInfo = (PropertyInfo)propertyAsset.Target;
            GenerateAttributeElements(context, propertyInfo.GetCustomAttributesData());
        }

        public void EnrichReturnValue(IProcessingContext context, MethodAsset methodAsset)
        {
            MethodInfo methodInfo = (MethodInfo)methodAsset.Target;
            GenerateAttributeElements(context, CustomAttributeData.GetCustomAttributes(methodInfo.ReturnParameter));
        }

        public void EnrichParameter(IProcessingContext context, Parameter parameter)
        {
            //MethodBase methodInfo = null;//(MethodBase)methodAsset.Target;
            //ParameterInfo parameterInfo = methodInfo.GetParameters().Single(p => p.Name == parameter.ToString());
            //GenerateAttributeElements(context, parameterInfo.GetCustomAttributesData());
        }

        public void EnrichTypeParameter(IProcessingContext context, TypeParameter typeParameter)
        {
            //Type typeParameter;

            //MethodInfo methodInfo = typeOrMethodAsset.Target as MethodInfo;
            //if (methodInfo != null)
            //{
            //    typeParameter = methodInfo.GetGenericArguments().Single(t => t.Name == name);
            //}
            //else
            //{
            //    Type type = (Type)typeOrMethodAsset.Target;
            //    typeParameter = type.GetGenericArguments().Single(t => t.Name == name);
            //}
            //GenerateAttributeElements(context, typeParameter.GetCustomAttributesData());
        }

        // namespaces don't have attributes
        public void EnrichNamespace(IProcessingContext context, NamespaceAsset namespaceAsset)
        {
        }

        public void EnrichEvent(IProcessingContext context, EventAsset eventAsset)
        {
            EventInfo eventInfo = (EventInfo)eventAsset.Target;
            GenerateAttributeElements(context, eventInfo.GetCustomAttributesData());
        }

        #endregion

        protected virtual void GenerateAttributeElements(IProcessingContext context, IEnumerable<CustomAttributeData> attrData)
        {
            foreach (CustomAttributeData custAttr in attrData)
            {
                Type originatingType = custAttr.Constructor.ReflectedType
                                       ?? custAttr.Constructor.DeclaringType;

                Asset typeAsset = ReflectionServices.GetAsset(originatingType);

                if (context.IsFiltered(typeAsset))
                    continue;

                Asset ctorAsset = ReflectionServices.GetAsset(custAttr.Constructor);
                context.AddReference(ctorAsset);

                var attrElem = new XElement("attribute",
                                            new XAttribute("type", typeAsset.Id),
                                            new XAttribute("constructor", ctorAsset.Id));

                foreach (CustomAttributeTypedArgument cta in custAttr.ConstructorArguments)
                {
                    XElement argElem = new XElement("argument");

                    this.GenerateValueLiteral(context.Clone(argElem), cta);

                    attrElem.Add(argElem);
                }

                foreach (CustomAttributeNamedArgument cta in custAttr.NamedArguments)
                {
                    Asset asset = ReflectionServices.GetAsset(cta.MemberInfo);
                    context.AddReference(asset);

                    XElement argElem = new XElement("argument",
                                                    new XAttribute("member", asset.Id));

                    this.GenerateValueLiteral(context.Clone(argElem), cta.TypedValue);

                    attrElem.Add(argElem);
                }


                context.Element.Add(attrElem);
            }
        }

        protected virtual void GenerateValueLiteral(IProcessingContext context, CustomAttributeTypedArgument cta)
        {
            var arrayValues = cta.Value as IEnumerable<CustomAttributeTypedArgument>;
            if (arrayValues != null)
            {
                Debug.Assert(cta.ArgumentType.IsArray);
                this.GenerateArrayLiteral(context, cta.ArgumentType.GetElementType(), arrayValues);
            }
            else if (cta.ArgumentType == typeof(Type))
            {
                XElement typeElement = new XElement("typeRef");
                if (cta.Value == null)
                    this.GenerateNullLiteral(context.Clone(typeElement));
                else
                    DocGenerator.GenerateTypeRef(context.Clone(typeElement), (Type)cta.Value);
                context.Element.Add(typeElement);
            }
            else
            {
                XElement constElement = new XElement("constant");

                DocGenerator.GenerateTypeRef(context.Clone(constElement), cta.ArgumentType);

                if (cta.Value == null)
                    this.GenerateNullLiteral(context.Clone(constElement));
                else if (cta.ArgumentType == typeof(string) && InvalidCharacters.IsMatch((string)cta.Value))
                {
                    string rawValue = (string)cta.Value;
                    var matches = InvalidCharacters.Matches(rawValue);
                    int startPos = 0;
                    foreach (Match match in matches)
                    {
                        int invalidPos = match.Groups[0].Index;
                        Debug.Assert(match.Groups[0].Length == 1);

                        if (startPos < invalidPos)
                            constElement.Add(rawValue.Substring(startPos, invalidPos - startPos));

                        constElement.Add(new XElement("char", new XAttribute("value", (short)match.Groups[0].Value[0])));

                        startPos = invalidPos + match.Groups[0].Length;
                    }

                    // add trailing bit
                    constElement.Add(rawValue.Substring(startPos));
                }
                else
                {
                    constElement.Add(new XAttribute("value", cta.Value));
                }

                context.Element.Add(constElement);
            }
        }

        private void GenerateNullLiteral(IProcessingContext context)
        {
            context.Element.Add(new XAttribute(XName.Get("nil", "xsi"), XmlConvert.ToString(true)));
        }

        protected virtual void GenerateArrayLiteral(IProcessingContext context, Type elementType, IEnumerable<CustomAttributeTypedArgument> arrayValues)
        {
            XElement arrayElement = new XElement("arrayOf",
                                                 new XAttribute("rank", 1)); // attributes only suport one-dimensional arrays

            DocGenerator.GenerateTypeRef(context.Clone(arrayElement), elementType);

            foreach (CustomAttributeTypedArgument cta in arrayValues)
            {
                XElement elementElement = new XElement("element");
                this.GenerateValueLiteral(context.Clone(elementElement), cta);
                arrayElement.Add(elementElement);
            }

            context.Element.Add(arrayElement);
        }
    }
}
