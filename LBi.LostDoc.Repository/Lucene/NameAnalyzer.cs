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
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LBi.LostDoc.Templating;
using Lucene.Net.Analysis;

namespace LBi.LostDoc.Repository.Lucene
{
    public class NameAnalyzer : Analyzer
    {
        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            string str = reader.ReadToEnd();

            if (str.IndexOf('(') > 0)
                str = str.Substring(0, str.IndexOf('('));

            if (str.IndexOf('<') > 0)
                str = str.Substring(0, str.IndexOf('<'));

            string[] parts = str.Split('.');

            StringBuilder tokenString = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                tokenString.AppendLine(string.Join(".", parts, i, parts.Length - i));
            }

            if (parts.Length > 0)
            {
                var lastPart = parts[parts.Length - 1];
                int[] indices = lastPart.Select((c, i) => new { c, i }).Where(t => char.IsUpper(t.c)).Select(t => t.i).ToArray();

                int lastIndex = 0;
                foreach (var index in indices)
                {
                    var subPart = lastPart.Substring(lastIndex, index - lastIndex);
                    if (!string.IsNullOrWhiteSpace(subPart))
                        tokenString.AppendLine(subPart);

                    lastIndex = index;
                }
                if (lastIndex > 0 && lastIndex < lastPart.Length)
                    tokenString.AppendLine(lastPart.Substring(lastIndex));

            }
                

            return new WhitespaceTokenizer(new StringReader(tokenString.ToString().ToLowerInvariant()));
        }
    }
}