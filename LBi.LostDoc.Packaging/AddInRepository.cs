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
using NuGet;

namespace LBi.LostDoc.Packaging
{
    public class AddInRepository
    {
        private readonly IPackageRepository _repository;
        private readonly AddInSource[] _sources;

        public AddInRepository(params AddInSource[] sources)
        {
            _sources = sources;

            List<PackageSource> packageSources = new List<PackageSource>();

            foreach (AddInSource source in sources)
            {
                PackageSource packageSource = new PackageSource(source.Source, 
                                                                source.Name, 
                                                                isEnabled: true);

                packageSources.Add(packageSource);
            }

            PackageSourceProvider packageSourceProvider = new PackageSourceProvider(new NullSettings(), packageSources);
            IPackageRepositoryFactory packageRepositoryFactory = new PackageRepositoryFactory();

            // TODO probably turn this off and report proper errors
            this._repository = packageSourceProvider.GetAggregate(packageRepositoryFactory, 
                                                                  ignoreFailingRepositories: true);
        }

        internal IPackageRepository NuGetRepository
        {
            get { return this._repository; }
        }

        public AddInSource[] Sources { get { return this._sources; } }

        public AddInPackage Get(string id, string version)
        {
            SemanticVersion ver = SemanticVersion.Parse(version);

            IPackage pkg = this._repository.FindPackage(id, ver);

            if (pkg == null)
                return null;

            return AddInPackage.Create(pkg);
        }

        public AddInPackage Get(string id, bool includePrerelease)
        {
            var packages =
                this._repository.GetPackages()
                    .Where(ip => StringComparer.Ordinal.Equals(id, ip.Id));

            if (includePrerelease)
                packages = packages.Where(p => p.IsAbsoluteLatestVersion);
            else
                packages = packages.Where(p => p.IsLatestVersion);

            IPackage pkg = packages.FirstOrDefault();

            if (pkg == null)
                return null;

            return AddInPackage.Create(pkg);
        }

        public AddInPackage GetUpdate(AddInPackage package, bool includePrerelease)
        {
            var pkg = this._repository.GetUpdates(new[] { package.NuGetPackage }, includePrerelease, true).FirstOrDefault();

            if (pkg == null)
                return null;

            return AddInPackage.Create(pkg);
        }

        public IEnumerable<AddInPackage> Search(string terms, bool includePrerelease, int offset, int count)
        {
            IQueryable<IPackage> packages;
            if (string.IsNullOrWhiteSpace(terms))
            {
                packages = this._repository.GetPackages();
                if (!includePrerelease)
                    packages = packages.Where(pkg => pkg.IsReleaseVersion());
            }
            else
                packages = this._repository.Search(terms, includePrerelease);

            return packages.Skip(offset)
                           .Take(count)
                           .Select(AddInPackage.Create);
        }
    }
}