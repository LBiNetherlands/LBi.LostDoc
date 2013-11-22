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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Packaging;
using LBi.LostDoc.Packaging.Composition;
using LBi.LostDoc.Repository.Web.Areas.Administration;
using LBi.LostDoc.Repository.Web.Composition;
using LBi.LostDoc.Repository.Web.Configuration;
using LBi.LostDoc.Repository.Web.Configuration.Composition;
using LBi.LostDoc.Repository.Web.Configuration.Xaml;
using LBi.LostDoc.Repository.Web.Extensibility;
using LBi.LostDoc.Repository.Web.Extensibility.Http;
using LBi.LostDoc.Repository.Web.Extensibility.Mvc;
using LBi.LostDoc.Repository.Web.Host.Areas.Administration;
using LBi.LostDoc.Repository.Web.Notifications;
using LBi.LostDoc.Templating;
using ContractNames = LBi.LostDoc.Extensibility.ContractNames;
using Settings = LBi.LostDoc.Repository.Web.Configuration.Settings;
using TemplateInfo = LBi.LostDoc.Templating.TemplateInfo;

namespace LBi.LostDoc.Repository.Web.Host
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(CompositionContainer container, GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());

            // this filter injects Notifications into the IBaseModel
            filters.Add(new AdminFilter(container, container.GetExportedValue<NotificationManager>()));
        }

        public static void RegisterRoutes(CompositionContainer container, RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapHttpRoute("SearchCurrent", "library/search", new { controller = "Search", id = "current" });

            routes.MapHttpRoute("SearchArchive", "archive/{id}/search", new { controller = "Search" });

            routes.MapRoute(name: RouteConstants.ArchiveRouteName,
                            url: "archive/{id}/{*path}",
                            defaults: new
                                      {
                                          controller = "Content",
                                          id = "current",
                                          action = "GetContent",
                                          path = "Library.html"
                                      });

            routes.MapRoute(name: RouteConstants.LibraryRouteName,
                            url: "library/{*path}",
                            defaults: new
                                      {
                                          controller = "Content",
                                          id = "current",
                                          path = "Library.html",
                                          action = "GetContent"
                                      });

            routes.Add("Redirect", new Route(string.Empty, new RedirectRouteHandler("Library")));
        }

        protected void Application_Start()
        {
            // TODO maybe put this somewhere else (not in global.asax)
            // TODO maybe move all of this into the App class with "IAppConfig"

            // set up configuration
            string settingsPath = System.Configuration.ConfigurationManager.AppSettings["LostDoc.SettingsPath"];
            ISettingsProvider settings = new XamlSettingsProvider(Path.Combine(HostingEnvironment.ApplicationPhysicalPath, settingsPath));
            Func<string, string> abs = p => Path.Combine(HttpRuntime.AppDomainAppPath, p);

            foreach (string key in Settings.Paths)
            {
                string path = settings.GetValue<string>(key);
                string absPath = abs(path);
                if (!Directory.Exists(absPath))
                    Directory.CreateDirectory(absPath);
            }

            // initialize logger 
            // TODO replace with something that writes a new log every day
            string logFilename = string.Format("repository_{0:yyyy'-'MM'-'dd__HHmmss}.log", DateTime.Now);
            string logDir = abs(settings.GetValue<string>(Settings.LogPath));
            TraceListener traceListener = new TextWriterTraceListener(Path.Combine(logDir, logFilename));

            // TODO introduce flags/settings for controlling logging levels, but for now include everything
            traceListener.Filter = new EventTypeFilter(SourceLevels.All);

            traceListener.TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime;

            Web.TraceSources.Content.Listeners.Add(traceListener);
            Web.TraceSources.AddInManager.Listeners.Add(traceListener);
            Repository.TraceSources.ContentManagerSource.Listeners.Add(traceListener);
            Repository.TraceSources.ContentSearcherSource.Listeners.Add(traceListener);

            // this might be stupid, but it fixes things for iisexpress
            Directory.SetCurrentDirectory(HostingEnvironment.ApplicationPhysicalPath);

            // set up add-in system
            AddInSource officalSource = new AddInSource("Official LostDoc Add-in Repository",
                                                        settings.GetValue<string>(Settings.AddInRepository),
                                                        isOfficial: true);

            AddInSource localSource = new AddInSource("Local LostDoc Add-in Repository",
                                                      abs(settings.GetValue<string>(Settings.LocalRepositoryFolder)),
                                                      isOfficial: false);

            // intialize MEF

            // core 'add-ins'
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = currentAssembly.GetName();
            string corePackageId = assemblyName.Name;
            string corePackageVersion = assemblyName.Version.ToString();
            AggregateCatalog catalog = new AggregateCatalog();

            AddInRepository repository = new AddInRepository(officalSource, localSource);
            AddInManager addInManager = new AddInManager(repository,
                                                         abs(settings.GetValue<string>(Settings.AddInInstallPath)),
                                                         abs(settings.GetValue<string>(Settings.AddInPackagePath)),
                                                         abs(settings.GetValue<string>(Settings.TempPath)),
                                                         abs(settings.GetValue<string>(Settings.RequiredPackageConfigPath)));


            // create setings export provider
            SettingsExportProvider settingsExportProvider = new SettingsExportProvider(settings);

            // create container
            CompositionContainer container = new CompositionContainer(catalog, true, settingsExportProvider);

            // when the catalog changes, discover and route all ApiControllers
            // TODO could this be replaced with an ImportMany + recompositioning?
            catalog.Changed += (sender, args) => this.UpdateWebApiRegistry(container, args);

            //// TODO for debugging only
            //Debugger.Break();
            //Debugger.Launch();

            // now register core libs
            catalog.Catalogs.Add(new ApplicationCatalog(new AddInMetadataInjector(corePackageId, corePackageVersion)));

            // hook event so that installed add-ins get registered in the catalog, if composition occurs after this fires
            // or if recomposition is enabled, no restart should be requried
            addInManager.Installed += (sender, args) =>
                                      {
                                          if (args.Result == PackageResult.Ok)
                                          {
                                              catalog.Catalogs.Add(new DirectoryCatalog(args.InstallationPath, new AddInMetadataInjector(args.Package.Id, args.Package.Version)));
                                          }
                                      };

            // delete and redeploy all installed packages, this will trigger the Installed event ^
            // this acts as a crude "remove/overwrite plugins that were in use when un/installed" hack
            if (addInManager.Restore() == PackageResult.PendingRestart)
            {
                // restart app domain in case this failed
                HttpRuntime.UnloadAppDomain();
            }

            // set up template resolver
            var lazyProviders = container.GetExports<IFileProvider>(ContractNames.TemplateProvider);
            var realProviders = lazyProviders.Select(lazy => lazy.Value);
            TemplateResolver templateResolver = new TemplateResolver(realProviders.ToArray());

            // load template
            TemplateInfo templateInfo = templateResolver.Resolve(settings.GetValue<string>(Settings.Template));
            Template template = templateInfo.Load(container);

            // set up content manager
            ContentSettings contentSettings = new ContentSettings
                                                  {
                                                      ContentPath = abs(settings.GetValue<string>(Settings.ContentPath)),
                                                      IgnoreVersionComponent = settings.GetValue<VersionComponent>(Settings.IgnoreVersionComponent),
                                                      RepositoryPath = abs(settings.GetValue<string>(Settings.RepositoryPath)),
                                                      Template = template
                                                  };
            ContentManager contentManager = new ContentManager(contentSettings);

            // set up notifaction system
            NotificationManager notifications = new NotificationManager();

            // register application services in composition container
            CompositionBatch batch = new CompositionBatch();

            this.AddExport(batch, notifications);
            this.AddExport(batch, contentManager);
            this.AddExport(batch, addInManager);
            // TODO invert this by [Export]ing all TraceSources for automated discovery?
            this.AddExport(batch, traceListener);
            this.AddExport(batch, container);
            this.AddExport(batch, settings);
            this.AddExport(batch, templateResolver);

            container.Compose(batch);

            // MVC init
            AreaRegistration.RegisterAllAreas();
            RegisterGlobalFilters(container, GlobalFilters.Filters);
            RegisterRoutes(container, RouteTable.Routes);

            // inject our custom IControllerFactory for the Admin interface
            //IControllerFactory oldControllerFactory = ControllerBuilder.Current.GetControllerFactory();
            IControllerFactory innerFactory = new ControllerFactory(container);
            IControllerFactory newControllerFactory = new AddInControllerFactory(AdministrationAreaRegistration.Name,
                                                                                 container,
                                                                                 innerFactory);
            ControllerBuilder.Current.SetControllerFactory(newControllerFactory);

            FilterProviders.Providers.Add(new AddInFilterProvider(container));

            // WebAPI init
            GlobalConfiguration.Configuration.DependencyResolver = new MefDependencyResolver(container,
                                                                                             GlobalConfiguration.Configuration.DependencyResolver);
        }

        private void AddExport<T>(CompositionBatch batch, T instance)
        {
            var metadata = new Dictionary<string, object>();
            metadata.Add("ExportTypeIdentity", AttributedModelServices.GetTypeIdentity(typeof(T)));
            batch.AddExport(new Export(AttributedModelServices.GetContractName(typeof(T)),
                                       metadata,
                                       () => instance));
        }

        private void UpdateWebApiRegistry(CompositionContainer container, ComposablePartCatalogChangeEventArgs eventArg)
        {
            using (TraceSources.AddInManager.TraceActivity("Updating WebApi routes."))
            {
                var httpRouteInitializers = container.GetExports<IHttpRouteInitializer, IAddInMetadata>();

                HttpRouteCollection routeCollection = GlobalConfiguration.Configuration.Routes;

                routeCollection.Clear();

                foreach (var routeInitializer in httpRouteInitializers)
                {
                    IAddInMetadata metadata = routeInitializer.Metadata;

                    var httpRouteInitializer = routeInitializer.Value;

                    AddInHttpRouteRewriter routeRewriter = new AddInHttpRouteRewriter(routeCollection, metadata);
                    httpRouteInitializer.RegisterRoutes(routeRewriter);
                }
            }
        }
    }
}