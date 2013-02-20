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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet;

namespace LBi.LostDoc.Repository.Web.Extensibility
{
    public class AddInManager : IEnumerable<AddInPackage>
    {
        private readonly PackageManager _packageManager;

        public AddInManager(AddInRepository repository, string installDirectory, string packageDirectory)
        {
            this.Repository = repository;
            this.InstallDirectory = installDirectory;
            this.PackageDirectory = packageDirectory;
             
            this._packageManager = new PackageManager(repository.NuGetRepository, packageDirectory);
        }


        public void Restore()
        {
            foreach (var dir in Directory.EnumerateDirectories(this.InstallDirectory))
                Directory.Delete(dir, true);

            foreach (AddInPackage addInPackage in this)
                DeployAddIn(addInPackage);
        }

        public string PackageDirectory { get; protected set; }
        public string InstallDirectory { get; protected set; }
        public AddInRepository Repository { get; protected set; }

        public void Install(AddInPackage package)
        {
            // TODO fix the deps & prerelease hack
            this._packageManager.InstallPackage(package.NuGetPackage, true, !package.IsReleaseVersion);
        }

        private void DeployAddIn(AddInPackage package)
        {
            foreach (var assemblyReference in package.NuGetPackage.AssemblyReferences)
            {
                string sourcePath = Path.Combine(this._packageManager.PathResolver.GetInstallPath(package.NuGetPackage), assemblyReference.Path);

                File.Copy(sourcePath,
                          Path.Combine(this.InstallDirectory,
                                       this._packageManager.PathResolver.GetPackageDirectory(package.NuGetPackage),
                                       assemblyReference.EffectivePath));
            }
        }

        public void Uninstall(AddInPackage package)
        {
            // TODO fix the force & deps hack
            this._packageManager.UninstallPackage(package.NuGetPackage, true, false);
        }

        public IEnumerator<AddInPackage> GetEnumerator()
        {
            return this._packageManager.LocalRepository.GetPackages()
                                                       .Select(AddInPackage.Create)
                                                       .GetEnumerator();
        }

        public void Update(AddInPackage package)
        {
            // TODO fix the deps & prerelease hack
            this._packageManager.UpdatePackage(package.NuGetPackage, true, !package.IsReleaseVersion);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        public AddInPackage Get(string id, string version)
        {
            SemanticVersion ver = SemanticVersion.Parse(version);
            IPackage pkg =
                this._packageManager.LocalRepository.GetPackages()
                    .SingleOrDefault(ip => StringComparer.Ordinal.Equals(id, ip.Id) && ip.Version == ver);

            if (pkg == null)
                return null;

            return AddInPackage.Create(pkg);
        }
    }
}
