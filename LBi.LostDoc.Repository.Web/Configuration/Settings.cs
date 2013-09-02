using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LBi.LostDoc.Repository.Web.Configuration
{
    public static class Settings
    {
        // Paths
        public const string ContentPath = "LostDoc.Repository.ContentPath";
        public const string TempPath = "LostDoc.Repository.TempPath";
        public const string LogPath = "LostDoc.Repository.LogPath";
        public const string RepositoryPath = "LostDoc.Repository.RepositoryPath";

        // Template settings
        public const string IgnoreVersionComponent = "LostDoc.Repository.IgnoreVersionComponent";
        public const string Template = "LostDoc.Repository.Template";
        public const string TemplateParameters = "LostDoc.Repository.Template.Parameters";

        // Security
        public const string ApiKey = "LostDoc.Repository.ApiKey";

        // Add-ins
        public const string AddInRepository = "LostDoc.Repository.Extensibility.Repository";
        public const string AddInPackagePath = "LostDoc.Repository.Extensibility.PackagePath";
        public const string AddInInstallPath = "LostDoc.Repository.Extensibility.InstallPath";
        public const string LocalRepositoryFolder = "LostDoc.Repository.Extensibility.LocalRepositoryFolder";
        public const string RequiredPackageConfigPath = "LostDoc.Repository.Extensibility.RequiredPackageConfigPath";
    }
}
