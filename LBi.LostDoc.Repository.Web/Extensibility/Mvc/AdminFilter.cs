using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LBi.LostDoc.Repository.Web.Notifications;

namespace LBi.LostDoc.Repository.Web.Extensibility.Mvc
{
    public class AdminFilter : IActionFilter
    {
        public AdminFilter(NotificationManager notifications)
        {
            this.Notifications = notifications;
        }

        protected NotificationManager Notifications { get; set; }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            ViewResult viewResult = filterContext.Result as ViewResult;
            if (viewResult == null || viewResult.Model == null)
                return;

            HttpContextBase httpContext = filterContext.RequestContext.HttpContext;
            
            // this was populated by the AddInControllerFactory
            IControllerMetadata metadata = (IControllerMetadata)httpContext.Items[AddInControllerFactory.MetadataKey];

            if (metadata == null)
                return;

            IAdminModel model = viewResult.Model as IAdminModel;
            if (model == null)
                return;

            model.Notifications = this.Notifications.Get(filterContext.HttpContext.User).ToArray();


            var catalog = App.Instance.Container.Catalog;

            Navigation active;
            model.Navigation = BuildMenu(filterContext, catalog, out active);

            model.PageTitle = "LostDoc Administration - " + metadata.Text;

            if (active != null && !string.IsNullOrWhiteSpace(active.Text))
                model.PageTitle += string.Format(" - {0}", active.Text);
        }

        private static Navigation[] BuildMenu(ActionExecutedContext filterContext,
                                              ComposablePartCatalog catalog,
                                              out Navigation active)
        {
            active = null;
            List<Navigation> menu = new List<Navigation>();
            foreach (var exportInfo in catalog.FindExports<IControllerMetadata>(ContractNames.AdminController))
            {
                ReflectedControllerDescriptor descriptor = new ReflectedControllerDescriptor(exportInfo.Value);

                var controllerAttr =
                    descriptor.GetCustomAttributes(typeof(AdminControllerAttribute), true).FirstOrDefault()
                    as AdminControllerAttribute;

                if (controllerAttr == null)
                    continue;

                UrlHelper urlHelper = new UrlHelper(filterContext.RequestContext);
                Uri defaultTargetUrl = null;
                List<Navigation> children = new List<Navigation>();
                foreach (var actionDescriptor in descriptor.GetCanonicalActions())
                {
                    var actionAttr =
                        actionDescriptor.GetCustomAttributes(typeof(AdminActionAttribute), true).FirstOrDefault() as
                        AdminActionAttribute;
                    if (actionAttr == null)
                        continue;

                    // TODO replace anon class with concrete type?
                    string targetUrl = urlHelper.Action(actionAttr.Name,
                                                        controllerAttr.Name,
                                                        new
                                                            {
                                                                packageId = exportInfo.Metadata.PackageId,
                                                                packageVersion = exportInfo.Metadata.PackageVersion
                                                            });

                    Uri target = new Uri(targetUrl, UriKind.RelativeOrAbsolute);

                    if (defaultTargetUrl == null || actionAttr.IsDefault)
                        defaultTargetUrl = target;

                    bool isActive = filterContext.ActionDescriptor.ActionName == actionDescriptor.ActionName &&
                                    filterContext.ActionDescriptor.ControllerDescriptor.ControllerType ==
                                    descriptor.ControllerType;

                    Navigation navigation = new Navigation(null,
                                       actionAttr.Order,
                                       actionAttr.Text,
                                       target,
                                       isActive,
                                       Enumerable.Empty<Navigation>());

                    if (isActive)
                        active = navigation;



                    children.Add(navigation);
                }

                bool isAnyChildActive = children.Any(n => n.IsActive);

                // if there's only one child, ignore it
                if (children.Count == 1)
                    children.Clear();

                menu.Add(new Navigation(controllerAttr.Group,
                                        controllerAttr.Order,
                                        controllerAttr.Text,
                                        defaultTargetUrl,
                                        isAnyChildActive,
                                        children));
            }

            var navigations = menu.OrderBy(n => n.Order).ToArray();
            return navigations;
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
        }
    }
}