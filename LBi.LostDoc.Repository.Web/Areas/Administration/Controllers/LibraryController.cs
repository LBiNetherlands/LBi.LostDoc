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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using LBi.LostDoc.Repository.Web.Areas.Administration.Models;
using LBi.LostDoc.Repository.Web.Extensibility;
using LBi.LostDoc.Repository.Web.Models;

namespace LBi.LostDoc.Repository.Web.Areas.Administration.Controllers
{
    [AdminController("library", Group = Groups.Core, Order = 2500, Text = "Library")]
    public class LibraryController : Controller
    {
        //
        // GET: /Administration/System/

        [AdminAction("index", IsDefault = true)]
        public ActionResult Index()
        {
            string root = Path.GetFullPath(AppConfig.ContentPath);
            return this.View(new LibraryModel
                                 {
                                     Libraries = Directory.EnumerateDirectories(AppConfig.ContentPath)
                                                          .Select(
                                                              d =>
                                                              new LibraryDescriptorModel
                                                                  {
                                                                      Id = d.Substring(root.Length),
                                                                      Created =
                                                                          XmlConvert.ToDateTime(
                                                                              XDocument.Load(Path.Combine(d, "info.xml"))
                                                                                       .Element("content")
                                                                                       .Attribute("created")
                                                                                       .Value, XmlDateTimeSerializationMode.Local)
                                                                  }).ToArray(),
                                     Current = App.Instance.Content.ContentFolder
                                 });
        }

        public ActionResult Delete(string id)
        {
            throw new NotImplementedException();
        }

        public ActionResult SetCurrent(string id)
        {
            throw new NotImplementedException();
        }
    }
}
