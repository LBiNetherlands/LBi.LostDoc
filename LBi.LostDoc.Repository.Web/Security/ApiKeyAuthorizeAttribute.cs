/*
 * Copyright 2013 DigitasLBi Netherlands B.V.
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
using System.ComponentModel.Composition;
using System.Web;
using System.Web.Http;
using LBi.LostDoc.Repository.Web.Configuration;
using LBi.LostDoc.Repository.Web.Configuration.Composition;

namespace LBi.LostDoc.Repository.Web.Security
{
    public class ApiKeyAuthorizeAttribute : AuthorizeAttribute
    {
        [ImportSetting(Settings.ApiKey)]
        protected string ApiKey { get; set; }

        protected override bool IsAuthorized(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            var queryString = HttpUtility.ParseQueryString(actionContext.Request.RequestUri.Query);
            return StringComparer.OrdinalIgnoreCase.Equals(this.ApiKey, queryString["apiKey"]);
        }
    }
}