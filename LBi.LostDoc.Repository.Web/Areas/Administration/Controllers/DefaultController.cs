using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LBi.LostDoc.Repository.Web.Extensibility;

namespace LBi.LostDoc.Repository.Web.Areas.Administration.Controllers
{
    // this is a bit of a hack to get this controller into the CompositionContainer 
    // so the AddInControllerFactory can instantiate it
    [Export(ContractNames.AdminController, typeof(IController))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("Name", "Default")]
    [ExportMetadata("Order", 0.0)]
    public class DefaultController : Controller
    {
        //
        // GET: /Administration/Default/

        [Import]
        public CompositionContainer Container { get; set; }

        public ActionResult Redirect()
        {
            var catalog = this.Container.Catalog;
            double lowestOrder = double.MaxValue;
            Uri redirectUri = null;
            foreach (var exportInfo in catalog.FindExports<IControllerMetadata>(ContractNames.AdminController))
            {
                ReflectedControllerDescriptor descriptor = new ReflectedControllerDescriptor(exportInfo.Value);

                var controllerAttr =
                    descriptor.GetCustomAttributes(typeof(AdminControllerAttribute), true).FirstOrDefault()
                    as AdminControllerAttribute;

                if (controllerAttr == null)
                    continue;

                if (controllerAttr.Order >= lowestOrder)
                    continue;                

                Uri defaultTargetUrl = null;

                foreach (var actionDescriptor in descriptor.GetCanonicalActions())
                {
                    var actionAttr =
                        actionDescriptor.GetCustomAttributes(typeof(AdminActionAttribute), true).FirstOrDefault() as
                        AdminActionAttribute;
                    if (actionAttr == null)
                        continue;

                    string targetUrl = Url.Action(actionAttr.Name,
                                                  controllerAttr.Name,
                                                  new
                                                      {
                                                          packageId = exportInfo.Metadata.PackageId,
                                                          packageVersion = exportInfo.Metadata.PackageVersion
                                                      });

                    Uri target = new Uri(targetUrl, UriKind.RelativeOrAbsolute);

                    if (defaultTargetUrl == null || actionAttr.IsDefault)
                        defaultTargetUrl = target;
                }

                if (defaultTargetUrl != null)
                    redirectUri = defaultTargetUrl;
            }

            return Redirect(redirectUri.ToString());
        }

    }
}
