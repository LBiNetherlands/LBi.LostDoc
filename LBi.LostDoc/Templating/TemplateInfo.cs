/*
 * Copyright 2013 DigitasLBi Netherlands B.V.
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
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace LBi.LostDoc.Templating
{
    public class TemplateInfo
    {
        // TODO we can eliminate the TemplateResolver here once we remove it as a requirement for the Template class
        public TemplateInfo(IFileProvider source, string path, string name, TemplateParameterInfo[] parameters, TemplateInfo inheritedTemplate)
        {
            this.Source = source;
            this.Name = name;
            this.Parameters = parameters;
            this.Path = path;
            this.Inherits = inheritedTemplate;
        }

        public string Name { get; protected set; }
        public IFileProvider Source { get; protected set; }
        public string Path { get; protected set; }
        public TemplateParameterInfo[] Parameters { get; protected set; }
        public TemplateInfo Inherits { get; protected set; }

        // TODO see if we can't get rid of this CompositionContainer
        public Template Load(CompositionContainer container)
        {
            Template ret = new Template(container);
            ret.Load(this);
            return ret;
        }

        public IEnumerable<string> GetFiles()
        {
            return new[] {this.Name}
                .Concat(this.GetDirectories(this.Name))
                .Aggregate(Enumerable.Empty<string>(),
                           (aggregate, dir) =>
                           aggregate.Concat(this.Source.GetFiles(dir)
                                                .Select(file => System.IO.Path.Combine(dir, file))));
        }

        private IEnumerable<string> GetDirectories(string path)
        {
            IEnumerable<string> dirs = this.Source.GetDirectories(path);
            return dirs.Aggregate(Enumerable.Empty<string>(),
                                  (aggregate, dir) => aggregate.Concat(this.GetDirectories(System.IO.Path.Combine(path, dir))));
        }




        // TODO maybe move some (or all) of this to the TemplateResolver
        public static TemplateInfo Load(TemplateResolver resolver, IFileProvider source, string name)
        {
            string specPath = System.IO.Path.Combine(name, Template.TemplateDefinitionFileName);
            if (!source.FileExists(specPath))
                throw new FileNotFoundException("Couldn't find template specification: " + specPath + " from " + source.ToString(), specPath);

            Dictionary<string, TemplateParameterInfo> parameters = new Dictionary<string, TemplateParameterInfo>();

            TemplateInfo inheritedTemplate = null;

            using (var fileStream = source.OpenFile(specPath, FileMode.Open))
            {
                XDocument templateSpec = XDocument.Load(fileStream);
                XAttribute inheritsAttr = templateSpec.Element("template").Attribute("inherits");
                
                if (inheritsAttr != null)
                {
                    string inheritedTemplateName = inheritsAttr.Value;
                    if (!resolver.TryResolve(inheritedTemplateName, out inheritedTemplate))
                        throw new Exception("Failed to resolve inherted template: " + inheritedTemplateName);

                    // add inherited parameters
                    foreach (TemplateParameterInfo param in inheritedTemplate.Parameters)
                        parameters.Add(param.Name, param);
                }

                IEnumerable<XElement> parameterElements = templateSpec.XPathSelectElements("/template/parameter");
                foreach (XElement parameterElement in parameterElements)
                {
                    string paramName = parameterElement.Attribute("name").Value;
                    string defaultValue = ReadOptionalAttribute(parameterElement, "select");
                    string description = ReadOptionalAttribute(parameterElement, "description");

                    // add or override inherited parameter default value
                    parameters[paramName] = new TemplateParameterInfo(paramName, description, defaultValue);
                }
            }

            return new TemplateInfo(source, specPath, name, parameters.Values.ToArray(), inheritedTemplate);
        }

        private static string ReadOptionalAttribute(XElement element, string attribute)
        {
            var xattr = element.Attribute(attribute);
            if (xattr == null)
                return null;
            return xattr.Value;
        }
    }
}