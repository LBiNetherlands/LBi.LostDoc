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
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using Microsoft.Web.Administration;

namespace LBi.LostDoc.Repository.Web.Controllers
{
    public class ContentController : Controller
    {

        public ActionResult GetContent(string id, string path)
        {
            try
            {
                if (path[0] == '/')
                    path = path.Substring(1);

                string contentPath;
                if (id == "current")
                    contentPath = Path.Combine(App.Instance.ContentManager.ContentRoot, "Html", path);
                else
                    contentPath = Path.Combine(App.Instance.ContentManager.GetContentRoot(id), "Html", path);

                string contentType = MimeMapping.GetMimeMapping(Path.GetExtension(contentPath));
                return this.File(contentPath, contentType);
            }
            catch (InvalidOperationException)
            {
                return new HttpNotFoundResult();
            }
        }

    }
}
