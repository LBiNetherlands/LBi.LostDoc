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
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LBi.LostDoc.Repository.Web.Areas.Administration.Models;

namespace LBi.LostDoc.Repository.Web.Areas.Administration.Controllers
{
    public class AddInController : Controller
    {
        // default action is to list all installed add-ins
        public ActionResult Index()
        {

            return View(new AddInOverviewModel
                            {
                                Title = "Installed Add-ins",
                                AddIns = App.Instance.AddInManager
                                            .Select(pkg =>
                                                    new AddInModel
                                                        {
                                                            CanInstall = false,
                                                            CanUninstall = true,
                                                            CanUpdate = false,
                                                            Package = pkg
                                                        }).ToArray()
                            });
        }

        public ActionResult Repository()
        {
            const int count = 10;
            AddInModel[] results = App.Instance.AddInManager.Repository.Search(null, true, 0, count)
                             .Select(pkg =>
                                     new AddInModel
                                     {
                                         CanInstall = true,
                                         CanUninstall = true,
                                         CanUpdate = false,
                                         Package = pkg
                                     }).ToArray();

            return View(new SearchResultModel
                            {
                                Results = results,
                                NextOffset = results.Length == count ? count: (int?) null
                            });
        }

        public ActionResult Search(string terms, int offset = 0)
        {
            const int count = 10;
            AddInModel[] results = App.Instance.AddInManager.Repository.Search(terms, true, offset, count)
                             .Select(pkg =>
                                     new AddInModel
                                         {
                                             CanInstall = true,
                                             CanUninstall = true,
                                             CanUpdate = false,
                                             Package = pkg
                                         }).ToArray();

            return View(new SearchResultModel
                            {
                                Results = results,
                                NextOffset = results.Length == count ? offset + results.Length : (int?)null
                            });
        }

        [HttpPost]
        public ActionResult Install([Bind(Prefix = "package-id")]string id, [Bind(Prefix = "package-version")]string version)
        {
            return new EmptyResult();
        }

        [HttpPost]
        public ActionResult Uninstall([Bind(Prefix = "package-id")]string id, [Bind(Prefix = "package-version")]string version)
        {
            return new EmptyResult();
        }

        [HttpPost]
        public ActionResult Update([Bind(Prefix = "package-id")]string id, [Bind(Prefix = "package-version")]string version)
        {
            return new EmptyResult();

        }
    }
}
