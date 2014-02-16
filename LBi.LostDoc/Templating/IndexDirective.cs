/*
 * Copyright 2013-2014 DigitasLBi Netherlands B.V.
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

namespace LBi.LostDoc.Templating
{
    public class IndexDirective
    {
        public IndexDirective(int ordinal, string name, string inputExpression, string matchExpression, string keyExpression)
        {
            this.Ordinal = ordinal;
            this.Name = name;
            this.InputExpression = inputExpression;
            this.MatchExpression = matchExpression;
            this.KeyExpression = keyExpression;
        }

        public int Ordinal { get; protected set; }

        public string InputExpression { get; protected set; }

        public string Name { get; protected set; }

        public string MatchExpression { get; protected set; }
        
        public string KeyExpression { get; protected set; }
    }
}