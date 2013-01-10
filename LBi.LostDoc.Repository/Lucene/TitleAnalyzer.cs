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
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;

namespace LBi.LostDoc.Repository.Lucene
{

    public class TitleAnalyzer : Analyzer
    {
        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            string str = reader.ReadToEnd();
            StringBuilder builder = new StringBuilder(str);
            builder.Replace('.', ' ');
            builder.Replace('<', ' ');
            builder.Replace('>', ' ');
            builder.Replace('[', ' ');
            builder.Replace(']', ' ');
            builder.Replace('(', ' ');
            builder.Replace(')', ' ');
            builder.Replace(',', ' ');

            builder.Replace("  ", " ");

            str = builder.ToString();
            
            for (int i = builder.Length - 1; i > 0; i--)
            {
                if (char.IsUpper(builder[i]))
                    builder.Insert(i, ' ');
            }

            builder.Append(' ').Append(str);

            return new LowerCaseFilter(new WhitespaceTokenizer(new StringReader(builder.ToString())));
        }
    }
}
