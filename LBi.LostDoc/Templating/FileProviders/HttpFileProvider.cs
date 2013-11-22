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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace LBi.LostDoc.Templating.FileProviders
{
    public class HttpFileProvider : IFileProvider
    {
        public bool FileExists(string path)
        {
            Uri uri = new Uri(path, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri || !uri.Scheme.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                return false;

            using (HttpClient httpClient = new HttpClient())
            using (var task = httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri)))
            {
                if (!task.Wait(30000))
                    return false;

                return task.Result.StatusCode == HttpStatusCode.NoContent ||
                       task.Result.StatusCode == HttpStatusCode.OK;
            }
        }

        public Stream OpenFile(string path, FileMode mode)
        {
            if (mode != FileMode.Open)
                throw new ArgumentOutOfRangeException("mode", "Only FileMode.Open is supported.");

            Uri uri = new Uri(path, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri || !uri.Scheme.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Must be absolute http or https Uri.", "path");

            using (HttpClient httpClient = new HttpClient())
            using (var task = httpClient.GetStreamAsync(uri))
            {
                Stopwatch timer = new Stopwatch();
                if (!task.Wait(30000))
                {
                    throw new TimeoutException(string.Format("Waited for {0:N1}s, no response received from: {1}",
                                                             timer.Elapsed.TotalSeconds, uri));
                }
                return task.Result;
            }
        }

        public bool SupportsDiscovery
        {
            get { return false; }
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<string> GetFiles(string path)
        {
            throw new NotSupportedException();
        }
    }
}