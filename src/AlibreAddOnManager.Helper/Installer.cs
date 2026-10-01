using System;
using System.IO;
using System.Linq;
using AlibreAddOnManager.Models;

namespace AlibreAddOnManager.Helper
{
    /// <summary>
    /// Applies an InstallPlan. Runs elevated: it is the only code that writes to
    /// %ProgramData%\Alibre AddOns, the state file and HKLM.
    /// </summary>
    internal class Installer
    {
        private readonly PlanResult _result;
        private readonly ManagerState _state;

        public Installer(PlanResult result)
        {
            _result = result;
            _state = AddOnScanner.LoadState();
        }

        public void Apply(InstallPlan plan)
        {
            foreach (var op in plan.Operations)
            {
                try
                {
                    switch (op.Kind)
                    {
                        case PlanOperationKind.Install: Install(op); break;
                        case PlanOperationKind.Uninstall: Uninstall(op); break;
                        case PlanOperationKind.Disable: Disable(op); break;
                        case PlanOperationKind.Enable: Enable(op); break;
                        case PlanOperationKind.RemoveRegistration: RemoveRegistration(op); break;
                        default: throw new InvalidOperationException("Unknown operation '" + op.Kind + "'.");
                    }

                    _result.Lines.Add("OK      " + op.Describe());
                }
                catch (Exception e)
                {
                    _result.Failures++;
                    _result.Lines.Add("FAILED  " + op.Describe() + ": " + e.Message);
                }

                // Save after every operation so the state always matches what was done.
                Json.WriteFile(ManagerPaths.StateFile, _state);
            }
        }

        private void Install(PlanOperation op)
        {
            if (!File.Exists(op.PackagePath))
                throw new FileNotFoundException("Package not found: " + op.PackagePath);

            // The plan and package sit in the user's profile; check nothing changed
            // since the unelevated add-on validated them.
            var hash = Packages.Sha256File(op.PackagePath);
            if (!string.Equals(hash, op.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Package hash changed since it was validated.");

            var validation = Packages.Validate(op.PackagePath, op.Id);
            if (!validation.IsValid)
                throw new InvalidDataException(string.Join(" ", validation.Errors));

            var manifest = validation.Manifest;
            var root = ManagerPaths.AddOnsRoot;
            var target = Path.Combine(root, manifest.FolderName);
            if (!ManagerPaths.IsSameOrUnder(target, root) ||
                string.Equals(Path.GetFullPath(target).TrimEnd('\\'), Path.GetFullPath(root).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Target folder escapes " + root);

            // Refuse to overwrite a folder another add-on is registered to.
            var registered = AddOnRegistry.ReadAll();
            foreach (var pair in registered)
            {
                if (ManagerPaths.SameId(pair.Key, manifest.Id) || string.IsNullOrWhiteSpace(pair.Value)) continue;
                if (string.Equals(Path.GetFullPath(pair.Value).TrimEnd('\\'), Path.GetFullPath(target).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Folder {target} is registered to a different add-on ({pair.Key}).");
            }

            // An existing folder is only replaced if we own it (tracked, or this manager).
            var existingTracked = _state.Tracked.FirstOrDefault(t => ManagerPaths.SameId(t.Id, manifest.Id));
            if (Directory.Exists(target) && existingTracked == null && !ManagerPaths.SameId(manifest.Id, ManagerPaths.SelfId))
                throw new InvalidOperationException($"Folder {target} already exists and was not installed by the add-on manager.");

            Directory.CreateDirectory(root);
            var staging = target + ".staging";
            var backup = target + ".bak";
            DeleteDirectory(staging);
            Packages.ExtractSafely(op.PackagePath, staging);

            var movedOld = false;
            try
            {
                if (Directory.Exists(target))
                {
                    DeleteDirectory(backup);
                    Directory.Move(target, backup); // fails if a DLL in it is locked
                    movedOld = true;
                }

                Directory.Move(staging, target);
            }
            catch
            {
                if (movedOld && !Directory.Exists(target))
                    Directory.Move(backup, target);
                DeleteDirectory(staging);
                throw;
            }

            AddOnRegistry.SetValue(ManagerPaths.NormalizeId(manifest.Id), target);

            _state.Tracked.RemoveAll(t => ManagerPaths.SameId(t.Id, manifest.Id));
            _state.Disabled.RemoveAll(d => ManagerPaths.SameId(d.Id, manifest.Id));
            _state.Tracked.Add(new TrackedAddOn
            {
                Id = ManagerPaths.NormalizeId(manifest.Id),
                Name = manifest.Name,
                Version = manifest.Version,
                Folder = target,
                Source = op.Source,
                Sha256 = hash,
                InstalledUtc = DateTime.UtcNow.ToString("o")
            });

            TryDeleteDirectory(backup);
        }

        private void Uninstall(PlanOperation op)
        {
            var tracked = _state.Tracked.FirstOrDefault(t => ManagerPaths.SameId(t.Id, op.Id));
            if (tracked == null)
                throw new InvalidOperationException("Not installed by the add-on manager; only Disable is allowed.");

            AddOnRegistry.DeleteValue(op.Id);

            // Delete only a folder we created, and only inside our own root.
            if (Directory.Exists(tracked.Folder))
            {
                if (!ManagerPaths.IsSameOrUnder(tracked.Folder, ManagerPaths.AddOnsRoot))
                    throw new InvalidOperationException("Tracked folder is outside " + ManagerPaths.AddOnsRoot + "; files left in place.");
                Directory.Delete(tracked.Folder, true); // fails if a DLL in it is locked
            }

            _state.Tracked.Remove(tracked);
            _state.Disabled.RemoveAll(d => ManagerPaths.SameId(d.Id, op.Id));
        }

        private void Disable(PlanOperation op)
        {
            var registered = AddOnRegistry.ReadAll();
            var name = AddOnRegistry.FindValueName(registered, op.Id);
            if (name == null)
                throw new InvalidOperationException("Not currently registered.");

            _state.Disabled.RemoveAll(d => ManagerPaths.SameId(d.Id, op.Id));
            _state.Disabled.Add(new DisabledAddOn
            {
                Id = name, // keep the exact value name so Enable restores it verbatim
                Name = op.Name,
                Folder = registered[name],
                DisabledUtc = DateTime.UtcNow.ToString("o")
            });
            Json.WriteFile(ManagerPaths.StateFile, _state); // record before removing
            AddOnRegistry.DeleteValue(name);
        }

        private void Enable(PlanOperation op)
        {
            var disabled = _state.Disabled.FirstOrDefault(d => ManagerPaths.SameId(d.Id, op.Id));
            if (disabled == null)
                throw new InvalidOperationException("No record of this add-on being disabled.");

            AddOnRegistry.SetValue(disabled.Id, disabled.Folder);
            _state.Disabled.Remove(disabled);
        }

        private void RemoveRegistration(PlanOperation op)
        {
            if (!AddOnRegistry.DeleteValue(op.Id))
                throw new InvalidOperationException("Not currently registered.");
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        private static void TryDeleteDirectory(string path)
        {
            try { DeleteDirectory(path); }
            catch (Exception) { /* a leftover .bak is harmless */ }
        }
    }
}
