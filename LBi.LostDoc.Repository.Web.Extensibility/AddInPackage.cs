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
using NuGet;

namespace LBi.LostDoc.Repository.Web.Extensibility
{
    public class AddInPackage
    {
        public AddInPackage(string id, Version version, bool isReleaseVersion, DateTimeOffset? published, Uri iconUrl, string title, string summary, string description, Uri projectUrl)
        {
            this.Id = id;
            this.Version = version;
            this.IsReleaseVersion = isReleaseVersion;
            this.Published = published;
            this.Title = title;
            this.Summary = summary;
            this.Description = description;
            this.ProjectUrl = projectUrl;
            this.IconUrl = iconUrl;
        }

        internal static AddInPackage Create(IPackage pkg)
        {
            return new AddInPackage(pkg.Id, pkg.Version.Version, pkg.IsReleaseVersion(), pkg.Published, pkg.IconUrl, pkg.Title, pkg.Summary, pkg.Description, pkg.ProjectUrl);
        }

        public DateTimeOffset? Published { get; protected set; }

        public Uri IconUrl { get; protected set; }

        public Uri ProjectUrl { get; protected set; }

        public string Description { get; protected set; }

        public string Summary { get; protected set; }

        public string Title { get; protected set; }

        public bool IsReleaseVersion { get; protected set; }

        public Version Version { get; protected set; }

        public string Id { get; protected set; }
    }
}