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

using System.IO;
using System.Linq;
using System.Text;
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
                parts[i] = new string(parts[i].Where(char.IsUpper).ToArray());
            }

            tokenString.AppendLine(parts[parts.Length - 1]);

            for (int i = parts.Length - 2; i >= 0; i--)
            {
                tokenString.AppendLine(string.Join(".", parts, i, parts.Length - i));
            }

            return new WhitespaceTokenizer(new StringReader(tokenString.ToString()));
        }
    }
}