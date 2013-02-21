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
using System.Linq;

namespace LBi.LostDoc.Repository.Web.Notifications
{
    public class Notification
    {
        public Notification(Guid id, DateTime created, Severity type, Lifetime lifetime, Scope scope, string title, string message, IEnumerable<NotificationAction> actions)
        {
            this.Id = id;
            this.Created = created;
            this.Type = type;
            this.LifeTime = lifetime;
            this.Scope = scope;
            this.Title = title;
            this.Message = message;
            this.Actions = actions.ToArray();

            if (lifetime == Lifetime.Page && scope != Scope.User)
                throw new InvalidOperationException("Scope has to be User when lifetime is set to Page");
        }

        public DateTime Created { get; protected set; }
        public Scope Scope { get; protected set; }
        public Severity Type { get; protected set; }
        public Guid Id { get; protected set; }
        public Lifetime LifeTime { get; protected set; }
        public string Title { get; protected set; }
        public string Message { get; protected set; }
        public NotificationAction[] Actions { get; protected set; }
    }
}