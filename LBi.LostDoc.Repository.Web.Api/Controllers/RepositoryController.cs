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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using LBi.LostDoc.Repository.Web.Api.Models;
using LBi.LostDoc.Repository.Web.Configuration;
using LBi.LostDoc.Repository.Web.Models;
using LBi.LostDoc.Repository.Web.Security;

namespace LBi.LostDoc.Repository.Web.Api.Controllers
{
    [ApiKeyAuthorize]
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class RepositoryController : ApiController
    {
        [ImportingConstructor]
        public RepositoryController(ContentManager contentManager)
        {
            this.Content = contentManager;
        }

        protected ContentManager Content { get; set; }

        // GET /repository/
        public void Delete(string assembly, string version)
        {
            AssemblyVersion realVersion = Version.Parse(version);
            var file = Directory.EnumerateFiles(AppConfig.RepositoryPath, "*.ldoc")
                                .SingleOrDefault(ld =>
                                                     {
                                                         var fd = FileDescriptor.Load(new FileInfo(ld));
                                                         return
                                                             string.Equals(fd.Assembly, 
                                                                           assembly, 
                                                                           StringComparison.OrdinalIgnoreCase) &&
                                                             fd.Version == realVersion;
                                                     });

            if (file == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            File.Delete(file);

            this.Content.QueueRebuild(string.Format("Deleted assembly: {{A:{0}, V:{1}}}", 
                                                            assembly, 
                                                            realVersion.ToString()));
        }

        public IEnumerable<FileDescriptor> Get()
        {
            return Directory.EnumerateFiles(AppConfig.RepositoryPath, "*.ldoc")
                            .Select(ldocFile => FileDescriptor.Load(new FileInfo(ldocFile)));
        }

        // GET /repository/LBi.Test
        public IEnumerable<FileDescriptor> Get(string assembly)
        {
            return Directory.EnumerateFiles(AppConfig.RepositoryPath, "*.ldoc")
                            .Select(ldocFile => FileDescriptor.Load(new FileInfo(ldocFile)))
                            .Where(ld => string.Equals(ld.Assembly, assembly, StringComparison.OrdinalIgnoreCase));
        }

        // GET /repository/LBi.Test/1.0.5.4
        public FileDescriptor Get(string assembly, string version)
        {
            AssemblyVersion realVersion = Version.Parse(version);
            return Directory.EnumerateFiles(AppConfig.RepositoryPath, "*.ldoc")
                            .Select(ldocFile => FileDescriptor.Load(new FileInfo(ldocFile)))
                            .SingleOrDefault(
                                ld => string.Equals(ld.Assembly, assembly, StringComparison.OrdinalIgnoreCase) &&
                                      ld.Version == realVersion);
        }

        // POST /repository
        public async Task<HttpResponseMessage> Post()
        {
            if (!this.Request.Content.IsMimeMultipartContent("form-data"))
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);

            List<FileDescriptor> ret = new List<FileDescriptor>();

            using (var tmp = new TempDir(AppConfig.TempPath))
            {
                MultipartFormDataStreamProvider provider = new MultipartFormDataStreamProvider(tmp.Path);

                var bodypart = await this.Request.Content.ReadAsMultipartAsync(provider);

                foreach (var fileName in provider.FileData)
                {
                    var fi = new FileInfo(fileName.LocalFileName);
                    try
                    {
                        var fd = FileDescriptor.Load(fi);

                        string destFileName = Path.Combine(AppConfig.RepositoryPath, fileName.Headers.ContentDisposition.FileName.Trim('"'));
                        File.Copy(fi.FullName, destFileName);

                        ret.Add(fd);
                    }
                    catch (Exception ex)
                    {
                        // log
                        //Repository.TraceSources.Content.TraceError(
                        //    "An exception of type {2} occured while processing '{0}': {1}", 
                        //    fileName, 
                        //    ex.ToString(), 
                        //    ex.GetType().Name);
                        return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
                    }

                    fi.Delete();
                }
            }

            string reason = ret.Aggregate(
                new StringBuilder(), 
                (builder, descriptor) =>
                (builder.Length == 0
                     ? builder
                     : builder.Append(", "))
                    .AppendFormat(
                        "{{A:{0}, V:{1}}}", 
                        descriptor.Assembly, 
                        descriptor.Version), 
                builder => builder.ToString());

            if (ret.Count > 0)
                this.Content.QueueRebuild("Added assemblies: " + reason);

            return this.Request.CreateResponse(HttpStatusCode.Accepted, ret.AsEnumerable());
        }

        // DELETE /repository/LBi.Test/1.0.5.4

        [HttpGet]
        public string State()
        {
            return this.Content.CurrentState.ToString();
        }
    }
}