using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LBi.LostDoc.Composition;
using LBi.LostDoc.Repository.Web.Areas.Administration.Models;
using LBi.LostDoc.Repository.Web.Extensibility;

namespace LBi.LostDoc.Repository.Web.Areas.Administration
{
    public class AdminFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            ViewResult viewResult = filterContext.Result as ViewResult;
            if (viewResult != null)
            {
                IAdminModel model = viewResult.Model as IAdminModel;
                if (model == null)
                    throw new InvalidOperationException("Model has to inherit ModelBase");


                model.Notifications = App.Instance.Notifications.Get(filterContext.HttpContext.User);

                // this was populated by the AddInControllerFactory
                IControllerMetadata metadata = (IControllerMetadata)filterContext.RequestContext.HttpContext.Items[AddInControllerFactory.MetadataKey];


                
                // TODO FIX THIS SOMEHOW, this is pretty crappy
                var allControllerExports =
                    App.Instance.Container.GetExports(
                        new ContractBasedImportDefinition(
                            Extensibility.ContractNames.AdminController,
                            AttributedModelServices.GetTypeIdentity(typeof(IController)),
                            Enumerable.Empty<KeyValuePair<string, Type>>(), ImportCardinality.ZeroOrMore, false, false,
                            CreationPolicy.NonShared));

                foreach (Export export in allControllerExports)
                {
                    IControllerMetadata controllerMetadata = AttributedModelServices.GetMetadataView<IControllerMetadata>(export.Metadata);
                    ReflectedControllerDescriptor descriptor = new ReflectedControllerDescriptor(export.Value.GetType());

                    // create navigation object
                }
                // (create) and assign it to model.Navigation

                model.PageTitle = string.Format("LostDoc Administration {0} - {1}",
                                                metadata.Name,
                                                "action");
            }
        }
    }
}