﻿/*
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Principal;

namespace LBi.LostDoc.Repository.Web.Notifications
{
    public class NotificationManager
    {
        private readonly ConcurrentDictionary<Guid, Notification> _notifications;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Notification>> _userNotifications;

        public NotificationManager()
        {
            this._notifications = new ConcurrentDictionary<Guid, Notification>();
            this._userNotifications =
                new ConcurrentDictionary<string, ConcurrentDictionary<Guid, Notification>>(StringComparer.Ordinal);
        }

        public void Add(Severity severity, Lifetime lifeTime, Scope scope, string title, string message, params NotificationAction[] actions)
        {
            if (scope == Scope.User)
                throw new ArgumentException("Scope 'User' is not allowed for this overload as no IPrincipal is provided.", "scope");

            this.Add(severity, lifeTime, scope, null, title, message, actions);
        }

        public void Add(Severity severity, 
                        Lifetime lifeTime, 
                        Scope scope, 
                        IPrincipal principal, 
                        string title, 
                        string message, 
                        params NotificationAction[] actions)
        {
            if (scope == Scope.User && principal == null)
                throw new ArgumentNullException("principal", "Argument cannot be null when Scope is 'User'.");

            var note = new Notification(Guid.NewGuid(), 
                                        DateTime.UtcNow, 
                                        severity, 
                                        lifeTime, 
                                        scope, 
                                        title, 
                                        message, 
                                        actions);
            if (scope == Scope.User)
            {
                var userNotifications = this._userNotifications.GetOrAdd(principal.Identity.Name, 
                                                                         s => new ConcurrentDictionary<Guid, Notification>());

                if (!userNotifications.TryAdd(note.Id, note))
                    throw new ReadOnlyException("Failed to add notifications.");
            }
            else
            {
                if (!this._notifications.TryAdd(note.Id, note))
                    throw new ReadOnlyException("Failed to add notifications.");
            }
        }

        public IEnumerable<Notification> Get(IPrincipal principal)
        {
            IEnumerable<Notification> ret;
            ConcurrentDictionary<Guid, Notification> userNotifications;

            // TODO somwhere we need to filter for the right scope
            if (this._userNotifications.TryGetValue(principal.Identity.Name, out userNotifications))
                ret = userNotifications.Values;
            else
                ret = Enumerable.Empty<Notification>();

            foreach (var note in ret.Concat(this._notifications.Values))
            {
                if (note.LifeTime == Lifetime.Page &&
                    note.Scope == Scope.User &&
                    this._userNotifications.TryGetValue(principal.Identity.Name, out userNotifications))
                {
                    Notification _;
                    userNotifications.TryRemove(note.Id, out _);
                }

                yield return note;
            }
        }

        public void Remove(Guid id)
        {
            Notification oldVal;
            this._notifications.TryRemove(id, out oldVal);
        }
    }
}