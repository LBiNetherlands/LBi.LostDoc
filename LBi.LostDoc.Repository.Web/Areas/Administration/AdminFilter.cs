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

                // TODO FIX THIS SOMEHOW

                //Type controllerType = filterContext.Controller.GetType();

                //// this wont be super fast, but it will be good enough for the admin system (and I'm lazy)
                //MetadataContractBuilder<IController, IControllerMetadata> contractBuilder =
                //    new MetadataContractBuilder<IController, IControllerMetadata>(ImportCardinality.ZeroOrMore,
                //                                                                  CreationPolicy.NonShared);

                //contractBuilder.Add((m, c) => StringComparer.OrdinalIgnoreCase.Equals(m.Name, c.Name));

                //var allControllerExports = App.Instance.Container.GetExports(contractBuilder.WithValue(c => c.Name,
                //                                                            AdminAttributeServices.GetName(controllerType)));

                ////foreach (Export allControllerExport in allControllerExports)
                ////{
                ////    IControllerMetadata controllerMetadata = AttributedModelServices.GetMetadataView<IControllerMetadata>(allControllerExport.Metadata);

                ////    allControllerExport.Value
                ////}


                model.PageTitle = string.Format("LostDoc Administration {0} - {1}",
                                                metadata.Name,
                                                "action");
            }
        }
    }
}