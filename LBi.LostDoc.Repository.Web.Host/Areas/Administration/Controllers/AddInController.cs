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
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using LBi.LostDoc.Packaging;
using LBi.LostDoc.Repository.Web.Areas.Administration.Controllers;
using LBi.LostDoc.Repository.Web.Extensibility.Mvc;
using LBi.LostDoc.Repository.Web.Host.Areas.Administration.Models;
using LBi.LostDoc.Repository.Web.Notifications;

namespace LBi.LostDoc.Repository.Web.Host.Areas.Administration.Controllers
{
    // TODO the TEXT isn't translateable, but it's good enough for now
    [AdminController("addins", Text = "Add-ins", Group = Groups.Core, Order = 1)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class AddInController : Controller
    {
        [ImportingConstructor]
        public AddInController(AddInManager addIns, NotificationManager notifications)
        {
            this.AddIns = addIns;
            this.Notifications = notifications;
        }

        protected NotificationManager Notifications { get; set; }

        protected AddInManager AddIns { get; set; }

        // default action is to list all installed add-ins
        [HttpPost]
        public ActionResult Install([Bind(Prefix = "package-id")] string id, 
                                    [Bind(Prefix = "package-version")] string version)
        {
            ActionResult ret;
            var pkg = this.AddIns.Repository.Get(id, version);
            if (pkg == null)
            {
                string message = string.Format("Package (Id: '{0}', Version: '{1}') not found.", id, version);
                ret = new HttpStatusCodeResult(HttpStatusCode.NotFound, message);
            }
            else
            {
                // TODO handle errors
                PackageResult result = this.AddIns.Install(pkg);
                if (result == PackageResult.PendingRestart)
                {
                    string message =
                        string.Format("Package {0} failed to install, another attempt will be made upon site restart.", 
                                      pkg.Id);
                    this.Notifications.Add(Severity.Warning, 
                                                   Lifetime.Application, 
                                                   Scope.Administration, 
                                                   "Pending restart", 
                                                   message, 
                                                   NotificationActions.Restart);
                }

                ret = this.Redirect(this.Url.Action("Installed"));
            }

            return ret;
        }

        [AdminAction("installed", Text = "Installed", IsDefault = true)]
        public ActionResult Installed()
        {
            return this.View(new AddInOverviewModel
                                 {
                                     Title = "Manage Add-ins", 
                                     AddIns = this.AddIns
                                                 .Select(pkg =>
                                                         new AddInModel
                                                             {
                                                                 CanInstall = false, 
                                                                 CanUninstall = true, 
                                                                 CanUpdate = this.CheckForUpdates(pkg), 
                                                                 Package = pkg
                                                             }).ToArray()
                                 });
        }

        [AdminAction("repository", Text = "Repository")]
        public ActionResult Repository(int[] source, bool? includePrerelease, string terms = null, int offset = 0, int count = 1000)
        {
            if (source == null)
                source = Enumerable.Range(0, this.AddIns.Repository.Sources.Length).ToArray();

            AddInRepository repository = new AddInRepository(source.Select(i => this.AddIns.Repository.Sources[i]).ToArray());

            AddInModel[] results = repository.Search(terms, includePrerelease.HasValue && includePrerelease.Value, offset, count)
                                      .Select(pkg =>
                                              new AddInModel
                                                  {
                                                      CanInstall = !this.CheckInstalled(pkg), 
                                                      CanUninstall = this.CheckInstalled(pkg), 
                                                      CanUpdate = this.CheckInstalled(pkg) && this.CheckForUpdates(pkg), 
                                                      Package = pkg
                                                  }).ToArray();

            

            return this.View(new SearchResultModel
                                 {
                                     Title = "Add-in Repository", 
                                     AddInSources = this.AddIns.Repository.Sources.Select((s,i) => new AddInSourceModel {Name = s.Name, Enabled = source.Contains(i)}).ToArray(),
                                     Results = results,
                                     NextOffset = results.Length == count ? count : (int?)null,
                                     Terms = terms,
                                     IncludePrerelease = includePrerelease ?? false
                                 });
        }

        public ActionResult Search(int[] source, bool? includePrerelease, string terms, int offset = 0)
        {
            const int COUNT = 10;

            if (source == null)
                source = Enumerable.Range(0, this.AddIns.Repository.Sources.Length).ToArray();

            AddInRepository repository = new AddInRepository(source.Select(i => this.AddIns.Repository.Sources[i]).ToArray());

            AddInModel[] results = repository.Search(terms, includePrerelease.HasValue && includePrerelease.Value, offset, COUNT)
                                             .Select(pkg =>
                                                     new AddInModel
                                                     {
                                                         CanInstall = !this.CheckInstalled(pkg),
                                                         CanUninstall = this.CheckInstalled(pkg),
                                                         CanUpdate = this.CheckInstalled(pkg) && this.CheckForUpdates(pkg),
                                                         Package = pkg
                                                     }).ToArray();

            return this.View(new SearchResultModel
                                 {
                                     Results = results, 
                                     NextOffset = results.Length == COUNT ? offset + results.Length : (int?)null
                                 });
        }

        [HttpPost]
        public ActionResult Uninstall([Bind(Prefix = "package-id")] string id, 
                                      [Bind(Prefix = "package-version")] string version)
        {
            ActionResult ret;
            var pkg = this.AddIns.Get(id, version);
            if (pkg == null)
            {
                string message = string.Format("Package (Id: '{0}', Version: '{1}') not found.", id, version);
                ret = new HttpStatusCodeResult(HttpStatusCode.NotFound, message);
            }
            else
            {
                // TODO handle errors
                PackageResult result = this.AddIns.Uninstall(pkg);
                if (result == PackageResult.PendingRestart)
                {
                    string message =
                        string.Format(
                            "Package {0} failed to uninstall, another attempt will be made upon site restart.", pkg.Id);
                    this.Notifications.Add(
                        Severity.Warning, 
                        Lifetime.Application, 
                        Scope.Administration, 
                        "Pending restart", 
                        message, 
                        NotificationActions.Restart);
                }

                ret = this.Redirect(this.Url.Action("Installed"));
            }

            return ret;
        }

        [HttpPost]
        public ActionResult Update([Bind(Prefix = "package-id")] string id, 
                                   [Bind(Prefix = "package-version")] string version)
        {
            ActionResult ret;
            var pkg = this.AddIns.Repository.Get(id, version);
            if (pkg == null)
            {
                string message = string.Format("Package (Id: '{0}', Version: '{1}') not found.", id, version);
                ret = new HttpStatusCodeResult(HttpStatusCode.NotFound, 
                                               message);
            }
            else
            {
                // TODO handle errors
                PackageResult result = this.AddIns.Update(pkg);
                if (result == PackageResult.PendingRestart)
                {
                    string message =
                        string.Format("Package {0} failed to update, another attempt will be made upon site restart.", 
                                      pkg.Id);
                    this.Notifications.Add(
                        Severity.Warning, 
                        Lifetime.Application, 
                        Scope.Administration, 
                        "Pending restart", 
                        message, 
                        NotificationActions.Restart);
                }

                ret = this.Redirect(this.Url.Action("Installed"));
            }

            return ret;
        }

        private bool CheckForUpdates(AddInPackage pkg)
        {
            // TODO fix prerelase hack
            return this.AddIns.Repository.GetUpdate(pkg, true) != null;
        }

        private bool CheckInstalled(AddInPackage pkg)
        {
            return this.AddIns.Contains(pkg);
        }
    }
}