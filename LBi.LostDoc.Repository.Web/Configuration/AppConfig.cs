/*
 * Copyright 2012 DigitasLBi Netherlands B.V.
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
using System.Collections.Specialized;
using System.IO;
using System.Web;

namespace LBi.LostDoc.Repository.Web.Configuration
{
    /// <summary>
    ///     Contains basic app settings.
    /// </summary>
    [Obsolete("Don't use this anymore.")]
    public static class AppConfig
    {
        private static readonly string _BasePath;

        /// <summary>
        ///     Initializes static members of the <see cref="AppConfig" /> class.
        /// </summary>
        static AppConfig()
        {
            _BasePath = HttpRuntime.AppDomainAppPath;
        }

        public static string AddInInstallPath
        {
            get { return AppSettings["LBi.LostDoc.Repository.Extensibility.InstallPath"]; }
        }

        public static string AddInPackagePath
        {
            get { return AppSettings["LBi.LostDoc.Repository.Extensibility.PackagePath"]; }
        }

        public static string AddInRepository
        {
            get { return AppSettings["LBi.LostDoc.Repository.Extensibility.Repository"]; }
        }

        /// <summary>
        ///     Gets the Api Key
        /// </summary>
        public static string ApiKey
        {
            get { return AppSettings["LBi.LostDoc.Repository.ApiKey"]; }
        }

        /// <summary>
        ///     Gets the path where the generated content will be stored.
        /// </summary>
        public static string ContentPath
        {
            get
            {
                return
                    Path.Combine(_BasePath, 
                                 AppSettings["LBi.LostDoc.Repository.ContentPath"]);
            }
        }

        public static string LogPath
        {
            get
            {
                return
                    Path.Combine(_BasePath, 
                                 AppSettings["LBi.LostDoc.Repository.LogPath"]);
            }
        }

        /// <summary>
        ///     Gets the path where all ldoc files are stored.
        /// </summary>
        public static string RepositoryPath
        {
            get
            {
                return
                    Path.Combine(_BasePath, 
                                 AppSettings["LBi.LostDoc.Repository.RepositoryPath"]);
            }
        }

        /// <summary>
        ///     Gets the path where temporary files and folders will be created.
        /// </summary>
        public static string TempPath
        {
            get
            {
                return
                    Path.Combine(_BasePath, 
                                 AppSettings["LBi.LostDoc.Repository.TempPath"]);
            }
        }

        public static string Template
        {
            get { return AppSettings["LBi.LostDoc.Repository.Template"]; }
        }

        private static NameValueCollection AppSettings
        {
            get { return System.Configuration.ConfigurationManager.AppSettings; }
        }
    }
}