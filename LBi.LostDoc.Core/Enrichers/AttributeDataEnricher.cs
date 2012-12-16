using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace LBi.LostDoc.Core.Enrichers
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

        private static void GenerateAttributeElements(IProcessingContext context,
                                                      IEnumerable<CustomAttributeData> attrData)
        {
            foreach (CustomAttributeData custAttr in attrData)
            {
                context.AddReference(AssetIdentifier.FromMemberInfo(custAttr.Constructor));

                var attrElem = new XElement("attribute",
                                            new XAttribute("type",
                                                           AssetIdentifier.FromMemberInfo(
                                                                                          custAttr.Constructor.
                                                                                              ReflectedType ??
                                                                                          custAttr.Constructor.
                                                                                              DeclaringType)),
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
                                                                                                          GenerateAttributeArgument
                                                                                                              (context,
                                                                                                               ata))))));
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
        }

        private static IEnumerable<XObject> GenerateAttributeArgument(IProcessingContext context,
                                                                      CustomAttributeTypedArgument cata)
        {
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
                        new XElement("enum",
                                     Array.ConvertAll(parts,
                                                      s => new XElement("flag", new XAttribute("value", s.Trim()))));
                }
                else
                {
                    yield return
                        new XElement("enum", new XAttribute("value", Enum.GetName(cata.ArgumentType, cata.Value)));
                }
            }
            else if (cata.ArgumentType == typeof(Type))
            {
                XElement tmp = new XElement("tmp");
                DocGenerator.GenerateTypeRef(context.Clone(tmp), (Type)cata.Value, "value");
                yield return tmp.Attribute("value");
                foreach (XElement xElement in tmp.Elements())
                    yield return xElement;


// yield return new XAttribute("value", AssetIdentifier.FromMemberInfo((Type)cata.Value));
            }
            else
                yield return new XAttribute("value", cata.Value.ToString());
        }
    }
}