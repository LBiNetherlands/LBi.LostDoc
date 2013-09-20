/*
 * Copyright 2012-2013 LBi Netherlands B.V.
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

using System.Runtime.Serialization;
using LBi.LostDoc.Repository.Web.Models;

namespace LBi.LostDoc.Repository.Web.Api.Models
{
    [DataContract]
    public class ResultSet
    {
        [DataMember(Order = 1)]
        public string Query { get; set; }

        [DataMember(Order = 2)]
        public int Offset { get; set; }

        [DataMember(Order = 3)]
        public int HitCount { get; set; }

        [DataMember(Order = 4)]
        public Result[] Results { get; set; }
    }
}
