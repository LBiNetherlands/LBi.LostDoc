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

        public AddInManager(AddInRepository repository, string installDirectory, string packageDirectory, string tempDirectory, string requiredPackagePath)
        {
            this.Repository = repository;
            this.InstallDirectory = installDirectory;
            this.PackageDirectory = packageDirectory;
            this.TempDirectory = tempDirectory;
            this.RequiredPackagesConfigPath = requiredPackagePath;

            // WORKAROUND NuGet 2.5 introduced a bug whereby it fails to locate a good temp path
            Environment.SetEnvironmentVariable("NuGetCachePath", tempDirectory);
            this._packageManager = new PackageManager(repository.NuGetRepository, packageDirectory);
        }

        public string RequiredPackagesConfigPath { get; set; }

        public string TempDirectory { get; protected set; }

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
                        PackageEquals(package, p)) != null;
        }

        private static bool PackageEquals(AddInPackage package, IPackage p)
        {
            return StringComparer.Ordinal.Equals(p.Id, package.Id) &&
                   StringComparer.Ordinal.Equals(p.Version.ToString(), package.Version);
        }

        public AddInPackage Get(string id, string version)
        {
            IPackage pkg;
            IQueryable<IPackage> installedPackages = this._packageManager.LocalRepository.GetPackages();

            if (version != null)
            {
                SemanticVersion ver = SemanticVersion.Parse(version);
                pkg = installedPackages.SingleOrDefault(ip => StringComparer.Ordinal.Equals(id, ip.Id) && ip.Version == ver);
            }
            else
            {
                pkg = installedPackages.SingleOrDefault(ip => StringComparer.Ordinal.Equals(id, ip.Id));
            }

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

        public PackageResult Restore()
        {
            PackageResult ret = PackageResult.Ok;

            foreach (var dir in Directory.EnumerateDirectories(this.InstallDirectory))
                Directory.Delete(dir, true);

            // force install of required packages

            PackageReferenceFile packageReferenceFile = new PackageReferenceFile(this.RequiredPackagesConfigPath);
            foreach (PackageReference packageReference in packageReferenceFile.GetPackageReferences(false))
            {
                // TODO this should take the packageReference.Version and packageReference.VersionConstraint into consideration
                AddInPackage requiredPackage = this.Get(packageReference.Id, null);

                // not installed
                if (requiredPackage == null)
                {
                    requiredPackage = this.Repository.Get(packageReference.Id, false);
                    if (requiredPackage == null)
                        throw new Exception("Could not find required add-in package: " + packageReference.Id);

                    this._packageManager.InstallPackage(requiredPackage.NuGetPackage, false, !requiredPackage.IsReleaseVersion);
                }
            }

            foreach (AddInPackage addInPackage in this)
            {
                if (this.DeployAddIn(addInPackage))
                {
                    this.OnInstalled(addInPackage, PackageResult.Ok);
                }
                else
                {
                    ret = PackageResult.PendingRestart;
                    this.OnInstalled(addInPackage, PackageResult.PendingRestart);
                }
            }

            return ret;
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
                    string sourcePath = Path.Combine(this._packageManager.PathResolver.GetInstallPath(package.NuGetPackage), assemblyReference.Path);
                    string targetPath = Path.Combine(targetdir, Path.GetFileName(assemblyReference.Path));

                    File.Copy(sourcePath, targetPath);
                }
            }
            else
                ret = false;

            return ret;
        }
    }
}