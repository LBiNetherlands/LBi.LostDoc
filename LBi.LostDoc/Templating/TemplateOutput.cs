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

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LBi.LostDoc.Templating
{
    public class TemplateOutput
    {
        public TemplateOutput(WorkUnitResult[] result, TempFileCollection tempFiles)
        {
            this.Results = result;
            this.TemporaryFiles = tempFiles;
        }

        public TempFileCollection TemporaryFiles { get; protected set; }
        public WorkUnitResult[] Results { get; protected set; }
    }
}
