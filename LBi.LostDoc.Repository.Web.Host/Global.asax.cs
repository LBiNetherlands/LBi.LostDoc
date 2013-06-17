/*
 * Copyright 2012-2013 LBi Netherlands B.V.
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
using LBi.LostDoc.Repository.Web.Notifications;
using LBi.LostDoc.Templating;
using ContractNames = LBi.LostDoc.Extensibility.ContractNames;
using Settings = LBi.LostDoc.Repository.Web.Configuration.Settings;

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
            filters.Add(new AdminFilter(container.GetExportedValue<NotificationManager>()));
        }

        public static void RegisterRoutes(CompositionContainer container, RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapHttpRoute("Search", "search/{id}/{searchTerms}", new { controller = "Search" });

            routes.MapRoute(
                name: "Archive", 
                url: "archive/{id}/{*path}", 
                defaults: new
                              {
                                  controller = "Content", 
                                  id = "current", 
                                  action = "GetContent", 
                                  path = "Library.html"
                              });

            routes.MapRoute(
                name: "Library", 
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

            // initialize logger
            TraceListener traceListener =
                new TextWriterTraceListener(Path.Combine(abs(settings.GetValue<string>(Settings.LogPath)),
                                                         string.Format("repository_{0:yyyy'-'MM'-'dd__HHmmss}.log",
                                                                       DateTime.Now)));

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
            AddInSource officalSource = new AddInSource("Official LostDoc repository add-in feed", 
                                                        settings.GetValue<string>(Settings.AddInRepository),
                                                        isOfficial: true);

            // intialize MEF

            // core 'add-ins'
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = currentAssembly.GetName();
            string corePackageId = assemblyName.Name;
            string corePackageVersion = assemblyName.Version.ToString();
            AggregateCatalog catalog = new AggregateCatalog();

            // load other sources from site-settings (not config)
            AddInRepository repository = new AddInRepository(officalSource);
            AddInManager addInManager = new AddInManager(repository,
                                                         abs(settings.GetValue<string>(Settings.AddInInstallPath)),
                                                         abs(settings.GetValue<string>(Settings.AddInPackagePath)));

            // when the catalog changes, discover and route all ApiControllers
            catalog.Changed += (sender, args) => this.UpdateWebApiRegistry(args);

            //// TODO for debugging only
            //Debugger.Break();
            //Debugger.Launch();

            // now register core libs
            catalog.Catalogs.Add(new AddInCatalog(new ApplicationCatalog(), corePackageId, corePackageVersion));

            // hook event so that installed add-ins get registered in the catalog, if composition occurs after this fires
            // or if recomposition is enabled, no restart should be requried
            addInManager.Installed +=
                (sender, args) => catalog.Catalogs.Add(new AddInCatalog(new DirectoryCatalog(args.InstallationPath), 
                                                                        args.Package.Id, 
                                                                        args.Package.Version));

            // delete and redeploy all installed packages, this will trigger the Installed event ^
            // this acts as a crude "remove/overwrite plugins that were in use when un/installed" hack
            addInManager.Restore();

            // create setings export provider
            SettingsExportProvider settingsExportProvider = new SettingsExportProvider(settings);

            // create container
            CompositionContainer container = new CompositionContainer(catalog, settingsExportProvider);

            // set up template resolver
            var lazyProviders = container.GetExports<IFileProvider>(ContractNames.TemplateProvider);
            var realProviders = lazyProviders.Select(lazy => lazy.Value);
            TemplateResolver templateResolver = new TemplateResolver(realProviders.ToArray());

            // load template
            Template template = new Template(container);
            template.Load(templateResolver, settings.GetValue<string>(Settings.Template));

            

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
            
            //XamlSettingsProvider settingsProvider = new XamlSettingsProvider(@"c:\temp\config.xml");
            //settingsProvider.SetValue("AStringArray", new[] {"abc", "def"});
            //settingsProvider.SetValue("AnArray", new[] { "abc", "def" });
            //settingsProvider.SetValue("AnInt", 123);
            //settingsProvider.SetValue("ABool", true);

            this.AddExport(batch, notifications);
            this.AddExport(batch, contentManager);
            this.AddExport(batch, addInManager);
            this.AddExport(batch, traceListener);
            this.AddExport(batch, container);
            this.AddExport(batch, settings);

            container.Compose(batch);

            // TODO replace this with MEF based injection
            // initialize app-singleton
            App.Initialize(container, contentManager, addInManager, notifications, traceListener);

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

            // TODO figure out if we actually need this
            // hook in our MEF based IHttpController instead of the default one
            //GlobalConfiguration.Configuration.Services.Replace(typeof(IHttpControllerTypeResolver), new AddInHttpControllerTypeResolver(App.Instance.Container));

        }

        private void AddExport<T>(CompositionBatch batch, T instance)
        {
            var metadata = new Dictionary<string, object>();
            metadata.Add("ExportTypeIdentity", AttributedModelServices.GetTypeIdentity(typeof(T)));
            batch.AddExport(new Export(AttributedModelServices.GetContractName(typeof(T)),
                                       metadata,
                                       () => instance));
        }

        private void UpdateWebApiRegistry(ComposablePartCatalogChangeEventArgs eventArg)
        {
            using (TraceSources.AddInManager.TraceActivity("Updating WebApi routes."))
            {
                // TODO this could be cleaned up a bit

                foreach (var partDefinition in eventArg.RemovedDefinitions)
                {
                    IApiControllerMetadata controllerMetadata;
                    if (!TryGetMetadata(partDefinition, out controllerMetadata))
                        continue;

                    Type controllerType = AddInModelServices.GetPartType(partDefinition).Value;

                    string routeName = CreateRouteName(controllerMetadata, controllerType);

                    TraceSources.AddInManager.TraceInformation("Removing route: {0} (Source: {1}, Version: {2})", 
                                                               routeName, 
                                                               controllerMetadata.PackageId, 
                                                               controllerMetadata.PackageVersion);

                    RouteBase route = RouteTable.Routes[routeName];
                    using (RouteTable.Routes.GetWriteLock())
                    {
                        RouteTable.Routes.Remove(route);
                    }
                }

                foreach (var partDefinition in eventArg.AddedDefinitions)
                {
                    IApiControllerMetadata controllerMetadata;
                    if (!TryGetMetadata(partDefinition, out controllerMetadata)) 
                        continue;

                    Type controllerType = AddInModelServices.GetPartType(partDefinition).Value;

                    // Pacakge version explicitly ignored in order to remain backwards compatible
                    string urlTemplate = string.Format("api/{0}/{1}/", 
                                                       controllerMetadata.PackageId, 
                                                       controllerMetadata.UrlFragment.Trim('/'));

                    string routeName = CreateRouteName(controllerMetadata, controllerType);

                    string controllerName = controllerType.Name.Substring(0, controllerType.Name.Length - "Controller".Length);

                    TraceSources.AddInManager.TraceInformation(
                        "Adding route: {0} (Template: '{1}', Controller: {2}, Source: {3}, Version: {4})", 
                        routeName, 
                        urlTemplate, 
                        controllerName, 
                        controllerMetadata.PackageId, 
                        controllerMetadata.PackageVersion);

                    using (RouteTable.Routes.GetWriteLock())
                    {
                        RouteTable.Routes.MapHttpRoute(
                            routeName, 
                            urlTemplate, 
                            new
                                {
                                    Controller = controllerName, 
                                });
                    }
                }
            }
        }

        private static string CreateRouteName(IApiControllerMetadata controllerMetadata, Type controllerType)
        {
            return string.Format("{0}_{1}", 
                                 controllerMetadata.PackageId, 
                                 controllerType.FullName.Replace('.', '_'));
        }

        private static bool TryGetMetadata(ComposablePartDefinition partDefinition, out IApiControllerMetadata controllerMetadata)
        {
            IEnumerable<ExportDefinition> exports = partDefinition.ExportDefinitions;
            var apiExport = exports.SingleOrDefault(export => StringComparer.Ordinal.Equals(export.ContractName,
                                                                                            Extensibility.ContractNames.ApiController));
            if (apiExport == null)
            {
                controllerMetadata = null;
                return false;
            }

            controllerMetadata = AttributedModelServices.GetMetadataView<IApiControllerMetadata>(apiExport.Metadata);
            return true;
        }
    }
}