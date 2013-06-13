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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using LBi.LostDoc.Repository.Web.Areas.Administration.Models;
using LBi.LostDoc.Repository.Web.Configuration;
using LBi.LostDoc.Repository.Web.Extensibility;
using LBi.LostDoc.Repository.Web.Extensibility.Mvc;
using LBi.LostDoc.Repository.Web.Notifications;

namespace LBi.LostDoc.Repository.Web.Areas.Administration.Controllers
{
    // TODO this whole controller is BL soup, but it "works"
    [AdminController("repository", Text = "Repository", Group = Groups.Core, Order = 3000)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ContentRepositoryController : Controller
    {
        [HttpPost]
        public ActionResult Delete(string id)
        {
            // TODO this security check might not be good enough
            if (Directory.GetFiles(App.Instance.Content.RepositoryPath, id, SearchOption.TopDirectoryOnly).Length > 0)
            {
                System.IO.File.Delete(Path.Combine(App.Instance.Content.RepositoryPath, id));

                App.Instance.Notifications.Add(Severity.Information, 
                                               Lifetime.Page, 
                                               Scope.User, 
                                               this.User, 
                                               "File removed", 
                                               string.Format("Successfully deleted file: '{0}'.", id));
            }
            else
            {
                App.Instance.Notifications.Add(Severity.Error, 
                                               Lifetime.Page, 
                                               Scope.User, 
                                               this.User, 
                                               "File not found", 
                                               string.Format("Unable to delete file '{0}' as it was not found.", id));
            }

            return this.RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Download(string id)
        {
            // TODO this security check might not be good enough
            if (Directory.GetFiles(App.Instance.Content.RepositoryPath, id, SearchOption.TopDirectoryOnly).Length == 1)
            {
                return this.File(Path.Combine(App.Instance.Content.RepositoryPath, id), "text/xml", id);
            }

            return new HttpStatusCodeResult(HttpStatusCode.NotFound, id + " not found");
        }

        [AdminAction("index", IsDefault = true)]
        public ActionResult Index()
        {
            var ldocFiles = Directory.GetFiles(App.Instance.Content.RepositoryPath, 
                                               "*.ldoc", 
                                               SearchOption.TopDirectoryOnly);
            var ldocs = ldocFiles.Select(p => new LostDocFileInfo(p));
            var groups = ldocs.GroupBy(ld => ld.PrimaryAssembly.AssetId);

            List<AssemblyModel> assemblies = new List<AssemblyModel>();

            foreach (var group in groups)
            {
                assemblies.Add(new AssemblyModel
                                   {
                                       Name = group.First().PrimaryAssembly.Name, 
                                       Versions = group.Select(ld =>
                                                               new VersionModel
                                                                   {
                                                                       Filename = Path.GetFileName(ld.Path), 
                                                                       Created = System.IO.File.GetCreationTime(ld.Path), 
                                                                       Version = ld.PrimaryAssembly.AssetId.Version
                                                                   }).ToArray()
                                   });
            }

            return this.View(new ContentRepositoryModel
                                 {
                                     Assemblies = assemblies.ToArray()
                                 });
        }

        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase file)
        {
            string filename = Path.GetFileName(file.FileName);

            LostDocFileInfo fileInfo;
            using (TempDir tempDir = new TempDir(AppConfig.TempPath))
            {
                string tempLocation = Path.Combine(tempDir.Path, filename);
                file.SaveAs(tempLocation);

                fileInfo = new LostDocFileInfo(tempLocation);

                string targetFile = string.Format("{0}_{1}.ldoc", 
                                                  fileInfo.PrimaryAssembly.Filename, 
                                                  fileInfo.PrimaryAssembly.AssetId.Version);

                if (System.IO.File.Exists(Path.Combine(AppConfig.RepositoryPath, targetFile)))
                {
                    string message = string.Format("Unable to add file '{0}' as it already exists.", targetFile);
                    App.Instance.Notifications.Add(Severity.Error, 
                                                   Lifetime.Page, 
                                                   Scope.User, 
                                                   this.User, 
                                                   "Failed to upload file", 
                                                   message);
                }
                else
                {
                    System.IO.File.Move(tempLocation, Path.Combine(AppConfig.RepositoryPath, targetFile));

                    string message = string.Format("Successfully added file '{0}' (as '{1}') to repository.", filename, targetFile);
                    App.Instance.Notifications.Add(
                        Severity.Information, 
                        Lifetime.Page, 
                        Scope.User, 
                        this.User, 
                        "File uploaded", 
                        message);
                }
            }

            return this.RedirectToAction("Index");
        }
    }
}