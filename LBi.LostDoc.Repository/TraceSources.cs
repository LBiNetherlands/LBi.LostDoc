﻿/*
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
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LBi.LostDoc.Repository
{
    public static class TraceSources
    {
        private static readonly TraceSource _ContentBuilderSource = new TraceSource("LBi.LostDoc.Repository.ContentBuilder",
                                                                         SourceLevels.All);

        private static readonly TraceSource _ContentManagerSource = new TraceSource("LBi.LostDoc.Repository.ContentManager",
                                                                         SourceLevels.All);

        private static readonly TraceSource _ContentSearcherSource = new TraceSource("LBi.LostDoc.Repository.ContentSearcher",
                                                                          SourceLevels.All);

        public static TraceSource ContentBuilderSource
        {
            get { return _ContentBuilderSource; }
        }

        public static TraceSource ContentManagerSource
        {
            get { return _ContentManagerSource; }
        }

        public static TraceSource ContentSearcherSource
        {
            get { return _ContentSearcherSource; }
        }
    }
}
