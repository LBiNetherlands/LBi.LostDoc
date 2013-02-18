/*
 * Copyright 2012 LBi Netherlands B.V.
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

using System.ComponentModel.Composition.Hosting;
using System.Web.Http;
using System.Web.Http.WebHost.Routing;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using LBi.LostDoc;
using LBi.LostDoc.Templating;
using LBi.LostDoc.Templating.FileProviders;

namespace LBi.LostDoc.Repository.Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapHttpRoute(
                                name: "Repository",
                                routeTemplate: "site/repository/{assembly}/{version}",
                                defaults: new
                                              {
                                                  controller = "Repository",
                                                  assembly = RouteParameter.Optional,
                                                  version = RouteParameter.Optional
                                              });


            routes.MapHttpRoute(
                                name: "Admin",
                                routeTemplate: "site/library",
                                defaults: new
                                              {
                                                  controller = "Library",
                                              });

            routes.MapHttpRoute(
                                name: "Rebuild",
                                routeTemplate: "site/rebuild",
                                defaults: new
                                              {
                                                  controller = "Site",
                                                  action = "Rebuild",
                                              });

            routes.MapHttpRoute(
                                name: "Status",
                                routeTemplate: "site/status",
                                defaults: new
                                              {
                                                  controller = "Site",
                                                  action = "GetStatus"
                                              });


            routes.MapHttpRoute(
                                name: "Search",
                                routeTemplate: "archive/{id}/search/{searchTerms}",
                                defaults: new
                                              {
                                                  controller = "Search",
                                                  action = "Get"
                                              });


            routes.MapHttpRoute(
                                name: "DefaultSearch",
                                routeTemplate: "library/search/{searchTerms}",
                                defaults: new
                                              {
                                                  controller = "Search",
                                                  id = "current",
                                                  action = "Get"
                                              });


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


            routes.Add("Redirect", new Route("", new RedirectRouteHandler("Library")));
        }

        protected void Application_Start()
        {
            AggregateCatalog catalog = new AggregateCatalog(new ApplicationCatalog());

            CompositionContainer container = new CompositionContainer(catalog);
            Template template = new Template(container);

            TemplateResolver resolver = new TemplateResolver(new ResourceFileProvider("LBi.LostDoc.Repository.Web.App_Data.Templates"));
            template.Load(resolver, AppConfig.Template);

            ContentManager.Initialize(new ContentSettings
                                          {
                                              ContentPath = AppConfig.ContentPath,
                                              IgnoreVersionComponent = VersionComponent.Patch,
                                              RepositoryPath = AppConfig.RepositoryPath,
                                              Template = template
                                          });

            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }

    }
}
