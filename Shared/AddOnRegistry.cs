using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace AlibreAddInManager
{
    /// <summary>
    /// HKLM\SOFTWARE\Alibre Design Add-Ons: value name = .adc Identifier GUID,
    /// value data = folder containing the .adc. Always uses the 64-bit registry
    /// view — a 32-bit process would otherwise be silently redirected to
    /// WOW6432Node, where 64-bit Alibre never looks.
    /// </summary>
    public static class AddOnRegistry
    {
        public const string KeyPath = @"SOFTWARE\Alibre Design Add-Ons";

        /// <summary>Registered add-ons, value name as written (not normalized).</summary>
        public static Dictionary<string, string> ReadAll()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var key = hklm.OpenSubKey(KeyPath, false))
            {
                if (key == null) return result;
                foreach (var name in key.GetValueNames())
                {
                    if (string.IsNullOrEmpty(name)) continue; // the (Default) value
                    result[name] = key.GetValue(name) as string ?? string.Empty;
                }
            }

            return result;
        }

        /// <summary>Finds the value name for an id, tolerating case/format differences.</summary>
        public static string FindValueName(IDictionary<string, string> registered, string id)
        {
            foreach (var name in registered.Keys)
                if (ManagerPaths.SameId(name, id))
                    return name;
            return null;
        }

        /// <summary>Requires administrator rights.</summary>
        public static void SetValue(string id, string folder)
        {
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var key = hklm.CreateSubKey(KeyPath))
            {
                if (key == null) throw new InvalidOperationException("Could not open or create HKLM\\" + KeyPath);
                key.SetValue(id, folder, RegistryValueKind.String);
            }
        }

        /// <summary>Requires administrator rights. Returns false if the value was not present.</summary>
        public static bool DeleteValue(string id)
        {
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var key = hklm.OpenSubKey(KeyPath, true))
            {
                if (key == null) return false;
                var name = FindValueName(ReadAll(), id);
                if (name == null) return false;
                key.DeleteValue(name, false);
                return true;
            }
        }
    }
}
