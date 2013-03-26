/*
 * Copyright 2013 LBi Netherlands B.V.
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
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Http;
using LBi.Cli.Arguments;
using LBi.LostDoc.ConsoleApplication.Extensibility;

namespace LBi.LostDoc.ConsoleApplication.Plugin.Repository
{
    [ParameterSet("Pull ldoc or xml file to repository", Command = "Pull", HelpMessage = "Downloads ldoc file from repository.")]
    public class PullCommand : ICommand
    {
        [Parameter(HelpMessage = "Include verbose output.")]
        public LBi.Cli.Arguments.Switch Verbose { get; set; }

        [Parameter(HelpMessage = "Path to ldoc file."), Required]
        public string Path { get; set; }

        [Parameter(HelpMessage = "Server address."), Required]
        public string Server { get; set; }

        [Parameter(HelpMessage = "Security key."), Required]
        public string ApiKey { get; set; }

        public void Invoke(CompositionContainer container)
        {
            UriBuilder requestUri = new UriBuilder(this.Server);
            requestUri.Query = "apiKey=" + this.ApiKey;
            using (FileStream inputStream = File.OpenRead(this.Path))
            using (HttpClient client = new HttpClient())
            {
                // TODO implement this
                //MultipartFormDataContent form = new MultipartFormDataContent();

                //HttpContent fileContent = new StreamContent(inputStream);
                //form.Add(fileContent, "file");

                //fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data");
                //fileContent.Headers.ContentDisposition.FileName = System.IO.Path.GetFileName(this.Path);

                //client.PostAsync(requestUri.Uri, form);
            }
        }
    }
}