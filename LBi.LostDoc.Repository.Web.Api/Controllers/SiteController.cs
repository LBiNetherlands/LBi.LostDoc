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

using System;
using System.ComponentModel.Composition;
using System.Web.Http;
using LBi.LostDoc.Repository.Web.Security;

namespace LBi.LostDoc.Repository.Web.Api.Controllers
{
    [Export]
    public class SiteController : ApiController
    {
        [ImportingConstructor]
        public SiteController(ContentManager content)
        {
            this.Content = content;
        }

        protected ContentManager Content { get; set; }

        public string Get()
        {
            return this.Content.CurrentState.ToString();
        }

        [ApiKeyAuthorize]
        public bool Post()
        {
            try
            {
                this.Content.QueueRebuild(string.Empty);
                return true;
            } 
            catch (Exception)
            {
                return false;
            }
        }
    }
}
