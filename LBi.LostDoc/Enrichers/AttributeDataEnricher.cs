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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace LBi.LostDoc.Enrichers
{
    public class AttributeDataEnricher : IEnricher
    {
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
            GenerateAttributeElements(context,
                                      CustomAttributeData.GetCustomAttributes(methodInfo.ReturnParameter));
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

        // TODO fix this, and the accompanying XSLT template, the construction of attribute 
        // arguments isn't very consitent 
        private static void GenerateAttributeElements(IProcessingContext context,
                                                      IEnumerable<CustomAttributeData> attrData)
        {
            foreach (CustomAttributeData custAttr in attrData)
            {
                AssetIdentifier typeAssetId =
                    AssetIdentifier.FromMemberInfo(custAttr.Constructor.ReflectedType
                                                   ?? custAttr.Constructor.DeclaringType);

                if (context.IsFiltered(typeAssetId))
                    continue;

                context.AddReference(AssetIdentifier.FromMemberInfo(custAttr.Constructor));

                var attrElem = new XElement("attribute",
                                            new XAttribute("type",
                                                           typeAssetId),
                                            new XAttribute("constructor",
                                                           AssetIdentifier.FromMemberInfo(custAttr.Constructor)));

                foreach (CustomAttributeTypedArgument cta in custAttr.ConstructorArguments)
                {
                    if (cta.Value is ReadOnlyCollection<CustomAttributeTypedArgument>)
                    {
                        AssetIdentifier elementAssetId =
                            AssetIdentifier.FromMemberInfo(cta.ArgumentType.GetElementType());
                        context.AddReference(elementAssetId);
                        attrElem.Add(new XElement("argument",
                                                  new XElement("array",
                                                               new XAttribute("type", elementAssetId),
                                                               ((IEnumerable<CustomAttributeTypedArgument>)cta.Value).
                                                                   Select(
                                                                          ata =>
                                                                          new XElement("element",
                                                                                       GenerateAttributeArgument(
                                                                                                                 context,
                                                                                                                 ata))))));
                    }
                    else
                    {
                        attrElem.Add(new XElement("argument",
                                                  GenerateAttributeArgument(context, cta)));
                    }
                }

                foreach (CustomAttributeNamedArgument cta in custAttr.NamedArguments)
                {
                    AssetIdentifier namedMember = AssetIdentifier.FromMemberInfo(cta.MemberInfo);
                    context.AddReference(namedMember);

                    if (cta.TypedValue.Value is ReadOnlyCollection<CustomAttributeTypedArgument>)
                    {
                        context.AddReference(namedMember);
                        AssetIdentifier elementAssetId =
                            AssetIdentifier.FromMemberInfo(cta.TypedValue.ArgumentType.GetElementType());
                        context.AddReference(elementAssetId);
                        attrElem.Add(new XElement("argument",
                                                  new XAttribute("member", namedMember),
                                                  new XElement("array",
                                                               new XAttribute("type", elementAssetId),
                                                               ((IEnumerable<CustomAttributeTypedArgument>)
                                                                cta.TypedValue.Value).Select(
                                                                                             ata =>
                                                                                             new XElement("element",
                                                                                                          GenerateAttributeArgument(context, ata))))));
                    }
                    else
                    {
                        attrElem.Add(new XElement("argument",
                                                  new XAttribute("member", namedMember),
                                                  GenerateAttributeArgument(context, cta.TypedValue)));
                    }
                }


                context.Element.Add(attrElem);
            }

            //using (var ms = new MemoryStream())
            //    context.Element.Save(ms);
        }

        private static IEnumerable<XObject> GenerateAttributeArgument(IProcessingContext context,
                                                                      CustomAttributeTypedArgument cata)
        {
            // TODO this needs to be cleaned up, and fixed
            context.AddReference(AssetIdentifier.FromMemberInfo(cata.ArgumentType));
            yield return new XAttribute("type", AssetIdentifier.FromMemberInfo(cata.ArgumentType));

            if (cata.ArgumentType.IsEnum)
            {
                if (
                    cata.ArgumentType.GetCustomAttributesData().Any(
                                                                    ca =>
                                                                    ca.Constructor.DeclaringType ==
                                                                    typeof(FlagsAttribute)))
                {
                    string flags = Enum.ToObject(cata.ArgumentType, cata.Value).ToString();
                    string[] parts = flags.Split(',');

                    yield return
                        new XElement("literal",
                                     new XAttribute("value", cata.Value),
                                     Array.ConvertAll(parts,
                                                      s => new XElement("flag", new XAttribute("value", s.Trim()))));
                }
                else
                {
                    string value = Enum.GetName(cata.ArgumentType, cata.Value);
                    if (value != null)
                        yield return new XElement("literal", new XAttribute("value", value));

                    yield return new XElement("literal", new XAttribute("value", cata.Value));
                }
            }
            else if (cata.ArgumentType == typeof(Type))
            {
                XElement tmp = new XElement("tmp");
                DocGenerator.GenerateTypeRef(context.Clone(tmp), (Type)cata.Value, "value");
                yield return tmp.Attribute("value");
                foreach (XElement xElement in tmp.Elements())
                    yield return xElement;

            }
            else // TODO fix how this encodes unprintable characters 
                yield return new XAttribute("value", cata.Value.ToString().Replace("\0", "\\0"));
        }
    }
}
