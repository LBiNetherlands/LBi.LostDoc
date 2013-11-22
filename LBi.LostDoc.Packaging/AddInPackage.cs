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
using NuGet;

namespace LBi.LostDoc.Packaging
{
    public class AddInPackage
    {
        // TODO clean this up

        #region Constructors and Destructors

        internal AddInPackage(IPackage pkg, 
                              string id, 
                              bool isReleaseVersion, 
                              DateTimeOffset? published, 
                              Uri iconUrl, 
                              string title, 
                              string summary, 
                              string description, 
                              Uri projectUrl)
        {
            this.NuGetPackage = pkg;
            this.Id = id;

            // TODO this can't be right
            this.Version = pkg.Version.Version.ToString();
            if (!string.IsNullOrWhiteSpace(pkg.Version.SpecialVersion))
                this.Version += '-' + pkg.Version.SpecialVersion;

            this.IsReleaseVersion = isReleaseVersion;
            this.Published = published;
            this.Title = title;
            this.Summary = summary;
            this.Description = description;
            this.ProjectUrl = projectUrl;
            this.IconUrl = iconUrl;
            this.LicenseUrl = pkg.LicenseUrl;
        }

        #endregion

        #region Public Properties

        public string Description { get; protected set; }

        public Uri IconUrl { get; protected set; }

        public string Id { get; protected set; }

        public bool IsReleaseVersion { get; protected set; }

        public Uri LicenseUrl { get; set; }

        public Uri ProjectUrl { get; protected set; }

        public DateTimeOffset? Published { get; protected set; }

        public string Summary { get; protected set; }

        public string Title { get; protected set; }

        public string Version { get; protected set; }

        #endregion

        #region Properties

        internal IPackage NuGetPackage { get; set; }

        #endregion

        #region Public Methods and Operators

        public override string ToString()
        {
            return string.Format("{0}.{1}", this.Id, this.Version);
        }

        #endregion

        #region Methods

        internal static AddInPackage Create(IPackage pkg)
        {
            return new AddInPackage(pkg, 
                                    pkg.Id, 
                                    pkg.IsReleaseVersion(), 
                                    pkg.Published, 
                                    pkg.IconUrl, 
                                    pkg.Title, 
                                    pkg.Summary, 
                                    pkg.Description, 
                                    pkg.ProjectUrl);
        }

        #endregion
    }
}