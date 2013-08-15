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
using System.ComponentModel.Composition;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using LBi.LostDoc.Extensibility;
using LBi.LostDoc.Repository.Web.Areas.Administration.Controllers;
using LBi.LostDoc.Repository.Web.Areas.Administration.Models;
using LBi.LostDoc.Repository.Web.Configuration;
using LBi.LostDoc.Repository.Web.Configuration.Composition;
using LBi.LostDoc.Repository.Web.Configuration.Xaml;
using LBi.LostDoc.Repository.Web.Extensibility.Mvc;
using LBi.LostDoc.Repository.Web.Host.Areas.Administration.Models;
using LBi.LostDoc.Templating;
using Settings = LBi.LostDoc.Repository.Web.Configuration.Settings;
using TemplateInfo = LBi.LostDoc.Templating.TemplateInfo;

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
            string currentTemplateName = this.SettingsProvider.GetValue<string>(Settings.Template);
            TemplateInfo template;

            if (!resolver.Resolve(currentTemplateName, out template))
            {
                template = resolver.GetTemplates().FirstOrDefault();
                if (template == null)
                    currentTemplateName = null;
                else
                    currentTemplateName = template.Name;
            }

            TemplateParameterModel[] settings;
            if (template != null)
                settings = Array.ConvertAll(template.Parameters, this.CreateTemplateParameterModel);
            else
                settings = new TemplateParameterModel[0];

            return this.View(new SystemModel()
                                 {
                                     Templates = resolver.GetTemplates().Select(ti => ti.Name).ToArray(),
                                     CurrentTemplate = currentTemplateName,
                                     Settings = settings
                                 });
        }

        private TemplateParameterModel CreateTemplateParameterModel(TemplateParameterInfo templateParameterInfo)
        {
            return new TemplateParameterModel
                   {
                       Name = templateParameterInfo.Name,
                       Description = templateParameterInfo.Description,
                       DefaultValue = templateParameterInfo.DefaultExpression,
                       Value = this.SettingsProvider.GetValueOrDefault<string>(Settings.TemplateParameterPrefix + templateParameterInfo.Name)
                   };
        }

        //[AdminAction("logs", Text = "Logs")]
        //public ActionResult Logs()
        //{
        //    return View();
        //}
    }
}
