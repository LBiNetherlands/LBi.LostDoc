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

using System;
using System.Numerics;

namespace LBi.LostDoc.Repository
{
    public static class GuidExtensions
    {
        public static string ToBase36String(this Guid guid)
        {
            const string CLIST = "0123456789abcdefghijklmnopqrstuvwxyz";
            byte[] buffer = guid.ToByteArray();
            
            // ensure it gets intepreted as a positive number by appending a zero-byte at the end
            Array.Resize(ref buffer, buffer.Length + 1);
            buffer[buffer.Length - 1] = 0;
            BigInteger num = new BigInteger(buffer);

            string ret = string.Empty;
            while (num > 0)
            {
                int val = (int)(num % 36);
                num = num / 36;
                ret = CLIST[val] + ret;
            }

            return ret;
        }
    }
}
