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

using System.IO;
using System.Linq;
using System.Web.Http;
using System.Xml;
using System.Xml.Linq;
using LBi.LostDoc.Repository.Web.Areas.Api.Models;
using LBi.LostDoc.Repository.Web.Configuration;
using LBi.LostDoc.Repository.Web.Models;

namespace LBi.LostDoc.Repository.Web.Areas.Api.Controllers
{
    public class LibraryController : ApiController
    {
        public LibraryModel Get()
        {
            string root = Path.GetFullPath(AppConfig.ContentPath);
            // TODO fix the raw xml access
            return new LibraryModel
                       {
                           Libraries = Directory.EnumerateDirectories(AppConfig.ContentPath)
                               .Select(
                                       d =>
                                       new LibraryDescriptor
                                           {
                                               Id = d.Substring(root.Length + 1),
                                               Created = XmlConvert.ToDateTime(XDocument.Load(Path.Combine(d, "info.xml")).Element("content").Attribute("created").Value)
                                           }),
                           Current = App.Instance.Content.ContentFolder
                       };
        }

        public bool Delete(string id)
        {
            var dir = Directory.EnumerateDirectories(AppConfig.ContentPath, id).SingleOrDefault();

            if (dir == null)
                return false;

            Directory.Delete(dir, true);

            return true;
        }

        public bool Post(string id)
        {
            var dir = Directory.EnumerateDirectories(AppConfig.ContentPath, id).SingleOrDefault();

            if (dir == null)
                return false;

            App.Instance.Content.SetCurrentContentFolder(id);

            return true;
        }
    }
}
