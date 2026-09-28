using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using AlibreAddInManager.Models;

namespace AlibreAddInManager.Helper
{
    /// <summary>
    /// Elevated helper for the Alibre add-on manager.
    /// Usage: AlibreAddInManager.Helper.exe --apply "&lt;plan.json&gt;"
    /// The add-on copies this EXE to %TEMP% before launching it, so it can also
    /// replace the manager's own folder during a self-update.
    /// </summary>
    internal static class Program
    {
        private const string Title = "Alibre Add-On Manager";

        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length != 2 || args[0] != "--apply")
            {
                MessageBox.Show("This helper is started by the Alibre Add-On Manager add-on.\n\nUsage: --apply <plan.json>",
                    Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 2;
            }

            if (!IsElevated())
            {
                MessageBox.Show("The helper must run as administrator.", Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 3;
            }

            var planPath = args[1];
            var result = new PlanResult();
            try
            {
                var plan = Json.ReadFile<InstallPlan>(planPath);

                if (plan.RequiresAlibreClosed && IsAlibreRunning())
                {
                    using (var wait = new WaitForAlibreForm(plan))
                    {
                        if (wait.ShowDialog() != DialogResult.OK)
                        {
                            result.Cancelled = true;
                            result.Lines.Add("Cancelled before any change was made.");
                            Finish(planPath, result);
                            return 1;
                        }
                    }
                }

                new Installer(result).Apply(plan);
            }
            catch (Exception e)
            {
                result.Failures++;
                result.Lines.Add("FAILED  " + e.Message);
            }

            Finish(planPath, result);

            var message = new StringBuilder();
            message.AppendLine(result.Failures == 0 ? "All changes were applied." : $"{result.Failures} change(s) failed.");
            message.AppendLine();
            foreach (var line in result.Lines) message.AppendLine(line);
            message.AppendLine();
            message.Append("Alibre Design reads add-ons only at startup — start (or restart) it to see the changes.");
            MessageBox.Show(message.ToString(), Title, MessageBoxButtons.OK,
                result.Failures == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            return result.Failures == 0 ? 0 : 1;
        }

        internal static bool IsAlibreRunning()
        {
            return Process.GetProcessesByName(ManagerPaths.AlibreProcessName).Any();
        }

        private static bool IsElevated()
        {
            using (var identity = WindowsIdentity.GetCurrent())
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void Finish(string planPath, PlanResult result)
        {
            result.Completed = DateTime.UtcNow.ToString("o");
            try
            {
                Json.WriteFile(Path.ChangeExtension(planPath, ".result.json"), result);
            }
            catch (Exception)
            {
                // The message box still reports the outcome.
            }
        }
    }
}
