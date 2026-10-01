using System;
using System.IO;
using AlibreAddOnManager.Models;

namespace AlibreAddOnManager
{
    /// <summary>
    /// Per-user settings in %APPDATA%\AlibreAddOnManager\settings.json. Not
    /// Properties.Settings: in a hosted DLL that would write into Alibre's own
    /// user.config.
    /// </summary>
    public static class SettingsStore
    {
        public static ManagerSettings Load()
        {
            try
            {
                if (File.Exists(ManagerPaths.SettingsFile))
                    return Json.ReadFile<ManagerSettings>(ManagerPaths.SettingsFile) ?? new ManagerSettings();
            }
            catch (Exception)
            {
                // Corrupt settings: fall back to defaults.
            }

            return new ManagerSettings();
        }

        public static void Save(ManagerSettings settings)
        {
            Json.WriteFile(ManagerPaths.SettingsFile, settings);
        }
    }
}
