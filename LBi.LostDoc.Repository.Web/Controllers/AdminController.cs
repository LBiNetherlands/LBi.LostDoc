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

using System.Json;
using System.Web.Mvc;

namespace LBi.LostDoc.Repository.Web.Controllers
{
    public class AdminController : Controller
    {
        [HttpGet]
        public ActionResult Login()
        {
            return this.View();
        }

        [HttpGet]
        public ActionResult History()
        {
            return this.View();
        }

        [HttpGet]
        public ActionResult Content()
        {
            return this.View();
        }

        [HttpGet]
        public ActionResult Status()
        {
            return this.View();
        }

        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            return this.Json(new JsonPrimitive(true));
        }

        [HttpGet]
        public ActionResult Logout()
        {
            return this.View();
        }
    }
}
