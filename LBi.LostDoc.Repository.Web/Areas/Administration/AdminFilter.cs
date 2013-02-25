using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
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
            }
        }
    }
}