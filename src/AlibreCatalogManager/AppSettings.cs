using System;
using System.IO;
using System.Runtime.Serialization;
using AlibreAddOnManager;

namespace AlibreCatalogManager
{
    /// <summary>
    /// %APPDATA%\AlibreCatalogManager\settings.json. One instance is shared by every
    /// tab, so one tab's save never overwrites another's values with stale ones.
    /// </summary>
    [DataContract]
    public sealed class AppSettings
    {
        private static string FilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AlibreCatalogManager", "settings.json");

        [DataMember(Name = "lastCatalog", EmitDefaultValue = false)]
        public string LastCatalog { get; set; }

        [DataMember(Name = "lastProject", EmitDefaultValue = false)]
        public string LastProject { get; set; }

        [DataMember(Name = "msBuildPath", EmitDefaultValue = false)]
        public string MsBuildPath { get; set; }

        public static AppSettings Load()
        {
            try
            {
                return File.Exists(FilePath) ? Json.ReadFile<AppSettings>(FilePath) : new AppSettings();
            }
            catch (Exception)
            {
                return new AppSettings();
            }
        }

        public void Save()
        {
            try
            {
                Json.WriteFile(FilePath, this);
            }
            catch (Exception)
            {
                // Settings are a convenience; never fail an operation over them.
            }
        }
    }
}
