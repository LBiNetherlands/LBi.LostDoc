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

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet;

namespace LBi.LostDoc.Repository.Web.Extensibility
{
    public class AddInRepository
    {
        private readonly IPackageRepository _repository;

        public AddInRepository(params AddInSource[] sources)
        {
            List<PackageSource> packageSources = new List<PackageSource>();

            foreach (AddInSource source in sources)
            {
                PackageSource packageSource = new PackageSource(source.Source,
                                                                        source.Name,
                                                                        isEnabled: true,
                                                                        isOfficial: source.IsOfficial);

                packageSources.Add(packageSource);
            }

            PackageSourceProvider packageSourceProvider = new PackageSourceProvider(new NullSettings(), packageSources);
            IPackageRepositoryFactory packageRepositoryFactory = new PackageRepositoryFactory();
            
            // TODO probably turn this off and report proper errors
            this._repository = packageSourceProvider.GetAggregate(packageRepositoryFactory, ignoreFailingRepositories: true);
        }

        public IEnumerable<AddInPackage> Search(string terms, bool includePrerelease, int offset, int count)
        {
            var packages = this._repository.Search(terms, includePrerelease);
            return packages.Skip(offset)
                           .Take(count)
                           .Select(this.ConvertPackage);
        }

        private AddInPackage ConvertPackage(IPackage pkg)
        {
            return new AddInPackage(pkg.Id, pkg.Version.Version, pkg.IsReleaseVersion(), pkg.IconUrl, pkg.Title, pkg.Summary, pkg.Description, pkg.ProjectUrl);
        }
    }
}
