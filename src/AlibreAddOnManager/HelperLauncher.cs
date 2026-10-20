using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AlibreAddOnManager.Models;

namespace AlibreAddOnManager
{
    /// <summary>
    /// Writes the plan and starts the elevated helper (one UAC prompt per batch).
    /// The helper is copied to %TEMP% first so it never runs from — and locks —
    /// the manager's own folder, which a self-update needs to replace.
    /// </summary>
    public static class HelperLauncher
    {
        private const int ErrorCancelled = 1223; // user declined the UAC prompt

        /// <summary>Returns the started helper, or null if the user declined elevation.</summary>
        public static Process Launch(InstallPlan plan, out string planPath)
        {
            plan.Created = DateTime.UtcNow.ToString("o");
            Directory.CreateDirectory(ManagerPaths.PlanDirectory);
            planPath = Path.Combine(ManagerPaths.PlanDirectory, "plan-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
            Json.WriteFile(planPath, plan);

            var sourceDirectory = Path.GetDirectoryName(typeof(HelperLauncher).Assembly.Location);
            foreach (var name in ManagerPaths.HelperRequiredFiles)
            {
                var required = Path.Combine(sourceDirectory, name);
                if (!File.Exists(required))
                    throw new FileNotFoundException("The elevated helper is incomplete: " + name + " is missing from the add-on folder.", required);
            }

            var helperRoot = Path.Combine(Path.GetTempPath(), "AlibreAddOnManager");
            DeleteStaleCopies(helperRoot);
            var tempDirectory = Path.Combine(helperRoot, "helper-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            // The helper loads its dependencies from its own folder, so they travel with it.
            foreach (var name in ManagerPaths.HelperRequiredFiles.Concat(ManagerPaths.HelperOptionalFiles))
            {
                var source = Path.Combine(sourceDirectory, name);
                if (File.Exists(source))
                    File.Copy(source, Path.Combine(tempDirectory, name));
            }

            var helperExe = Path.Combine(tempDirectory, ManagerPaths.HelperExeName);

            try
            {
                return Process.Start(new ProcessStartInfo(helperExe, "--apply \"" + planPath + "\"")
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = tempDirectory
                });
            }
            catch (Win32Exception e) when (e.NativeErrorCode == ErrorCancelled)
            {
                TryDeleteDirectory(tempDirectory);
                return null;
            }
        }

        /// <summary>
        /// Removes copies left by earlier runs. A helper that is still running
        /// (e.g. waiting for Alibre to close) has its EXE locked, so its folder
        /// survives and is collected next time.
        /// </summary>
        private static void DeleteStaleCopies(string helperRoot)
        {
            if (!Directory.Exists(helperRoot)) return;
            foreach (var directory in Directory.GetDirectories(helperRoot, "helper-*"))
                TryDeleteDirectory(directory);
        }

        private static void TryDeleteDirectory(string path)
        {
            try { Directory.Delete(path, true); }
            catch (Exception) { /* in use or already gone */ }
        }
    }
}
