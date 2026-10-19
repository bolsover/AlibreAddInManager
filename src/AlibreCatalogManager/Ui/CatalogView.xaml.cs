using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AlibreAddOnManager;
using AlibreAddOnManager.Models;
using AlibreCatalogManager.Catalog;

namespace AlibreCatalogManager.Ui
{
    /// <summary>
    /// Browse and edit a local catalog folder. Edits are held in memory until Save;
    /// "Remove" unlists an add-on (its files stay, it can be re-listed), "Delete"
    /// removes its manifest and, optionally, its packages.
    /// </summary>
    public partial class CatalogView : UserControl
    {
        private static readonly Brush ErrorBrush = Brushes.Firebrick;

        private readonly AppSettings _settings;
        private readonly ObservableCollection<AddOnRow> _rows = new ObservableCollection<AddOnRow>();
        private readonly ObservableCollection<VersionRow> _versionRows = new ObservableCollection<VersionRow>();
        private readonly ObservableCollection<ProblemRow> _problemRows = new ObservableCollection<ProblemRow>();
        private CatalogDocument _doc;
        private List<CatalogIssue> _issues = new List<CatalogIssue>();
        private bool _loaded;

        public CatalogView(AppSettings settings)
        {
            _settings = settings;
            InitializeComponent();
            AddOnList.ItemsSource = _rows;
            VersionList.ItemsSource = _versionRows;
            ProblemList.ItemsSource = _problemRows;
            UpdateUi();

            Loaded += (s, e) =>
            {
                if (_loaded) return;
                _loaded = true;
                if (!string.IsNullOrEmpty(_settings.LastCatalog) && File.Exists(Path.Combine(_settings.LastCatalog, CatalogDocument.IndexFileName)))
                    OpenCatalog(_settings.LastCatalog);
            };
        }

        public bool HasUnsavedChanges => _doc != null && _doc.HasUnsavedChanges;

        /// <summary>The open catalog's folder, or null.</summary>
        public string CatalogFolder => _doc?.RootFolder;

        /// <summary>Raised whenever the open catalog, its contents or its saved state change.</summary>
        public event EventHandler CatalogChanged;

        private CatalogItem SelectedItem => (AddOnList.SelectedItem as AddOnRow)?.Item;

        private AddOnVersion SelectedVersion => (VersionList.SelectedItem as VersionRow)?.Version;

        #region Toolbar

        private void OnNew(object sender, RoutedEventArgs e) => CreateCatalog();

        private void OnOpen(object sender, RoutedEventArgs e) => BrowseAndOpen();

        private void OnReload(object sender, RoutedEventArgs e)
        {
            if (_doc != null && ConfirmDiscard()) OpenCatalog(_doc.RootFolder);
        }

        private void OnSave(object sender, RoutedEventArgs e) => SaveCatalog();

        /// <summary>The "Add add-on" / "Add version" buttons drop their menu down below them.</summary>
        private void OnShowMenu(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.IsOpen = true;
        }

        private void OnAddAddOnFromPackage(object sender, RoutedEventArgs e) => AddAddOn(false);

        private void OnAddAddOnFromFolder(object sender, RoutedEventArgs e) => AddAddOn(true);

        private void OnAddVersionFromPackage(object sender, RoutedEventArgs e) => AddVersion(false);

        private void OnAddVersionFromFolder(object sender, RoutedEventArgs e) => AddVersion(true);

        private void OnToggleListed(object sender, RoutedEventArgs e) => ToggleListed();

        private void OnDelete(object sender, RoutedEventArgs e) => DeleteAddOn();

        private void OnCheck(object sender, RoutedEventArgs e) => RunCheck(true);

        private void OnSearchChanged(object sender, TextChangedEventArgs e) => RefreshList(SelectedItem);

        private void OnRemoveVersion(object sender, RoutedEventArgs e) => RemoveVersion();

        private void OnRehash(object sender, RoutedEventArgs e) => Rehash();

        private void OnAddOnSelected(object sender, SelectionChangedEventArgs e) => ShowDetails();

        private void OnVersionSelected(object sender, SelectionChangedEventArgs e) => ShowVersion();

        private void OnProblemDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => GoToProblem();

        #endregion

        #region Open / save

        public void BrowseAndOpen()
        {
            if (!ConfirmDiscard()) return;
            var path = Dialogs.OpenFile(this, "Open catalog index.json", "Catalog index (index.json)|index.json|JSON files (*.json)|*.json",
                _doc?.RootFolder ?? _settings.LastCatalog);
            if (path != null) OpenCatalog(path);
        }

        public void CreateCatalog()
        {
            if (!ConfirmDiscard()) return;
            var folder = Dialogs.BrowseFolder(this, "Folder for the new catalog (index.json, addons\\, packages\\)");
            if (folder == null) return;
            try
            {
                _doc = CatalogDocument.CreateNew(folder);
                _settings.LastCatalog = _doc.RootFolder;
                _settings.Save();
                _issues = new List<CatalogIssue>();
                AfterChange(null);
            }
            catch (Exception e)
            {
                Dialogs.Error(this, "Cannot create the catalog", e);
            }
        }

        private void OpenCatalog(string folderOrIndex)
        {
            try
            {
                using (Dialogs.Busy())
                {
                    _doc = CatalogDocument.Open(folderOrIndex);
                    _settings.LastCatalog = _doc.RootFolder;
                    _settings.Save();
                    _issues = CatalogValidator.Check(_doc, false);
                }

                AfterChange(null);
            }
            catch (Exception e)
            {
                Dialogs.Error(this, "Cannot open the catalog", e);
            }
        }

        private void SaveCatalog()
        {
            if (_doc == null || !_doc.HasUnsavedChanges) return;

            var errors = CatalogValidator.Check(_doc, false).Count(i => i.Severity == CatalogIssueSeverity.Error);
            var changes = _doc.DescribePendingChanges();
            var text = "Save these changes to " + _doc.RootFolder + "?\n\n" +
                       string.Join("\n", changes.Take(20).Select(c => "• " + c)) +
                       (changes.Count > 20 ? $"\n… and {changes.Count - 20} more" : string.Empty) +
                       (errors > 0 ? $"\n\nThe catalog has {errors} error(s); the manager may reject the affected add-ons." : string.Empty);
            if (Dialogs.Show(this, text, "Save catalog", MessageBoxButton.OKCancel,
                    errors > 0 ? MessageBoxImage.Warning : MessageBoxImage.Question) != MessageBoxResult.OK) return;

            try
            {
                using (Dialogs.Busy()) _doc.Save();
            }
            catch (Exception e)
            {
                Dialogs.Error(this, "Save failed — some changes may not have been written", e);
            }

            _issues = CatalogValidator.Check(_doc, false);
            AfterChange(SelectedItem);
        }

        /// <summary>True if it is fine to drop the open catalog (saved, discarded, or nothing to lose).</summary>
        public bool ConfirmDiscard()
        {
            if (!HasUnsavedChanges) return true;
            var answer = Dialogs.Show(this, "The catalog has unsaved changes. Save them?", "Unsaved changes",
                MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (answer == MessageBoxResult.Cancel) return false;
            if (answer == MessageBoxResult.Yes)
            {
                SaveCatalog();
                return !HasUnsavedChanges;
            }

            return true;
        }

        #endregion

        #region Add

        /// <summary>Asks for a package (or a build folder to pack) and validates it. Null if cancelled or rejected.</summary>
        private string ChoosePackage(bool fromFolder, string expectedId, out PackageValidationResult validation)
        {
            validation = null;
            string path;
            if (fromFolder)
            {
                var folder = Dialogs.BrowseFolder(this, "Built add-on folder (the .adc, the DLL and, if it has one, addon.json), e.g. bin\\Release");
                if (folder == null) return null;
                try
                {
                    var outDir = Path.Combine(Path.GetTempPath(), "AlibreCatalogManager", Guid.NewGuid().ToString("N"));
                    path = PackageBuilder.Pack(folder, outDir, out var manifestCreated);
                    if (manifestCreated)
                        Dialogs.Show(this, $"The folder had no {Packages.ManifestEntry}, so one was built from its .adc and written to\n\n" +
                                           Path.Combine(folder, Packages.ManifestEntry) +
                                           "\n\nCheck its version; it has no minAlibreBuild. Edit it and pack again if anything is wrong.",
                            Packages.ManifestEntry + " created", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception e)
                {
                    Dialogs.Error(this, "Cannot pack the folder", e);
                    return null;
                }
            }
            else
            {
                path = Dialogs.OpenFile(this, "Add-on package", "Alibre add-on package (*.alibreaddon)|*.alibreaddon");
                if (path == null) return null;
            }

            validation = ValidatePackage(path, expectedId);
            return validation == null ? null : path;
        }

        /// <summary>Validates a package, showing errors and asking about warnings. Null if rejected.</summary>
        private PackageValidationResult ValidatePackage(string path, string expectedId)
        {
            var validation = Packages.Validate(path, expectedId);
            if (!validation.IsValid)
            {
                Dialogs.Show(this, "The package is not valid:\n\n" + string.Join("\n", validation.Errors.Select(e => "• " + e)),
                    "Invalid package", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            if (validation.Warnings.Count > 0 &&
                Dialogs.Show(this, "The package has warnings:\n\n" + string.Join("\n", validation.Warnings.Select(w => "• " + w)) + "\n\nAdd it anyway?",
                    "Package warnings", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return null;

            return validation;
        }

        /// <summary>
        /// Publishes a package to the open catalog — as a new add-on, or as a new
        /// version of the one with the same id — then offers to save. The package
        /// is snapshotted first, so a rebuild before the save cannot change what is
        /// copied in after its sha256 was recorded.
        /// </summary>
        /// <returns>True if the package was added and the catalog saved.</returns>
        public bool Publish(string packagePath, string releaseNotes)
        {
            if (_doc == null) throw new InvalidOperationException("Open or create a catalog first.");

            string snapshot;
            try
            {
                var folder = Path.Combine(Path.GetTempPath(), "AlibreCatalogManager", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(folder);
                snapshot = Path.Combine(folder, Path.GetFileName(packagePath));
                File.Copy(packagePath, snapshot);
            }
            catch (Exception e)
            {
                Dialogs.Error(this, "Cannot read the package", e);
                return false;
            }

            var validation = ValidatePackage(snapshot, null);
            if (validation == null) return false;

            CatalogItem item;
            AddOnVersion version;
            try
            {
                item = _doc.Find(validation.Manifest.Id);
                if (item != null)
                {
                    if (!item.IsEditable)
                        throw new InvalidOperationException($"{item.Name} is in the catalog but its manifest cannot be edited: {item.ManifestError}");
                    version = _doc.AddVersion(item, snapshot, validation);
                }
                else
                {
                    item = _doc.AddAddOn(snapshot, validation);
                    version = item.LatestVersion;
                }

                if (!string.IsNullOrWhiteSpace(releaseNotes)) version.ReleaseNotes = releaseNotes.Trim();
            }
            catch (Exception e)
            {
                Dialogs.Error(this, "Cannot publish to the catalog", e);
                return false;
            }

            if (!item.Matches(SearchBox.Text)) SearchBox.Text = string.Empty;
            AfterChange(item, version);
            SaveCatalog();
            return !HasUnsavedChanges;
        }

        private void AddAddOn(bool fromFolder)
        {
            if (_doc == null) return;
            var path = ChoosePackage(fromFolder, null, out var validation);
            if (path == null) return;

            try
            {
                var existing = _doc.Find(validation.Manifest.Id);
                if (existing != null)
                {
                    if (Dialogs.Show(this, $"{existing.Name} is already in the catalog. Add this package as version {validation.Manifest.Version} of it?",
                            "Add-on already in catalog", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                    var version = _doc.AddVersion(existing, path, validation);
                    AfterChange(existing, version);
                    return;
                }

                var item = _doc.AddAddOn(path, validation);
                AfterChange(item, item.LatestVersion);
            }
            catch (Exception e)
            {
                Dialogs.Error(this, "Cannot add the add-on", e);
            }
        }

        private void AddVersion(bool fromFolder)
        {
            var item = SelectedItem;
            if (item == null || !item.IsEditable) return;
            var path = ChoosePackage(fromFolder, item.Id, out var validation);
            if (path == null) return;

            try
            {
                var version = _doc.AddVersion(item, path, validation);
                AfterChange(item, version);
            }
            catch (Exception e)
            {
                Dialogs.Error(this, "Cannot add the version", e);
            }
        }

        #endregion

        #region Remove / delete

        private void ToggleListed()
        {
            var item = SelectedItem;
            if (item == null) return;
            try
            {
                if (item.Listed) _doc.Unlist(item);
                else _doc.Relist(item);
                AfterChange(item);
            }
            catch (Exception e)
            {
                Dialogs.Error(this, "Cannot change the listing", e);
            }
        }

        private void DeleteAddOn()
        {
            var item = SelectedItem;
            if (item == null) return;

            var packages = item.Manifest != null ? _doc.DeletablePackages(item, item.Manifest.Versions.Where(v => v != null)) : new List<string>();
            var text = $"Delete {item.Name} from the catalog?\n\nIts index entry and manifest are deleted on save. " +
                       "Users who already installed it keep it, but can no longer update it from this catalog.";
            var deletePackages = false;
            if (packages.Count > 0)
            {
                var answer = Dialogs.Show(this, text + $"\n\nAlso delete its {packages.Count} package file(s)?\n\n" +
                                                string.Join("\n", packages.Select(p => "• " + _doc.RelativeUrl(p))) +
                                                "\n\nYes = delete add-on and packages, No = keep the packages",
                    "Delete add-on", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (answer == MessageBoxResult.Cancel) return;
                deletePackages = answer == MessageBoxResult.Yes;
            }
            else if (Dialogs.Show(this, text, "Delete add-on", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
                return;

            _doc.Delete(item, deletePackages);
            _issues.RemoveAll(i => i.Item == item);
            AfterChange(null);
        }

        private void RemoveVersion()
        {
            var item = SelectedItem;
            var version = SelectedVersion;
            if (item == null || version == null) return;

            var packages = _doc.DeletablePackages(item, new[] { version });
            var text = $"Remove version {version.Version} of {item.Name}?";
            var deletePackage = false;
            if (packages.Count > 0)
            {
                var answer = Dialogs.Show(this, text + $"\n\nAlso delete its package {_doc.RelativeUrl(packages[0])}?\n\nYes = remove and delete, No = keep the file",
                    "Remove version", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (answer == MessageBoxResult.Cancel) return;
                deletePackage = answer == MessageBoxResult.Yes;
            }
            else if (Dialogs.Show(this, text, "Remove version", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                return;

            try
            {
                _doc.RemoveVersion(item, version, deletePackage);
                AfterChange(item);
            }
            catch (Exception e)
            {
                Dialogs.Error(this, "Cannot remove the version", e);
            }
        }

        private void Rehash()
        {
            var item = SelectedItem;
            var version = SelectedVersion;
            if (item == null || version == null) return;
            if (Dialogs.Show(this, $"Recompute sha256 and size of version {version.Version} from its package file?\n\n" +
                                   "Only do this if the package was rebuilt on purpose: users who already downloaded the old file will see a hash mismatch.",
                    "Rehash", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
            try
            {
                _doc.Rehash(item, version);
                AfterChange(item, version);
            }
            catch (Exception e)
            {
                Dialogs.Error(this, "Cannot rehash", e);
            }
        }

        #endregion

        #region Check

        private void RunCheck(bool deep)
        {
            if (_doc == null) return;
            using (Dialogs.Busy()) _issues = CatalogValidator.Check(_doc, deep);

            RefreshProblems();
            RefreshList(SelectedItem);
            var errors = _issues.Count(i => i.Severity == CatalogIssueSeverity.Error);
            var warnings = _issues.Count(i => i.Severity == CatalogIssueSeverity.Warning);
            SetStatus(errors == 0 && warnings == 0 ? "Check passed: no errors or warnings." : $"Check found {errors} error(s) and {warnings} warning(s).");
        }

        private void GoToProblem()
        {
            if (!(ProblemList.SelectedItem is ProblemRow row) || row.Issue.Item == null) return;
            if (!row.Issue.Item.Matches(SearchBox.Text)) SearchBox.Text = string.Empty;
            RefreshList(row.Issue.Item);
            AddOnList.Focus();
        }

        #endregion

        #region Refresh

        /// <summary>After any change: refresh everything and reselect.</summary>
        private void AfterChange(CatalogItem select, AddOnVersion selectVersion = null)
        {
            RefreshList(select);
            if (selectVersion != null)
            {
                var row = _versionRows.FirstOrDefault(r => r.Version == selectVersion);
                if (row != null)
                {
                    VersionList.SelectedItem = row;
                    VersionList.ScrollIntoView(row);
                }
            }

            RefreshProblems();
            UpdateUi();
        }

        private void RefreshList(CatalogItem select)
        {
            _rows.Clear();
            AddOnRow selected = null;
            if (_doc != null)
            {
                var withErrors = new HashSet<CatalogItem>(_issues.Where(i => i.Severity == CatalogIssueSeverity.Error && i.Item != null).Select(i => i.Item));
                foreach (var item in _doc.Items.Where(i => i.Matches(SearchBox.Text)))
                {
                    var row = new AddOnRow(item, withErrors.Contains(item) ? ErrorBrush : !item.Listed ? SystemColors.GrayTextBrush : null);
                    _rows.Add(row);
                    if (item == select) selected = row;
                }
            }

            AddOnList.SelectedItem = selected;
            if (selected != null) AddOnList.ScrollIntoView(selected);
            ShowDetails();
        }

        private void RefreshProblems()
        {
            _problemRows.Clear();
            foreach (var issue in _issues.OrderBy(i => i.Severity).ThenBy(i => i.Item?.Name))
                _problemRows.Add(new ProblemRow(issue,
                    issue.Severity == CatalogIssueSeverity.Error ? ErrorBrush
                    : issue.Severity == CatalogIssueSeverity.Info ? SystemColors.GrayTextBrush
                    : null));
        }

        private void ShowDetails()
        {
            var item = SelectedItem;
            AddOnEditorView.SelectedObject = item == null ? null : new AddOnEditor(_doc, item, () => OnEdited(item));
            AddOnEditorView.IsEnabled = item != null && item.IsEditable;

            _versionRows.Clear();
            if (item != null)
                foreach (var v in item.VersionsNewestFirst)
                    _versionRows.Add(new VersionRow(v));

            ShowVersion();
            UpdateUi();
        }

        private void ShowVersion()
        {
            var item = SelectedItem;
            var version = SelectedVersion;
            VersionEditorView.SelectedObject = item != null && version != null && item.IsEditable
                ? new VersionEditor(_doc, item, version, () => OnEdited(item))
                : null;
            UpdateUi();
        }

        /// <summary>A property edit: update the rows in place rather than rebuilding (keeps the editor's focus).</summary>
        private void OnEdited(CatalogItem item)
        {
            _doc.SyncLatestVersion(item);
            foreach (var row in _rows.Where(r => r.Item == item)) row.Refresh();
            foreach (var row in _versionRows) row.Refresh();
            UpdateUi();
        }

        private void UpdateUi()
        {
            var open = _doc != null;
            var item = SelectedItem;
            SaveButton.IsEnabled = open && _doc.HasUnsavedChanges;
            ReloadButton.IsEnabled = open;
            AddAddOnButton.IsEnabled = open;
            CheckButton.IsEnabled = open;
            AddVersionButton.IsEnabled = item != null && item.IsEditable;
            ListToggleButton.IsEnabled = item != null;
            ListToggleButton.Content = item != null && !item.Listed ? "Re-list" : "Remove from listing";
            ListToggleButton.ToolTip = item != null && !item.Listed
                ? "Put the add-on back in index.json"
                : "Take the add-on out of index.json; its manifest and packages stay so it can be re-listed";
            DeleteButton.IsEnabled = item != null;
            RemoveVersionButton.IsEnabled = item != null && item.IsEditable && SelectedVersion != null;
            RehashButton.IsEnabled = RemoveVersionButton.IsEnabled;
            CatalogChanged?.Invoke(this, EventArgs.Empty);

            if (!open)
            {
                SetStatus("Open a catalog folder (index.json), or create a new one.");
                return;
            }

            var listed = _doc.Items.Count(i => i.Listed);
            SetStatus($"{_doc.RootFolder} — {listed} listed, {_doc.Items.Count - listed} unlisted" +
                      (_doc.HasUnsavedChanges ? " — unsaved changes" : string.Empty));
        }

        private void SetStatus(string text) => StatusText.Text = text;

        #endregion

        #region Rows

        /// <summary>Base for list rows: a foreground brush (null = default) and change notification.</summary>
        public abstract class Row : INotifyPropertyChanged
        {
            protected Row(Brush foreground)
            {
                Foreground = foreground ?? SystemColors.ControlTextBrush;
            }

            public Brush Foreground { get; }

            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>Re-reads every displayed value.</summary>
            public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        public sealed class AddOnRow : Row
        {
            public AddOnRow(CatalogItem item, Brush foreground) : base(foreground)
            {
                Item = item;
            }

            public CatalogItem Item { get; }
            public string Name => Item.Name;
            public string Latest => Item.LatestVersion?.Version ?? string.Empty;
            public string Publisher => Item.Publisher ?? string.Empty;
            public string Status => Item.Listed ? "Listed" : "Unlisted";
        }

        public sealed class VersionRow : Row
        {
            public VersionRow(AddOnVersion version) : base(null)
            {
                Version = version;
            }

            public AddOnVersion Version { get; }
            public string Number => Version.Version;
            public string Released => Version.Released ?? string.Empty;

            public string Builds =>
                Version.MinAlibreBuild == null && Version.MaxAlibreBuild == null
                    ? "any"
                    : $"{Version.MinAlibreBuild?.ToString() ?? "…"}–{Version.MaxAlibreBuild?.ToString() ?? "…"}";
        }

        public sealed class ProblemRow : Row
        {
            public ProblemRow(CatalogIssue issue, Brush foreground) : base(foreground)
            {
                Issue = issue;
            }

            public CatalogIssue Issue { get; }
            public string Severity => Issue.Severity.ToString();
            public string AddOn => Issue.Item?.Name ?? "(catalog)";
            public string Message => Issue.Message;
        }

        #endregion
    }
}
