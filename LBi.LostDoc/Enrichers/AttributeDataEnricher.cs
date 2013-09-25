/*
 * Copyright 2012-2013 LBi Netherlands B.V.
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LBi.LostDoc.Enrichers
{
    public class AttributeDataEnricher : IEnricher
    {
        protected static readonly Regex InvalidCharacters = new Regex("[^\x09\x0A\x0D\x20-\uD7FF\uE000-\uFFFD\u10000-u10FFFF]",
                                                                      RegexOptions.Compiled);

        #region IEnricher Members

        public void EnrichType(IProcessingContext context, Type type)
        {
            GenerateAttributeElements(context, type.GetCustomAttributesData());
        }

        public void EnrichConstructor(IProcessingContext context, ConstructorInfo ctor)
        {
            GenerateAttributeElements(context, ctor.GetCustomAttributesData());
        }

        public void EnrichAssembly(IProcessingContext context, Assembly asm)
        {
            GenerateAttributeElements(context, asm.GetCustomAttributesData());
        }

        public void RegisterNamespace(IProcessingContext context)
        {
        }

        public void EnrichMethod(IProcessingContext context, MethodInfo mInfo)
        {
            GenerateAttributeElements(context, mInfo.GetCustomAttributesData());
        }

        public void EnrichField(IProcessingContext context, FieldInfo fieldInfo)
        {
            GenerateAttributeElements(context, fieldInfo.GetCustomAttributesData());
        }

        public void EnrichProperty(IProcessingContext context, PropertyInfo propertyInfo)
        {
            GenerateAttributeElements(context, propertyInfo.GetCustomAttributesData());
        }

        public void EnrichReturnValue(IProcessingContext context, MethodInfo methodInfo)
        {
            GenerateAttributeElements(context, CustomAttributeData.GetCustomAttributes(methodInfo.ReturnParameter));
        }

        public void EnrichParameter(IProcessingContext context, ParameterInfo item)
        {
            GenerateAttributeElements(context, item.GetCustomAttributesData());
        }

        public void EnrichTypeParameter(IProcessingContext context, Type typeParameter)
        {
            GenerateAttributeElements(context, typeParameter.GetCustomAttributesData());
        }

        // namespaces don't have attributes
        public void EnrichNamespace(IProcessingContext context, string ns)
        {
        }

        public void EnrichEvent(IProcessingContext context, EventInfo eventInfo)
        {
            GenerateAttributeElements(context, eventInfo.GetCustomAttributesData());
        }

        #endregion

        protected virtual void GenerateAttributeElements(IProcessingContext context, IEnumerable<CustomAttributeData> attrData)
        {
            foreach (CustomAttributeData custAttr in attrData)
            {
                AssetIdentifier typeAssetId = AssetIdentifier.FromMemberInfo(custAttr.Constructor.ReflectedType
                                                                             ?? custAttr.Constructor.DeclaringType);

                if (context.IsFiltered(typeAssetId))
                    continue;

                context.AddReference(AssetIdentifier.FromMemberInfo(custAttr.Constructor));

                var attrElem = new XElement("attribute",
                                            new XAttribute("type", typeAssetId),
                                            new XAttribute("constructor", AssetIdentifier.FromMemberInfo(custAttr.Constructor)));

                foreach (CustomAttributeTypedArgument cta in custAttr.ConstructorArguments)
                {
                    XElement argElem = new XElement("argument");

                    this.GenerateValueLiteral(context.Clone(argElem), cta);

                    attrElem.Add(argElem);
                }

                foreach (CustomAttributeNamedArgument cta in custAttr.NamedArguments)
                {
                    AssetIdentifier namedMember = AssetIdentifier.FromMemberInfo(cta.MemberInfo);
                    context.AddReference(namedMember);

                    XElement argElem = new XElement("argument",
                                                    new XAttribute("member", namedMember));

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
                DocGenerator.GenerateTypeRef(context.Clone(typeElement), (Type)cta.Value);
                context.Element.Add(typeElement);
            }
            else
            {
                XElement constElement = new XElement("constant",
                                                     new XAttribute("type", AssetIdentifier.FromType(cta.ArgumentType)));

                if (cta.ArgumentType == typeof(string) && InvalidCharacters.IsMatch((string)cta.Value))
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

        protected virtual void GenerateArrayLiteral(IProcessingContext context, Type elementType, IEnumerable<CustomAttributeTypedArgument> arrayValues)
        {
            XElement arrayElement = new XElement("arrayOf",
                                                 new XAttribute("type", AssetIdentifier.FromType(elementType)),
                                                 new XAttribute("rank", 1)); // attributes only suport one-dimensional arrays

            foreach (CustomAttributeTypedArgument cta in arrayValues)
            {
                this.GenerateValueLiteral(context.Clone(arrayElement), cta);
            }

            context.Element.Add(arrayElement);
        }
    }
}
