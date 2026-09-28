using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AlibreAddInManager.Models;

namespace AlibreAddInManager
{
    public enum AddOnStatus
    {
        /// <summary>This add-on manager itself.</summary>
        Self,
        /// <summary>Installed by this tool; its files are ours to replace or delete.</summary>
        Tracked,
        /// <summary>Registered by hand, a vendor installer or a developer build — never delete its files.</summary>
        External,
        /// <summary>Registry value removed by Disable; can be restored.</summary>
        Disabled,
        /// <summary>Registry points at a missing folder, or no .adc there carries the id.</summary>
        Broken
    }

    public class InstalledAddOn
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public AddOnStatus Status { get; set; }
        public string Folder { get; set; }
        public AdcFile Adc { get; set; }
        public string Problem { get; set; }
        public TrackedAddOn Tracked { get; set; }

        public string DllTypeText => Adc == null ? "" : AdcFile.DllTypeName(Adc.DllType);

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case AddOnStatus.Self: return "This add-on";
                    case AddOnStatus.Disabled: return Tracked != null ? "Disabled (tracked)" : "Disabled";
                    default: return Status.ToString();
                }
            }
        }
    }

    /// <summary>
    /// Builds the Installed view. The registry key is the source of truth, and each
    /// registered path is followed wherever it points — Program Files, ProgramData,
    /// a developer's bin\Debug on another drive.
    /// </summary>
    public static class AddOnScanner
    {
        public static ManagerState LoadState()
        {
            try
            {
                if (File.Exists(ManagerPaths.StateFile))
                    return Json.ReadFile<ManagerState>(ManagerPaths.StateFile) ?? new ManagerState();
            }
            catch (Exception e)
            {
                Trace.WriteLine("AlibreAddInManager: unreadable state file: " + e.Message);
            }

            return new ManagerState();
        }

        public static List<InstalledAddOn> Scan(ManagerState state)
        {
            var result = new List<InstalledAddOn>();
            var registered = AddOnRegistry.ReadAll();

            foreach (var pair in registered)
            {
                var id = ManagerPaths.NormalizeId(pair.Key);
                var folder = pair.Value;
                var tracked = state.Tracked.FirstOrDefault(t => ManagerPaths.SameId(t.Id, id));
                var item = new InstalledAddOn { Id = id, Folder = folder, Tracked = tracked };

                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    item.Status = AddOnStatus.Broken;
                    item.Name = tracked?.Name ?? id;
                    item.Problem = "Registered folder does not exist: " + folder;
                }
                else
                {
                    item.Adc = AdcFile.FindForIdentifier(folder, id);
                    if (item.Adc == null)
                    {
                        item.Status = AddOnStatus.Broken;
                        item.Name = tracked?.Name ?? id;
                        item.Problem = "No .adc in the folder has Identifier " + id;
                    }
                    else
                    {
                        item.Name = item.Adc.DisplayName;
                        item.Status = ManagerPaths.SameId(id, ManagerPaths.SelfId) ? AddOnStatus.Self
                            : tracked != null ? AddOnStatus.Tracked
                            : AddOnStatus.External;
                        item.Version = tracked?.Version ?? DllFileVersion(item.Adc);
                        var dll = item.Adc.ResolveDllPath();
                        if (dll != null && !File.Exists(dll))
                            item.Problem = "DLL named in the .adc is missing: " + dll;
                    }
                }

                result.Add(item);
            }

            foreach (var disabled in state.Disabled)
            {
                if (AddOnRegistry.FindValueName(registered, disabled.Id) != null) continue; // re-registered externally
                var tracked = state.Tracked.FirstOrDefault(t => ManagerPaths.SameId(t.Id, disabled.Id));
                var adc = AdcFile.FindForIdentifier(disabled.Folder, disabled.Id);
                result.Add(new InstalledAddOn
                {
                    Id = ManagerPaths.NormalizeId(disabled.Id),
                    Name = adc?.DisplayName ?? disabled.Name ?? disabled.Id,
                    Folder = disabled.Folder,
                    Adc = adc,
                    Tracked = tracked,
                    Version = tracked?.Version ?? (adc != null ? DllFileVersion(adc) : null),
                    Status = AddOnStatus.Disabled,
                    Problem = adc == null ? "Folder or .adc no longer present; Enable would register a broken add-on." : null
                });
            }

            return result.OrderBy(a => a.Status == AddOnStatus.Self ? 0 : 1)
                .ThenBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private static string DllFileVersion(AdcFile adc)
        {
            try
            {
                var dll = adc.ResolveDllPath();
                if (dll == null || !File.Exists(dll)) return null;
                var info = FileVersionInfo.GetVersionInfo(dll);
                return info.ProductVersion ?? info.FileVersion;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
