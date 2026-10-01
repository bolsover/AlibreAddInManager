using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AlibreAddOnManager.Models;

namespace AlibreAddOnManager.Ui
{
    /// <summary>
    /// The manager window. Everything here runs unelevated inside Alibre: it
    /// reads the registry and state, downloads and validates packages, and
    /// queues changes. Only "Apply changes" crosses into the elevated helper.
    /// </summary>
    public partial class ManagerForm : Form
    {
        private readonly string _alibreVersion;
        private readonly int _runningBuild;
        private readonly List<AlibreInstall> _installs;
        private readonly List<PendingChange> _pending = new List<PendingChange>();
        private readonly Dictionary<string, AddOnManifest> _manifests = new Dictionary<string, AddOnManifest>(StringComparer.OrdinalIgnoreCase);
        private readonly ManagerSettings _settings;

        private ManagerState _state = new ManagerState();
        private List<InstalledAddOn> _installed = new List<InstalledAddOn>();
        private CatalogClient _catalog;
        private CatalogIndex _index;

        public ManagerForm(string alibreVersion)
        {
            InitializeComponent();
            _alibreVersion = alibreVersion;
            _runningBuild = AlibreInstallDetector.ParseBuild(alibreVersion);
            _installs = SafeFindInstalls();
            _settings = SettingsStore.Load();
        }

        private sealed class PendingChange
        {
            public PlanOperation Operation { get; set; }
            public bool RequiresAlibreClosed { get; set; }

            public override string ToString() =>
                Operation.Describe() + (RequiresAlibreClosed ? "   (applied after Alibre Design closes)" : "");
        }

        #region Load / refresh

        private async void ManagerForm_Load(object sender, EventArgs e)
        {
            txtCatalogUrl.Text = _settings.CatalogUrl ?? string.Empty;
            RefreshInstalled();
            UpdatePendingButtons();
            SetStatus(DescribeEnvironment());

            if (!string.IsNullOrWhiteSpace(txtCatalogUrl.Text))
                await LoadCatalogAsync();
        }

        private string DescribeEnvironment()
        {
            var running = _runningBuild > 0 ? $"Running Alibre Design build {_runningBuild}" : "Running Alibre Design (build unknown)";
            if (_installs.Count <= 1) return running + ".";
            return running + ". Installed side by side: " + string.Join(", ", _installs.Select(i => i.Version)) +
                   " — every one of them loads every registered add-on.";
        }

        private void RefreshInstalled()
        {
            try
            {
                _state = AddOnScanner.LoadState();
                _installed = AddOnScanner.Scan(_state);
            }
            catch (Exception e)
            {
                _installed = new List<InstalledAddOn>();
                ShowError("Could not read the registered add-ons", e);
            }

            listInstalled.BeginUpdate();
            listInstalled.Items.Clear();
            foreach (var addOn in _installed)
            {
                var item = new ListViewItem(new[]
                {
                    addOn.Name ?? addOn.Id,
                    addOn.Version ?? "",
                    addOn.StatusText,
                    addOn.DllTypeText,
                    addOn.Folder ?? ""
                }) { Tag = addOn };
                if (addOn.Status == AddOnStatus.Broken || addOn.Problem != null)
                    item.ForeColor = System.Drawing.Color.Firebrick;
                else if (addOn.Status == AddOnStatus.Disabled)
                    item.ForeColor = System.Drawing.SystemColors.GrayText;
                listInstalled.Items.Add(item);
            }

            listInstalled.EndUpdate();
            UpdateInstalledButtons();
            RefreshAvailableInstalledColumn();
        }

        #endregion

        #region Installed tab

        private InstalledAddOn SelectedInstalled =>
            listInstalled.SelectedItems.Count == 1 ? listInstalled.SelectedItems[0].Tag as InstalledAddOn : null;

        private void listInstalled_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateInstalledButtons();
        }

        private void UpdateInstalledButtons()
        {
            var a = SelectedInstalled;
            btnDisable.Enabled = a != null && (a.Status == AddOnStatus.Tracked || a.Status == AddOnStatus.External);
            btnEnable.Enabled = a != null && a.Status == AddOnStatus.Disabled && a.Adc != null;
            btnUninstall.Enabled = a != null && a.Tracked != null && a.Status != AddOnStatus.Self;
            btnRemoveRegistration.Enabled = a != null && a.Status == AddOnStatus.Broken;
            btnOpenFolder.Enabled = a != null && !string.IsNullOrEmpty(a.Folder) && Directory.Exists(a.Folder);
            txtInstalledDetails.Text = a == null ? string.Empty : DescribeInstalled(a);
        }

        private static string DescribeInstalled(InstalledAddOn a)
        {
            var text = new StringBuilder();
            text.Append(a.Name).Append("   ").Append(a.Id).AppendLine();
            if (a.Adc != null)
            {
                if (!string.IsNullOrWhiteSpace(a.Adc.AuthorName)) text.Append("Author: ").Append(a.Adc.AuthorName).Append("   ");
                text.Append("DLL: ").Append(a.Adc.DllLocation).Append(" (").Append(a.DllTypeText).Append(")   ");
                text.Append("Workspace: ").Append(a.Adc.Workspace).Append("   Spec v").Append(a.Adc.SpecificationVersion).AppendLine();
                if (!string.IsNullOrWhiteSpace(a.Adc.Description)) text.AppendLine(a.Adc.Description);
            }

            switch (a.Status)
            {
                case AddOnStatus.External:
                    text.AppendLine("Not installed by this manager: it can be disabled, but its files are never deleted. Use its own uninstaller to remove it.");
                    break;
                case AddOnStatus.Self:
                    text.AppendLine("This add-on manager. Update it from the catalog or a package; remove it with deploy\\uninstall.ps1.");
                    break;
            }

            if (a.Problem != null) text.Append("Problem: ").AppendLine(a.Problem);
            return text.ToString();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshInstalled();
            SetStatus(DescribeEnvironment());
        }

        private void btnInstallFromFile_Click(object sender, EventArgs e)
        {
            if (openPackageDialog.ShowDialog(this) != DialogResult.OK) return;
            QueuePackage(openPackageDialog.FileName, "file:" + openPackageDialog.FileName, null, null, null);
        }

        private void btnDisable_Click(object sender, EventArgs e)
        {
            var a = SelectedInstalled;
            if (a == null) return;
            Queue(new PlanOperation { Kind = PlanOperationKind.Disable, Id = a.Id, Name = a.Name }, false);
        }

        private void btnEnable_Click(object sender, EventArgs e)
        {
            var a = SelectedInstalled;
            if (a == null) return;
            Queue(new PlanOperation { Kind = PlanOperationKind.Enable, Id = a.Id, Name = a.Name }, false);
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            var a = SelectedInstalled;
            if (a?.Tracked == null) return;
            if (!Confirm($"Uninstall {a.Name}?\n\nIts registry entry and the folder\n{a.Tracked.Folder}\nwill be deleted.")) return;
            Queue(new PlanOperation { Kind = PlanOperationKind.Uninstall, Id = a.Id, Name = a.Name }, Directory.Exists(a.Tracked.Folder));
        }

        private void btnRemoveRegistration_Click(object sender, EventArgs e)
        {
            var a = SelectedInstalled;
            if (a == null) return;
            Queue(new PlanOperation { Kind = PlanOperationKind.RemoveRegistration, Id = a.Id, Name = a.Name }, false);
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            var a = SelectedInstalled;
            if (a != null && Directory.Exists(a.Folder))
                Process.Start("explorer.exe", "\"" + a.Folder + "\"");
        }

        #endregion

        #region Available tab

        private async void btnLoadCatalog_Click(object sender, EventArgs e)
        {
            await LoadCatalogAsync();
        }

        private async Task LoadCatalogAsync()
        {
            var location = txtCatalogUrl.Text.Trim();
            if (location.Length == 0)
            {
                SetStatus("Enter a catalog URL (https://…/index.json) or a local folder.");
                return;
            }

            SetBusy(true, "Loading catalog…");
            try
            {
                var client = new CatalogClient();
                var index = await client.LoadIndexAsync(location);
                _catalog = client;
                _index = index;
                _manifests.Clear();

                if (!string.Equals(_settings.CatalogUrl, location, StringComparison.Ordinal))
                {
                    _settings.CatalogUrl = location;
                    SettingsStore.Save(_settings);
                }

                listAvailable.BeginUpdate();
                listAvailable.Items.Clear();
                foreach (var entry in index.AddOns.OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase))
                    listAvailable.Items.Add(new ListViewItem(new[] { entry.Name, entry.Publisher ?? "", entry.LatestVersion ?? "", "", entry.Summary ?? "" }) { Tag = entry });
                listAvailable.EndUpdate();
                RefreshAvailableInstalledColumn();
                SetStatus($"Catalog loaded: {index.AddOns.Count} add-on(s). Catalog signature is not verified in this version; packages are checked against their SHA-256.");
            }
            catch (Exception e)
            {
                SetStatus("Catalog could not be loaded.");
                ShowError("Could not load the catalog from " + location, e);
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        private void RefreshAvailableInstalledColumn()
        {
            foreach (ListViewItem item in listAvailable.Items)
            {
                var entry = (CatalogEntry) item.Tag;
                var installed = _installed.FirstOrDefault(a => ManagerPaths.SameId(a.Id, entry.Id));
                item.SubItems[3].Text = installed == null ? "" : installed.Version ?? installed.StatusText;
            }
        }

        private CatalogEntry SelectedEntry =>
            listAvailable.SelectedItems.Count == 1 ? listAvailable.SelectedItems[0].Tag as CatalogEntry : null;

        private async void listAvailable_SelectedIndexChanged(object sender, EventArgs e)
        {
            var entry = SelectedEntry;
            btnInstallSelected.Enabled = entry != null;
            if (entry == null) { txtAvailableDetails.Text = string.Empty; return; }

            txtAvailableDetails.Text = entry.Summary ?? string.Empty;
            try
            {
                var manifest = await GetManifestAsync(entry);
                if (SelectedEntry == entry)
                    txtAvailableDetails.Text = DescribeManifest(manifest);
            }
            catch (Exception ex)
            {
                if (SelectedEntry == entry)
                    txtAvailableDetails.Text = "Could not load the manifest: " + ex.Message;
            }
        }

        private async Task<AddOnManifest> GetManifestAsync(CatalogEntry entry)
        {
            if (_manifests.TryGetValue(entry.Id, out var cached)) return cached;
            var manifest = await _catalog.LoadManifestAsync(entry);
            _manifests[entry.Id] = manifest;
            return manifest;
        }

        private string DescribeManifest(AddOnManifest m)
        {
            var text = new StringBuilder();
            text.Append(m.Name).Append("   ").AppendLine(m.Id);
            if (m.Publisher != null) text.Append("Publisher: ").Append(m.Publisher.Name).Append("   ").AppendLine(m.Publisher.Url);
            if (!string.IsNullOrWhiteSpace(m.License)) text.Append("License: ").AppendLine(m.License);
            if (!string.IsNullOrWhiteSpace(m.Homepage)) text.Append("Homepage: ").AppendLine(m.Homepage);
            text.AppendLine();
            if (!string.IsNullOrWhiteSpace(m.Description)) text.AppendLine(m.Description).AppendLine();

            var chosen = CatalogClient.ChooseVersion(m, _runningBuild);
            text.AppendLine("Versions:");
            foreach (var v in m.Versions.OrderByDescending(v => v.Version, Comparer<string>.Create(Packages.CompareVersions)))
            {
                text.Append(v == chosen ? " → " : "   ").Append(v.Version);
                if (!string.IsNullOrWhiteSpace(v.Released)) text.Append("  (").Append(v.Released).Append(")");
                text.Append("  ").Append(v.DllType ?? "?").Append("/").Append(v.Arch ?? "?");
                text.Append("  builds ").Append(v.MinAlibreBuild?.ToString() ?? "any").Append("–").Append(v.MaxAlibreBuild?.ToString() ?? "");
                if (!v.IsCompatibleWith(_runningBuild)) text.Append("  NOT compatible with the running build");
                text.AppendLine();
                if (!string.IsNullOrWhiteSpace(v.ReleaseNotes)) text.Append("      ").AppendLine(v.ReleaseNotes);
            }

            return text.ToString();
        }

        private async void btnInstallSelected_Click(object sender, EventArgs e)
        {
            var entry = SelectedEntry;
            if (entry == null || _catalog == null) return;

            SetBusy(true, "Fetching " + entry.Name + "…");
            try
            {
                var manifest = await GetManifestAsync(entry);
                var version = CatalogClient.ChooseVersion(manifest, _runningBuild);
                if (version == null)
                {
                    MessageBox.Show(this, "The catalog lists no installable version of " + manifest.Name + ".", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SetStatus($"Downloading {manifest.Name} {version.Version}…");
                var path = await _catalog.DownloadAsync(manifest, version);

                var notes = new List<string>();
                if (version.Prerequisites != null && version.Prerequisites.Count > 0)
                    notes.Add("Requires: " + string.Join(", ", version.Prerequisites) + " (not installed automatically).");

                SetBusy(false, null);
                QueuePackage(path, _catalog.IndexUri.ToString(), manifest.Id, version.DllType, notes);
            }
            catch (Exception ex)
            {
                SetStatus("Download failed.");
                ShowError("Could not download " + entry.Name, ex);
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        #endregion

        #region Queueing

        /// <summary>Validates a package and, after any confirmations, queues an Install.</summary>
        private void QueuePackage(string path, string source, string catalogId, string catalogDllType, List<string> notes)
        {
            PackageValidationResult validation;
            string hash;
            try
            {
                validation = Packages.Validate(path, catalogId, catalogDllType);
                hash = Packages.Sha256File(path);
            }
            catch (Exception e)
            {
                ShowError("Could not read the package", e);
                return;
            }

            if (!validation.IsValid)
            {
                MessageBox.Show(this, "This package cannot be installed:\n\n• " + string.Join("\n• ", validation.Errors),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var m = validation.Manifest;
            var id = ManagerPaths.NormalizeId(m.Id);
            var target = Path.Combine(ManagerPaths.AddOnsRoot, m.FolderName);
            var warnings = new List<string>(validation.Warnings);
            if (notes != null) warnings.AddRange(notes);

            // Folder ownership and collisions (design doc §3.2 / §3.5).
            var existing = _installed.FirstOrDefault(a => ManagerPaths.SameId(a.Id, id));
            var ownsFolder = (existing?.Tracked != null) || ManagerPaths.SameId(id, ManagerPaths.SelfId);
            if (Directory.Exists(target) && !ownsFolder)
            {
                MessageBox.Show(this, $"The folder\n{target}\nalready exists and was not created by the add-on manager. Remove or rename it first.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var clash = _installed.FirstOrDefault(a => !ManagerPaths.SameId(a.Id, id) && a.Folder != null &&
                string.Equals(a.Folder.TrimEnd('\\'), target.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
            if (clash != null)
            {
                MessageBox.Show(this, $"The folder {target} is registered to a different add-on ({clash.Name}).", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Compatibility with the running build and every side-by-side install.
            var range = new AddOnVersion { MinAlibreBuild = m.MinAlibreBuild, MaxAlibreBuild = m.MaxAlibreBuild };
            if (!range.IsCompatibleWith(_runningBuild))
                warnings.Add($"Not compatible with the running Alibre build {_runningBuild} (supports {m.MinAlibreBuild?.ToString() ?? "any"}–{m.MaxAlibreBuild?.ToString() ?? ""}).");
            foreach (var install in _installs.Where(i => i.Build != _runningBuild && !range.IsCompatibleWith(i.Build)))
                warnings.Add($"Alibre Design {install.Version} is also installed and will load it, but is outside its supported range.");

            var question = new StringBuilder();
            question.Append("Install ").Append(m.Name).Append(' ').Append(m.Version);
            question.Append(" (").Append(AdcFile.DllTypeName(validation.Adc.DllType)).Append(", ").Append(validation.Dll.MachineName).AppendLine(")");
            question.Append("into ").AppendLine(target);

            if (existing != null)
            {
                question.AppendLine();
                switch (existing.Status)
                {
                    case AddOnStatus.External:
                        question.AppendLine($"{existing.Name} is already registered from\n{existing.Folder}\nby another installer. Taking it over points Alibre at the new copy; the old files stay where they are, and the vendor's uninstaller may later remove the registration.");
                        break;
                    case AddOnStatus.Tracked:
                    case AddOnStatus.Self:
                        question.AppendLine($"This replaces installed version {existing.Version}.");
                        break;
                    case AddOnStatus.Disabled:
                        question.AppendLine("The add-on is currently disabled; installing enables it.");
                        break;
                }
            }

            if (warnings.Count > 0)
                question.AppendLine().AppendLine("Warnings:").Append("• ").AppendLine(string.Join("\n• ", warnings));

            if (!Confirm(question.ToString())) return;

            Queue(new PlanOperation
            {
                Kind = PlanOperationKind.Install,
                Id = id,
                Name = m.Name,
                FolderName = m.FolderName,
                PackagePath = path,
                Sha256 = hash,
                Version = m.Version,
                Source = source
            }, Directory.Exists(target));
        }

        private void Queue(PlanOperation operation, bool requiresAlibreClosed)
        {
            // One pending change per add-on: a later choice replaces an earlier one.
            _pending.RemoveAll(p => ManagerPaths.SameId(p.Operation.Id, operation.Id));
            _pending.Add(new PendingChange { Operation = operation, RequiresAlibreClosed = requiresAlibreClosed });
            RefreshPending();
            SetStatus("Queued: " + operation.Describe() + ". Click Apply changes when ready.");
        }

        private void RefreshPending()
        {
            listPending.BeginUpdate();
            listPending.Items.Clear();
            foreach (var change in _pending) listPending.Items.Add(change);
            listPending.EndUpdate();
            UpdatePendingButtons();
        }

        private void UpdatePendingButtons()
        {
            btnApply.Enabled = _pending.Count > 0;
            btnClearPending.Enabled = _pending.Count > 0;
            btnRemovePending.Enabled = _pending.Count > 0;
            groupPending.Text = _pending.Count == 0 ? "Pending changes" : $"Pending changes ({_pending.Count})";
        }

        private void btnRemovePending_Click(object sender, EventArgs e)
        {
            if (listPending.SelectedItem is PendingChange change)
            {
                _pending.Remove(change);
                RefreshPending();
            }
        }

        private void btnClearPending_Click(object sender, EventArgs e)
        {
            _pending.Clear();
            RefreshPending();
        }

        private async void btnApply_Click(object sender, EventArgs e)
        {
            if (_pending.Count == 0) return;

            var plan = new InstallPlan
            {
                RequiresAlibreClosed = _pending.Any(p => p.RequiresAlibreClosed),
                Operations = _pending.Select(p => p.Operation).ToList()
            };

            var message = new StringBuilder("Apply these changes?\n\n");
            foreach (var p in _pending) message.Append("• ").AppendLine(p.Operation.Describe());
            message.AppendLine().AppendLine("Windows will ask for administrator permission.");
            if (plan.RequiresAlibreClosed)
                message.AppendLine("Some add-on files are in use, so the changes will be made after you close Alibre Design.");
            message.Append("Alibre Design must be restarted before it sees any change.");
            if (!Confirm(message.ToString())) return;

            Process helper;
            try
            {
                helper = HelperLauncher.Launch(plan, out _);
            }
            catch (Exception ex)
            {
                ShowError("Could not start the installer helper", ex);
                return;
            }

            if (helper == null)
            {
                SetStatus("Administrator permission was declined; nothing was changed.");
                return;
            }

            _pending.Clear();
            RefreshPending();

            if (plan.RequiresAlibreClosed)
            {
                SetStatus("Waiting for Alibre Design to close — the helper will then apply the changes.");
                MessageBox.Show(this, "The changes will be applied once you save your work and close Alibre Design.\n\n" +
                                      "Start Alibre Design again afterwards to use them.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetBusy(true, "Applying changes…");
            try
            {
                await Task.Run(() => helper.WaitForExit());
            }
            finally
            {
                helper.Dispose();
                SetBusy(false, null);
            }

            RefreshInstalled();
            SetStatus("Changes applied. Restart Alibre Design to load them.");
        }

        #endregion

        #region Helpers

        private void SetBusy(bool busy, string status)
        {
            UseWaitCursor = busy;
            tabControl.Enabled = !busy;
            groupPending.Enabled = !busy;
            if (status != null) SetStatus(status);
        }

        private void SetStatus(string text)
        {
            statusLabel.Text = text ?? string.Empty;
        }

        private bool Confirm(string text)
        {
            return MessageBox.Show(this, text, Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK;
        }

        private void ShowError(string what, Exception e)
        {
            var inner = e;
            while (inner.InnerException != null) inner = inner.InnerException;
            MessageBox.Show(this, what + ":\n\n" + inner.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static List<AlibreInstall> SafeFindInstalls()
        {
            try { return AlibreInstallDetector.Find(); }
            catch (Exception) { return new List<AlibreInstall>(); }
        }

        #endregion
    }
}
