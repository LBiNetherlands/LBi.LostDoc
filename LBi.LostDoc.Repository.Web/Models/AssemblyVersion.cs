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
using System.Runtime.Serialization;

namespace LBi.LostDoc.Repository.Web.Models
{
    /// <summary>
    /// </summary>
    [DataContract]
    public class AssemblyVersion
    {
        [DataMember]
        public int Major { get; set; }

        [DataMember]
        public int Minor { get; set; }

        [DataMember]
        public int Patch { get; set; }

        [DataMember]
        public int Build { get; set; }

        public static bool operator ==(AssemblyVersion av1, AssemblyVersion av2)
        {
            return av1.Major == av2.Major &&
                   av1.Minor == av2.Minor &&
                   av1.Patch == av2.Patch &&
                   av1.Build == av2.Build;
        }

        public static bool operator !=(AssemblyVersion av1, AssemblyVersion av2)
        {
            return !(av1 == av2);
        }

        public override bool Equals(object obj)
        {
            AssemblyVersion other = obj as AssemblyVersion;
            if (obj == null)
                return false;

            return this == other;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public static implicit operator AssemblyVersion(Version version)
        {
            return new AssemblyVersion
                       {
                           Major = version.Major,
                           Minor = version.Minor,
                           Patch = version.Build,
                           Build = version.Revision
                       };
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}.{3}", this.Major, this.Minor, this.Patch, this.Build);
        }
    }
}
