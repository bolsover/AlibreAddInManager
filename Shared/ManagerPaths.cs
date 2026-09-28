using System;
using System.IO;

namespace AlibreAddInManager
{
    public static class ManagerPaths
    {
        /// <summary>The add-on manager's own .adc Identifier.</summary>
        public const string SelfId = "{925B370F-9858-4416-A7D9-1DD8FAAC16A0}";

        public const string SelfFolderName = "AlibreAddInManager";

        public const string HelperExeName = "AlibreAddInManager.Helper.exe";

        public const string AlibreProcessName = "Alibre Design";

        /// <summary>Where this tool installs add-ons. Other add-ons may live anywhere.</summary>
        public static string AddOnsRoot =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Alibre AddOns");

        public static string StateFile => Path.Combine(AddOnsRoot, ".manager", "state.json");

        private static string LocalRoot =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlibreAddInManager");

        public static string CacheDirectory => Path.Combine(LocalRoot, "cache");

        public static string PlanDirectory => Path.Combine(LocalRoot, "plans");

        public static string SettingsFile =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AlibreAddInManager", "settings.json");

        /// <summary>64-bit Program Files, even if the calling process happens to be 32-bit.</summary>
        public static string ProgramFiles64 =>
            Environment.GetEnvironmentVariable("ProgramW6432") ??
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        public static bool IsSameOrUnder(string path, string root)
        {
            var full = Path.GetFullPath(path).TrimEnd('\\') + "\\";
            var fullRoot = Path.GetFullPath(root).TrimEnd('\\') + "\\";
            return full.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>A folder name must be a single, plain path segment.</summary>
        public static bool IsValidFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (name == "." || name == ".." || name.StartsWith(".")) return false;
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
            return name.Trim() == name;
        }

        public static string NormalizeId(string id)
        {
            if (Guid.TryParse(id, out var guid))
                return guid.ToString("B").ToUpperInvariant();
            return id?.Trim();
        }

        public static bool SameId(string a, string b)
        {
            return string.Equals(NormalizeId(a), NormalizeId(b), StringComparison.OrdinalIgnoreCase);
        }
    }
}
