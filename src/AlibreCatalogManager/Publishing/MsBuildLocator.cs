using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AlibreCatalogManager.Publishing
{
    /// <summary>
    /// Finds MSBuild.exe candidates: every Visual Studio / Build Tools instance
    /// vswhere knows about, then the copy bundled with JetBrains Rider.
    /// </summary>
    public static class MsBuildLocator
    {
        public static List<string> Find()
        {
            var found = new List<string>();
            found.AddRange(FromVsWhere());
            found.AddRange(FromRider());
            return found.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static IEnumerable<string> FromVsWhere()
        {
            var vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                @"Microsoft Visual Studio\Installer\vswhere.exe");
            if (!File.Exists(vswhere)) return Enumerable.Empty<string>();

            try
            {
                var start = new ProcessStartInfo(vswhere,
                    @"-products * -requires Microsoft.Component.MSBuild -sort -find MSBuild\**\Bin\MSBuild.exe")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var process = Process.Start(start))
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(10000);
                    return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();
                }
            }
            catch (Exception)
            {
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>Rider installed standalone (%LOCALAPPDATA%\Programs) or machine-wide (Program Files\JetBrains).</summary>
        private static IEnumerable<string> FromRider()
        {
            var roots = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "JetBrains")
            };

            foreach (var root in roots.Where(Directory.Exists))
            {
                string[] riders;
                try
                {
                    riders = Directory.GetDirectories(root, "*Rider*");
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (var rider in riders.OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase))
                {
                    yield return Path.Combine(rider, @"tools\MSBuild\Current\Bin\amd64\MSBuild.exe");
                    yield return Path.Combine(rider, @"tools\MSBuild\Current\Bin\MSBuild.exe");
                }
            }
        }
    }
}
