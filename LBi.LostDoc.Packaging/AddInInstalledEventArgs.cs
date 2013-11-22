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

namespace LBi.LostDoc.Packaging
{
    public class AddInInstalledEventArgs : EventArgs
    {
        public AddInInstalledEventArgs(AddInPackage package, PackageResult result, string installationPath, string packagePath)
        {
            this.Package = package;
            this.Result = result;
            this.InstallationPath = installationPath;
            this.PackagePath = packagePath;
        }

        public PackageResult Result { get; protected set; }

        public AddInPackage Package { get; protected set; }

        public string InstallationPath { get; protected set; }

        public string PackagePath { get; protected set; }
    }
}