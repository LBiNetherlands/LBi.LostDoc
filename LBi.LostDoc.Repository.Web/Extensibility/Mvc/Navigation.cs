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
using System.Linq;

namespace LBi.LostDoc.Repository.Web.Extensibility.Mvc
{
    public class Navigation
    {
        public Navigation(string group, 
                          double order, 
                          string text, 
                          Uri target, 
                          bool isActive, 
                          IEnumerable<Navigation> children)
        {
            this.Group = group;
            this.Order = order;
            this.Text = text;
            this.Target = target;
            this.IsActive = isActive;
            this.Children = children.ToArray();
        }

        public Navigation[] Children { get; protected set; }

        public string Group { get; protected set; }

        public bool IsActive { get; protected set; }

        public double Order { get; protected set; }

        public Uri Target { get; protected set; }

        public string Text { get; protected set; }
    }
}