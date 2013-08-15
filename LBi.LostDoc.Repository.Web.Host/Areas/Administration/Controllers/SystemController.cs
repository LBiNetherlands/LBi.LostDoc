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
using System.IO;
using System.IO.Compression;
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
        // TODO move to ctor with [ImportingConstructor]
        [Import]
        public ISettingsProvider SettingsProvider { get; set; }

        [Import]
        public TemplateResolver TemplateResolver { get; set; }

        [AdminAction("index", IsDefault = true, Text = "Status")]
        public ActionResult Index()
        {
            string currentTemplateName = this.SettingsProvider.GetValue<string>(Settings.Template);
            TemplateInfo template;

            if (!this.TemplateResolver.TryResolve(currentTemplateName, out template))
            {
                template = this.TemplateResolver.GetTemplates().FirstOrDefault();
                if (template == null)
                    currentTemplateName = null;
                else
                    currentTemplateName = template.Name;
            }

            TemplateParameterModel[] templateParameters;
            if (template != null)
                templateParameters = Array.ConvertAll(template.Parameters, this.CreateTemplateParameterModel);
            else
                templateParameters = new TemplateParameterModel[0];

            return this.View(new SystemModel()
                                 {
                                     Templates = this.TemplateResolver.GetTemplates().Select(ti => ti.Name).ToArray(),
                                     CurrentTemplate = currentTemplateName,
                                     TemplateParameters = templateParameters
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

        public FileResult DownloadTemplate(string template)
        {
            TemplateInfo templateInfo = this.TemplateResolver.Resolve(template);

            MemoryStream buffer = new MemoryStream();
            using (ZipArchive archive = new ZipArchive(buffer, ZipArchiveMode.Create, true))
            {
                foreach (string filename in templateInfo.GetFiles())
                {
                    var fileEntry = archive.CreateEntry(filename, CompressionLevel.Optimal);
                    using (var target = fileEntry.Open())
                    using (var content = templateInfo.Source.OpenFile(filename, FileMode.Open))
                    {
                        content.CopyTo(target);
                        content.Close();
                        target.Close();
                    }
                }
            }
            buffer.Seek(0, SeekOrigin.Begin);
            return new FileStreamResult(buffer, "application/zip")
                       {
                           FileDownloadName = string.Format("template_{0}.zip", template).Replace(' ', '_')
                       };
        }
        //[AdminAction("logs", Text = "Logs")]
        //public ActionResult Logs()
        //{
        //    return View();
        //}
    }
}
