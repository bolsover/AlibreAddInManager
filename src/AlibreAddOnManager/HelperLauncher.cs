using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
            var sourceExe = Path.Combine(sourceDirectory, ManagerPaths.HelperExeName);
            if (!File.Exists(sourceExe))
                throw new FileNotFoundException("The elevated helper is missing from the add-on folder.", sourceExe);

            var tempDirectory = Path.Combine(Path.GetTempPath(), "AlibreAddOnManager", "helper-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
            var helperExe = Path.Combine(tempDirectory, ManagerPaths.HelperExeName);
            File.Copy(sourceExe, helperExe);
            var config = sourceExe + ".config";
            if (File.Exists(config))
                File.Copy(config, helperExe + ".config");

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
                return null;
            }
        }
    }
}
