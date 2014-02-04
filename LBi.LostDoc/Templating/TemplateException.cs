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
using System.Xml;

namespace LBi.LostDoc.Templating
{
    // TODO we need to add the IFileProvider where the template.xml can be located here!
    public class TemplateException : Exception
    {
        public static TemplateException MissingAttribute(string filename, IXmlLineInfo lineInfo, string attributeName)
        {
            return new TemplateException(filename, lineInfo, string.Format("Missing required attribute '{0}'", attributeName));
        }


        public TemplateException(string filename, IXmlLineInfo lineInfo, string message)
            : this(filename, lineInfo, message, null)
        {
        }

        public TemplateException(string filename, IXmlLineInfo lineInfo, string message, Exception innerException)
            : base(WrapMessage(lineInfo, message, innerException), innerException)
        {
            this.LineInfo = lineInfo;
            this.Path = filename;
        }

        private static string WrapMessage(IXmlLineInfo lineInfo, string message, Exception innerException)
        {
            if (innerException == null)
                return string.Format("{0} (line: {1}, col: {2})", message, lineInfo.LineNumber, lineInfo.LinePosition);

            return string.Format("{0} (line: {1}, col: {2}): [{3}] {4}", message, lineInfo.LineNumber,
                                 lineInfo.LinePosition, innerException.GetType().Name, innerException.Message);
        }

        public IXmlLineInfo LineInfo { get; protected set; }
        public string Path { get; protected set; }
    }
}
