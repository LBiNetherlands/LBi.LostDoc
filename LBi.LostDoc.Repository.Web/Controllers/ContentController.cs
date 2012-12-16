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
using System.IO;
using System.Web.Hosting;
using System.Web.Mvc;
using Microsoft.Web.Administration;

namespace LBi.LostDoc.Repository.Web.Controllers
{
    public class ContentController : Controller
    {
        private static readonly Dictionary<string, string> _mimeLookup;

        static ContentController()
        {
            _mimeLookup = new Dictionary<string, string>();
            using (ServerManager serverManager = new ServerManager())
            {
                var siteName = HostingEnvironment.ApplicationHost.GetSiteName();
                Configuration config = serverManager.GetWebConfiguration(siteName);
                ConfigurationSection staticContentSection = config.GetSection("system.webServer/staticContent");
                ConfigurationElementCollection staticContentCollection = staticContentSection.GetCollection();

                foreach (ConfigurationElement confElem in staticContentCollection)
                {
                    _mimeLookup.Add(confElem.GetAttributeValue("fileExtension").ToString(),
                                    confElem.GetAttributeValue("mimeType").ToString());
                }
            }
        }

        public ActionResult GetContent(string id, string path)
        {
            try
            {
                if (path[0] == '/')
                    path = path.Substring(1);

                string contentPath;
                if (id == "current")
                    contentPath = Path.Combine(ContentManager.Instance.ContentRoot, "Html", path);
                else
                    contentPath = Path.Combine(ContentManager.Instance.GetContentRoot(id), "Html", path);

                string contentType;
                if (!_mimeLookup.TryGetValue(Path.GetExtension(contentPath), out contentType))
                    contentType = "application/octet-stream";
                return this.File(contentPath, contentType);
            }
            catch (InvalidOperationException)
            {
                return new HttpNotFoundResult();
            }
        }

    }
}
