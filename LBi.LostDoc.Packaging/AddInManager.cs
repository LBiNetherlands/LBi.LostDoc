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

namespace LBi.LostDoc.Packaging
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

        public event EventHandler<AddInInstalledEventArgs> Installed;

        public event EventHandler<AddInInstalledEventArgs> Uninstalled;

        public string InstallDirectory { get; protected set; }

        public string PackageDirectory { get; protected set; }

        public AddInRepository Repository { get; protected set; }

        public bool Contains(AddInPackage package)
        {
            return
                this._packageManager.LocalRepository.GetPackages()
                    .SingleOrDefault(
                        p =>
                        StringComparer.Ordinal.Equals(p.Id, package.Id) &&
                        StringComparer.Ordinal.Equals(p.Version.Version, package.Version)) != null;
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

        public IEnumerator<AddInPackage> GetEnumerator()
        {
            return this._packageManager.LocalRepository.GetPackages()
                       .Select(AddInPackage.Create)
                       .GetEnumerator();
        }

        public PackageResult Install(AddInPackage package)
        {
            // TODO fix the deps & prerelease hack
            if (this.Contains(package))
                throw new InvalidOperationException("Package already installed: " + package.ToString());
            this._packageManager.InstallPackage(package.NuGetPackage, true, !package.IsReleaseVersion);
            PackageResult ret;
            if (this.DeployAddIn(package))
                ret = PackageResult.Ok;
            else
                ret = PackageResult.PendingRestart;

            this.OnInstalled(package, ret);
            return ret;
        }

        public void Restore()
        {
            foreach (var dir in Directory.EnumerateDirectories(this.InstallDirectory))
                Directory.Delete(dir, true);

            foreach (AddInPackage addInPackage in this)
                this.DeployAddIn(addInPackage);
        }

        public PackageResult Uninstall(AddInPackage package)
        {
            // TODO fix the force & deps hack
            this._packageManager.UninstallPackage(package.NuGetPackage, true, false);
            PackageResult ret = PackageResult.PendingRestart;
            this.OnUninstalled(package, ret);
            return ret;
        }

        public PackageResult Update(AddInPackage package)
        {
            PackageResult ret = PackageResult.PendingRestart;

            // TODO fix the deps & prerelease hack
            this._packageManager.UpdatePackage(package.NuGetPackage, true, !package.IsReleaseVersion);
            return ret;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        protected virtual void OnInstalled(AddInPackage package, PackageResult result)
        {
            var handler = this.Installed;
            if (handler != null)
            {
                string packageDirectory = this._packageManager.PathResolver.GetPackageDirectory(package.NuGetPackage);
                var installationPath = Path.GetFullPath(Path.Combine(this.InstallDirectory, packageDirectory));
                var packagePath =
                    Path.GetFullPath(this._packageManager.PathResolver.GetInstallPath(package.NuGetPackage));

                handler(this, 
                        new AddInInstalledEventArgs(package, 
                                                    result, 
                                                    installationPath, 
                                                    packagePath));
            }
        }

        protected virtual void OnUninstalled(AddInPackage package, PackageResult result)
        {
            var handler = this.Uninstalled;
            if (handler != null)
            {
                string packageDirectory = this._packageManager.PathResolver.GetPackageDirectory(package.NuGetPackage);
                var installationPath = Path.GetFullPath(Path.Combine(this.InstallDirectory, packageDirectory));

                handler(this, 
                        new AddInInstalledEventArgs(package, 
                                                    result, 
                                                    installationPath, 
                                                    null));
            }
        }

        private bool DeployAddIn(AddInPackage package)
        {
            bool ret = true;
            string packageDirectory = this._packageManager.PathResolver.GetPackageDirectory(package.NuGetPackage);
            string targetdir = Path.Combine(Path.GetFullPath(this.InstallDirectory), packageDirectory);

            if (!Directory.Exists(targetdir))
            {
                Directory.CreateDirectory(targetdir);
                foreach (var assemblyReference in package.NuGetPackage.AssemblyReferences)
                {
                    string sourcePath =
                        Path.Combine(this._packageManager.PathResolver.GetInstallPath(package.NuGetPackage), 
                                     assemblyReference.Path);

                    File.Copy(sourcePath, 
                              Path.Combine(targetdir, assemblyReference.EffectivePath));
                }
            }
            else
                ret = false;

            return ret;
        }
    }
}