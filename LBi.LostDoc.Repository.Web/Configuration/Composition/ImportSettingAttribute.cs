using System;
using System.ComponentModel.Composition;

namespace LBi.LostDoc.Repository.Web.Configuration.Composition
{
    // TODO this doesn't work :( find a workaround
    // [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ImportSettingAttribute : ImportAttribute
    {
        public ImportSettingAttribute(string settingsKey)
            : base(SettingsConstants.SettingsContract)
        {
            this.SettingsKey = settingsKey;
        }

        public string SettingsKey { get; set; }
    }
}