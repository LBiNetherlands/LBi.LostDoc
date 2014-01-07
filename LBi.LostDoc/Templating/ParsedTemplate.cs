/*
 * Copyright 2012-2013 DigitasLBi Netherlands B.V.
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

using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace LBi.LostDoc.Templating
{
    public class ParsedTemplate
    {
        /// <summary>
        /// Contains all StylesheetDirective definitions specified in the template.
        /// </summary>
        public StylesheetDirective[] StylesheetsDirectives { get; set; }

        /// <summary>
        /// Contains all resource definitions specified in the template.
        /// </summary>
        public ResourceDirective[] ResourceDirectives { get; set; }

        /// <summary>
        /// Contains the index definitions specified in the template.
        /// </summary>
        public IndexDirective[] IndexDirectives { get; set; }

        /// <summary>
        /// Contains the processed source document, required for template inheritence.
        /// </summary>
        public XDocument Source { get; set; }

        /// <summary>
        /// Set of parameters
        /// </summary>
        public XPathVariable[] Parameters { get; set; }

        /// <summary>
        /// Temporary files generated while templating, useful for debugging.
        /// </summary>
        public TempFileCollection TemporaryFiles { get; set; }

        public IEnumerable<UnitOfWork> DiscoverWork(ITemplateContext context)
        {
            var ret = Enumerable.Empty<UnitOfWork>();

            context.XsltContext.PushVariableScope(context.Document.Root, this.Parameters);

            foreach (ResourceDirective resource in this.ResourceDirectives)
                ret = ret.Concat(resource.DiscoverWork(context));

            foreach (StylesheetDirective stylesheet in this.StylesheetsDirectives)
                ret = ret.Concat(stylesheet.DiscoverWork(context));

            context.XsltContext.PopVariableScope();

            return ret;
        }
    }
}
