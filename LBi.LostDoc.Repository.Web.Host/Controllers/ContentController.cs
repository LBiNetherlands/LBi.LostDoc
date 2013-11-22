/*
 * Copyright 2012-2013 DigitasLBi Netherlands B.V.
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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Web;
using System.Web.Mvc;
using LBi.LostDoc.Repository.Web.Host.Models;

namespace LBi.LostDoc.Repository.Web.Host.Controllers
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ContentController : Controller
    {
        private ContentManager _content;

        [ImportingConstructor]
        public ContentController(ContentManager contentManager)
        {
            this._content = contentManager;
        }
        public ActionResult GetContent(string id, string path)
        {
            try
            {
                if (this._content.ContentRoot == null)
                    return View("NoContent", new NoContentModel {IsBuilding = this._content.CurrentState != State.Idle});

                if (path[0] == '/')
                    path = path.Substring(1);

                string contentPath;
                if (id == "current")
                    contentPath = Path.Combine(this._content.ContentRoot, "Html", path);
                else
                    contentPath = Path.Combine(this._content.GetContentRoot(id), "Html", path);

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
