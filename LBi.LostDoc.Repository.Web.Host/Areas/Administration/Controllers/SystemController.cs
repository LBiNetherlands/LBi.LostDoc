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

using System.ComponentModel.Composition;
using System.Linq;
using System.Web.Mvc;
using LBi.LostDoc.Extensibility;
using LBi.LostDoc.Repository.Web.Areas.Administration.Controllers;
using LBi.LostDoc.Repository.Web.Areas.Administration.Models;
using LBi.LostDoc.Repository.Web.Configuration;
using LBi.LostDoc.Repository.Web.Configuration.Composition;
using LBi.LostDoc.Repository.Web.Configuration.Xaml;
using LBi.LostDoc.Repository.Web.Extensibility.Mvc;
using LBi.LostDoc.Templating;
using Settings = LBi.LostDoc.Repository.Web.Configuration.Settings;

namespace LBi.LostDoc.Repository.Web.Host.Areas.Administration.Controllers
{
    [AdminController("system", Group = Groups.Core, Order = 3000, Text = "System")]
    public class SystemController : Controller
    {
        [ImportMany(ContractNames.TemplateProvider)]
        public IFileProvider[] FileProviders { get; set; }

        [Import]
        public ISettingsProvider SettingsProvider { get; set; }


        [AdminAction("index", IsDefault = true, Text = "Status")]
        public ActionResult Index()
        {
            TemplateResolver resolver = new TemplateResolver(this.FileProviders);
            return this.View(new SystemModel()
                                 {
                                     Templates = resolver.GetTemplates().ToArray(),
                                     CurrentTemplate = this.SettingsProvider.GetValue<string>(Settings.Template)
                                 });
        }

        //[AdminAction("logs", Text = "Logs")]
        //public ActionResult Logs()
        //{
        //    return View();
        //}
    }
}
