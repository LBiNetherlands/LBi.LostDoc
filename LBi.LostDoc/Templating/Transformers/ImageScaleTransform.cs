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

using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;

namespace LBi.LostDoc.Templating.Transformers
{
    [ExportTransform("scale")]
    public class ImageScaleTransform : IResourceTransform
    {
        [Import("mode", AllowDefault = true)]
        [DefaultValue(ScalingMode.Bound)]
        public ScalingMode Mode { get; set; }

        [Import("height")]
        public int Height { get; set; }

        [Import("width")]
        public int Width { get; set; }

        public Stream Transform(Stream input)
        {
            // scale image here
            return null;
        }
    }
}