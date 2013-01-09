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
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using LBi.LostDoc.Core;

namespace LBi.LostDoc.Repository.Web.Models
{
    [DataContract]
    public class FileDescriptor
    {
        [DataMember]
        public string Assembly { get; set; }

        [DataMember]
        public AssemblyVersion Version { get; set; }

        public static FileDescriptor Load(FileInfo fileInfo)
        {
            if (fileInfo == null)
                throw new ArgumentNullException("fileInfo");

            if (!fileInfo.Exists)
                throw new FileNotFoundException("File not found: " + fileInfo.FullName, fileInfo.FullName);

            FileDescriptor ret = null;

            using (FileStream stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (XmlReader reader = XmlReader.Create(stream))
            {
                reader.MoveToContent();

                while (reader.IsStartElement("assembly") || reader.ReadToFollowing("assembly"))
                {
                    int phase = int.Parse(reader.GetAttribute("phase"), CultureInfo.InvariantCulture);
                    if (phase == 0)
                    {
                        string rawAssetId = reader.GetAttribute("assetId");
                        AssetIdentifier aid = AssetIdentifier.Parse(rawAssetId);

                        ret = new FileDescriptor
                                  {
                                      Assembly = aid.AssetId.Substring(aid.TypeMarker.Length + 1),
                                      Version = aid.Version
                                  };

                        break;
                    }
                }

            }

            return ret;
        }
    }
}
