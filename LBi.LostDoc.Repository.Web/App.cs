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
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Web;
using LBi.LostDoc.Packaging;
using LBi.LostDoc.Repository.Web.Notifications;
using LBi.LostDoc.Templating;

namespace LBi.LostDoc.Repository.Web
{
    public class App
    {
        private App(CompositionContainer container, ContentManager contentManager, AddInManager addInManager, NotificationManager notifications)
        {
            this.Container = container;
            this.Content = contentManager;
            this.AddIns = addInManager;
            this.Notifications = notifications;
        }

        public static App Instance { get; protected set; }

        public static void Initialize(CompositionContainer container, ContentManager contentManager, AddInManager addInManager, NotificationManager notifications)
        {
            Instance = new App(container, contentManager, addInManager, notifications);
        }

        public CompositionContainer Container { get; protected set; }
        public ContentManager Content { get; protected set; }
        public AddInManager AddIns { get; protected set; }
        public NotificationManager Notifications { get; protected set; }
        

    }
}