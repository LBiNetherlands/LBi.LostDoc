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
using System.Linq;
using System.Web.Mvc;

namespace LBi.LostDoc.Repository.Web.Extensibility
{
    public class AddInActionInvoker : ControllerActionInvoker
    {
        protected override ActionDescriptor FindAction(ControllerContext controllerContext, ControllerDescriptor controllerDescriptor, string actionName)
        {
            ActionDescriptor[] actionDescriptors = controllerDescriptor.GetCanonicalActions();
            for (int i = 0; i < actionDescriptors.Length; i++)
            {
                var attr = actionDescriptors[i].GetCustomAttributes(typeof(AdminActionAttribute), true).FirstOrDefault() as AdminActionAttribute;
                if (attr != null)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(actionName, attr.Name))
                        return actionDescriptors[i];
                    
                    if (StringComparer.OrdinalIgnoreCase.Equals("Index", actionName) && attr.IsDefault)
                    {
                        // reset the name, otherwise the View wont be picked up correctly
                        controllerContext.RouteData.Values["Action"] = attr.Name;
                        return actionDescriptors[i];
                    }
                }
            }

            return base.FindAction(controllerContext, controllerDescriptor, actionName);
        }
    }
}