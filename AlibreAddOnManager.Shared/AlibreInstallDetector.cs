using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AlibreAddOnManager
{
    /// <summary>
    /// Finds installed Alibre versions from the file system — deliberately not via
    /// COM, whose registration only ever points at one side-by-side version.
    /// </summary>
    public static class AlibreInstallDetector
    {
        public static List<AlibreInstall> Find()
        {
            var result = new List<AlibreInstall>();
            var programFiles = ManagerPaths.ProgramFiles64;
            if (!Directory.Exists(programFiles)) return result;

            foreach (var folder in Directory.GetDirectories(programFiles, "Alibre Design*"))
            {
                var exe = Path.Combine(folder, "Program", "Alibre Design.exe");
                if (!File.Exists(exe)) continue;
                var version = FileVersionInfo.GetVersionInfo(exe).FileVersion ?? string.Empty;
                result.Add(new AlibreInstall
                {
                    Folder = folder,
                    ExePath = exe,
                    Version = version,
                    Build = ParseBuild(version)
                });
            }

            return result.OrderBy(i => i.Build).ToList();
        }

        /// <summary>
        /// Build number from IADRoot.Version or a file version: the last
        /// comma- or dot-separated numeric field ("PRODUCTVERSION 29,1,0,29126" → 29126).
        /// Returns 0 if it cannot be parsed.
        /// </summary>
        public static int ParseBuild(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return 0;
            var fields = version.Split(new[] { ',', '.', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = fields.Length - 1; i >= 0; i--)
                if (int.TryParse(fields[i].Trim(), out var build))
                    return build;
            return 0;
        }
    }
}
