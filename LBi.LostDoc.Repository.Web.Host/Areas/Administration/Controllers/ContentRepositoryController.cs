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
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Hosting;
using System.Runtime.Versioning;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using LBi.LostDoc.Enrichers;
using LBi.LostDoc.Filters;
using LBi.LostDoc.Repository.Scheduling;
using LBi.LostDoc.Repository.Web.Areas.Administration.Controllers;
using LBi.LostDoc.Repository.Web.Areas.Administration.Models;
using LBi.LostDoc.Repository.Web.Configuration;
using LBi.LostDoc.Repository.Web.Extensibility.Mvc;
using LBi.LostDoc.Repository.Web.Host.Areas.Administration.Models;
using LBi.LostDoc.Repository.Web.Notifications;

namespace LBi.LostDoc.Repository.Web.Host.Areas.Administration.Controllers
{
    // TODO this whole controller is BL soup, but it "works"
    [AdminController("repository", Text = "Repository", Group = Groups.Core, Order = 3000)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ContentRepositoryController : Controller
    {
        [ImportingConstructor]
        public ContentRepositoryController(CompositionContainer container, IJobQueue jobQueue, ContentManager content, NotificationManager notifications)
        {
            this.Container = container;
            this.Content = content;
            this.Notifications = notifications;
            this.JobQueue = jobQueue;
        }

        protected IJobQueue JobQueue { get; set; }

        protected CompositionContainer Container { get; set; }

        protected ContentManager Content { get; set; }

        protected NotificationManager Notifications { get; set; }

        [HttpPost]
        public ActionResult Delete(string id)
        {
            // TODO this security check might not be good enough
            if (Directory.GetFiles(this.Content.RepositoryPath, id, SearchOption.TopDirectoryOnly).Length > 0)
            {
                System.IO.File.Delete(Path.Combine(this.Content.RepositoryPath, id));

                this.Notifications.Add(Severity.Information,
                                               Lifetime.Page,
                                               Scope.User,
                                               this.User,
                                               "File removed",
                                               string.Format("Successfully deleted file: '{0}'.", id));
            }
            else
            {
                this.Notifications.Add(Severity.Error,
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
            if (Directory.GetFiles(this.Content.RepositoryPath, id, SearchOption.TopDirectoryOnly).Length == 1)
            {
                return this.File(Path.Combine(this.Content.RepositoryPath, id), "text/xml", id);
            }

            return new HttpStatusCodeResult(HttpStatusCode.NotFound, id + " not found");
        }

        [AdminAction("index", Text = "Content", IsDefault = true)]
        public ActionResult Index()
        {
            string repositoryPath = this.Content.RepositoryPath;

            return this.View(new ContentRepositoryModel
                                 {
                                     Assemblies = CreateAssemblyModels(repositoryPath).ToArray()
                                 });
        }

        private static List<AssemblyModel> CreateAssemblyModels(string repositoryPath)
        {
            var ldocFiles = Directory.GetFiles(repositoryPath,
                                               "*.ldoc",
                                               SearchOption.TopDirectoryOnly);
            var ldocs = ldocFiles.Select(p => new LostDocFileInfo(p));
            var groups = ldocs.GroupBy(ld => ld.PrimaryAssembly.AssetId);

            List<AssemblyModel> assemblies = new List<AssemblyModel>();

            foreach (var group in groups)
            {
                assemblies.Add(new AssemblyModel
                               {
                                   Name = @group.First().PrimaryAssembly.Name,
                                   Versions = @group.Select(ld =>
                                                            new VersionModel
                                                            {
                                                                Filename = Path.GetFileName(ld.Path),
                                                                Created = System.IO.File.GetCreationTime(ld.Path),
                                                                Version = ld.PrimaryAssembly.AssetId.Version
                                                            }).ToArray()
                               });
            }
            return assemblies;
        }

        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase file)
        {
            string filename = Path.GetFileName(file.FileName);

            using (TempDir tempDir = new TempDir(AppConfig.TempPath))
            {
                string tempLocation = Path.Combine(tempDir.Path, filename);
                file.SaveAs(tempLocation);

                this.UploadLostDocFile(tempLocation);
            }

            return this.RedirectToAction("Index");
        }

        private void UploadLostDocFile(string tempLocation)
        {
            string filename = Path.GetFileName(tempLocation);

            LostDocFileInfo fileInfo = new LostDocFileInfo(tempLocation);

            string targetFile = string.Format("{0}_{1}.ldoc",
                                              fileInfo.PrimaryAssembly.Filename,
                                              fileInfo.PrimaryAssembly.AssetId.Version);

            if (System.IO.File.Exists(Path.Combine(AppConfig.RepositoryPath, targetFile)))
            {
                string message = string.Format("Unable to add file '{0}' as it already exists.", targetFile);
                this.Notifications.Add(Severity.Error,
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
                this.Notifications.Add(Severity.Information,
                                       Lifetime.Page,
                                       Scope.User,
                                       this.User,
                                       "File uploaded",
                                       message);
            }
        }

        [HttpPost]
        public ActionResult UploadAndExtract(HttpPostedFileBase assembly, HttpPostedFileBase xml)
        {

            string assemblyFilename = Path.GetFileName(assembly.FileName);
            string xmlDocFilename;
            if (xml == null)
                xmlDocFilename = null;
            else
                xmlDocFilename = Path.GetFileName(xml.FileName);

            TempDir tempDir = new TempDir(AppConfig.TempPath);

            string tempLocation = Path.Combine(tempDir.Path, assemblyFilename);
            assembly.SaveAs(tempLocation);

            if (xml != null)
                xml.SaveAs(Path.Combine(tempDir.Path, xmlDocFilename));

            this.JobQueue.Enqueue(new Job(string.Format("Extract LostDoc file from '{0}'", assemblyFilename),
                                          c =>
                                          {
                                              //AppDomainInitializer activator = args => this.GenerateLostDoc(args[0]);
                                              //var ad = AppDomain.CreateDomain("", AppDomain.CurrentDomain.Evidence,
                                              //                                new AppDomainSetup()
                                              //                                {
                                              //                                    AppDomainInitializer = activator,
                                              //                                    AppDomainInitializerArguments = new[] {assemblyFilename},
                                              //                                });
                                              var ad = AppDomain.CreateDomain("Extract AppDomain", AppDomain.CurrentDomain.Evidence);
                                              try
                                              {
                                                  ad.DoCallBack(() => this.GenerateLostDoc(tempLocation));
                                              }
                                              finally
                                              {
                                                  AppDomain.Unload(ad);
                                                  tempDir.Dispose();
                                              }
                                          }));

            string message = string.Format("Added LostDoc extraction to queue");
            this.Notifications.Add(Severity.Information,
                                   Lifetime.Page,
                                   Scope.User,
                                   this.User,
                                   "Files uploaded",
                                   message);

            return this.RedirectToAction("Index");
        }

        protected static void GenerateLostDoc(string assemblyPath)
        {
            DocGenerator gen = new DocGenerator(this.Container);
            gen.AssetFilters.Add(new ComObjectTypeFilter());
            gen.AssetFilters.Add(new CompilerGeneratedFilter());
            gen.AssetFilters.Add(new PublicTypeFilter());
            gen.AssetFilters.Add(new PrivateImplementationDetailsFilter());
            gen.AssetFilters.Add(new DynamicallyInvokableAttributeFilter());
            gen.AssetFilters.Add(new CompilerGeneratedFilter());
            gen.AssetFilters.Add(new LogicalMemberInfoVisibilityFilter());
            gen.AssetFilters.Add(new SpecialNameMemberInfoFilter());


            XmlDocEnricher docEnricher = new XmlDocEnricher();
            gen.Enrichers.Add(docEnricher);

            // TODO this will require a local-edit db
            //var namespaceEnricher = new ExternalNamespaceDocEnricher();
            //if (System.IO.Path.IsPathRooted(this.NamespaceDocPath))
            //    namespaceEnricher.Load(this.NamespaceDocPath);
            //else if (
            //    File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path),
            //                                       this.NamespaceDocPath)))
            //    namespaceEnricher.Load(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path),
            //                                                  this.NamespaceDocPath));
            //else
            //    namespaceEnricher.Load(this.NamespaceDocPath);

            //gen.Enrichers.Add(namespaceEnricher);


            string winPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string bclDocPath = Path.Combine(winPath,
                                             @"microsoft.net\framework\",
                                             string.Format("v{0}.{1}.{2}",
                                                           Environment.Version.Major,
                                                           Environment.Version.Minor,
                                                           Environment.Version.Build),
                                             @"en\");


            docEnricher.AddPath(bclDocPath);

            bclDocPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                                      @"Reference Assemblies\Microsoft\Framework\.NETFramework",
                                      string.Format("v{0}.{1}",
                                                    Environment.Version.Major,
                                                    Environment.Version.Minor));

            docEnricher.AddPath(bclDocPath);


            gen.AddAssembly(assemblyPath);

            var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);

            XDocument rawDoc = gen.Generate();


            using (TempDir tempDir = new TempDir(AppConfig.TempPath))
            {
                string filename = string.Format("{0}_{1}.ldoc",
                                                System.IO.Path.GetFileName(assemblyPath),
                                                assemblyName.Version);

                string tempLocation = Path.Combine(tempDir.Path, filename);
                rawDoc.Save(tempLocation);

                this.UploadLostDocFile(tempLocation);
            }
        }
    }
}