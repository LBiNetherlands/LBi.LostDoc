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
using System.Xml;
using System.Xml.Linq;
using LBi.LostDoc.Repository.Web.Areas.Administration.Controllers;
using LBi.LostDoc.Repository.Web.Areas.Administration.Models;
using LBi.LostDoc.Repository.Web.Configuration;
using LBi.LostDoc.Repository.Web.Extensibility;
using LBi.LostDoc.Repository.Web.Host.Areas.Administration.Models;
using LBi.LostDoc.Repository.Web.Notifications;

namespace LBi.LostDoc.Repository.Web.Host.Areas.Administration.Controllers
{
    [AdminController("library", Group = Groups.Core, Order = 2500, Text = "Library")]
    public class LibraryController : Controller
    {
        [ImportingConstructor]
        public LibraryController(NotificationManager notificationManager)
        {
            this.Notifications = notificationManager;
        }

        [Import]
        protected new ContentManager Content { get; set; }

        protected NotificationManager Notifications { get; set; }

        public ActionResult Build()
        {
            this.Content.QueueRebuild("Requested by " + this.User.Identity.Name);
            this.Notifications.Add(Severity.Information, 
                                   Lifetime.Page, 
                                   Scope.User, 
                                   this.User, 
                                   "Rebuilding Content", 
                                   "A new content rebuild request has been added to the processing queue.");

            return this.Redirect(this.Url.Action("Index"));
        }

        public ActionResult Delete(string id)
        {
            string contentRoot = this.Content.GetContentRoot(id);
            Directory.Delete(contentRoot, true);
            this.Notifications.Add(Severity.Information, 
                                   Lifetime.Page, 
                                   Scope.User, 
                                   this.User, 
                                   "Library Deleted", 
                                   string.Format("The library with id {0} has been deleted.", id));

            return this.Redirect(this.Url.Action("Index"));
        }

        public ActionResult Details(string id)
        {
            string contentRoot = App.Instance.Content.GetContentRoot(id);

            var files = Directory.GetFiles(Path.Combine(contentRoot, "Source"), "*.ldoc");
            var ldocs = files.Select(f => new LostDocFileInfo(f));
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

            string htmlRoot = Path.Combine(contentRoot, "Html");

            return this.View(new LibraryDetailsModel
                                 {
                                     Input = new ContentRepositoryModel
                                                 {
                                                     IsReadOnly = true, 
                                                     Assemblies = assemblies.ToArray()
                                                 }, 
                                     OutputDataUrl = this.Url.Action("LibraryHtmlFiles", new { id }), 
                                     OutputDownloadUrl = this.Url.Action("DownloadHtmlFile", new { id }).TrimEnd('/') + "?path=", 
                                     OutputViewUrl = this.Url.RouteUrl("Archive", new { id, path = string.Empty }).TrimEnd('/'), 
                                     LogDataUrl = this.Url.Action("LibraryLogFiles", new { id }), 
                                     LogDownloadUrl = this.Url.Action("LogFile", new { id }).TrimEnd('/') + "?d=", 
                                     LogViewUrl = this.Url.Action("LogFile", new { id, path = string.Empty }).TrimEnd('/') + "?v=", 
                                     Created = Directory.GetCreationTime(htmlRoot)
                                 });
        }

        public ActionResult DownloadHtmlFile(string id, string path)
        {
            return this.DownloadFile(id, "Html", path);
        }

        public ActionResult DownloadSourceFile(string id, string path)
        {
            return this.DownloadFile(id, "Source", path);
        }

        [AdminAction("index", IsDefault = true)]
        public ActionResult Index()
        {
            string root = Path.GetFullPath(AppConfig.ContentPath);
            IEnumerable<string> directories = Directory.EnumerateDirectories(AppConfig.ContentPath);
            var libraries = directories.Where(p => System.IO.File.Exists(Path.Combine(p, "info.xml")))
                                       .Select(d => CreateLibraryModel(d, root));

            return this.View(new LibraryModel
                                 {
                                     SystemState = App.Instance.Content.CurrentState, 
                                     Libraries = libraries.ToArray(), 
                                     Current = App.Instance.Content.ContentFolder
                                 });
        }

        public ActionResult LibraryHtmlFiles(string id, string dir)
        {
            return this.DirectoryListing(id, "Html", dir);
        }

        public ActionResult LibraryLogFiles(string id, string dir)
        {
            return this.DirectoryListing(id, "Logs", dir);
        }

        public ActionResult LogFile(string id, string v, string d)
        {
            return this.DownloadFile(id, "Logs", v ?? d, v == null);
        }

        public ActionResult SetCurrent(string id)
        {
            this.Content.SetCurrentContentFolder(id);
            this.Notifications.Add(Severity.Information, 
                                   Lifetime.Page, 
                                   Scope.User, 
                                   this.User, 
                                   "New Library Content", 
                                   "The library content has changed", 
                                   NotificationActions.Refresh);

            return this.Redirect(this.Url.Action("Index"));
        }

        private static LibraryDescriptorModel CreateLibraryModel(string d, string root)
        {
            string value = XDocument.Load(Path.Combine(d, "info.xml")).Element("content").Attribute("created").Value;
            return new LibraryDescriptorModel
                       {
                           Id = d.Substring(root.Length), 
                           Created = XmlConvert.ToDateTime(value, 
                                                           XmlDateTimeSerializationMode.Local)
                       };
        }

        private ActionResult DirectoryListing(string id, string folder, string path)
        {
            path = (path ?? string.Empty).TrimStart('/');
            string contentRoot = App.Instance.Content.GetContentRoot(id);
            string folderPath = Path.Combine(contentRoot, folder);
            string contentDir = Path.Combine(folderPath, path);

            DirectoryInfo di = new DirectoryInfo(contentDir);

            return this.View("_DirectoryListing", 
                             new DirectoryListModel
                                 {
                                     Root = new DirectoryInfo(folderPath), 
                                     Directories = di.GetDirectories(), 
                                     Files = di.GetFiles()
                                 });
        }

        private ActionResult DownloadFile(string id, string folder, string path, bool asDownload = true)
        {
            string contentRoot = this.Content.GetContentRoot(id);
            string htmlRoot = Path.Combine(contentRoot, folder);

            string realPath = Path.Combine(htmlRoot, path.TrimStart('/'));
            if (realPath.StartsWith(htmlRoot))
            {
                FilePathResult result = new FilePathResult(realPath, "text/text");

                if (asDownload)
                    result.FileDownloadName = Path.GetFileName(realPath);

                return result;
            }

            throw new HttpException((int)HttpStatusCode.Forbidden, "Forbidden!");
        }
    }
}