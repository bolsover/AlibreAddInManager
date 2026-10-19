using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AlibreAddOnManager;
using AlibreCatalogManager.Catalog;
using AlibreCatalogManager.Publishing;

namespace AlibreCatalogManager.Ui
{
    /// <summary>
    /// Build an add-on project, pack its build output into a .alibreaddon, and
    /// publish the package to the catalog open on the Catalog tab — as a new
    /// add-on, or as a new version of an existing one.
    /// </summary>
    public partial class PublishView : UserControl
    {
        private readonly AppSettings _settings;
        private readonly CatalogView _catalog;
        private readonly Action _showCatalog;

        private AddOnProject _addOn;
        private string _packagePath;
        private CancellationTokenSource _building;

        public PublishView(AppSettings settings, CatalogView catalog, Action showCatalog)
        {
            _settings = settings;
            _catalog = catalog;
            _showCatalog = showCatalog;

            InitializeComponent();
            _catalog.CatalogChanged += (s, e) => UpdateUi();

            // Before the tab is first shown, so a project generated on the New Add-On tab
            // is not replaced by the last project.
            FillMsBuild();
            if (!string.IsNullOrEmpty(_settings.LastProject) && File.Exists(_settings.LastProject))
                LoadProject(_settings.LastProject);
            UpdateUi();
        }

        public bool IsBusy => _building != null;

        /// <summary>Selects a project, e.g. one the New Add-On tab just generated.</summary>
        public void SetProject(string projectFile) => LoadProject(projectFile);

        #region Events

        private void OnProjectLostFocus(object sender, RoutedEventArgs e) => LoadProject(ProjectBox.Text.Trim());

        private void OnProjectKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) LoadProject(ProjectBox.Text.Trim());
        }

        private void OnBrowseProject(object sender, RoutedEventArgs e) => BrowseProject();

        private void OnBrowseMsBuild(object sender, RoutedEventArgs e)
        {
            var path = Dialogs.OpenFile(this, "MSBuild.exe", "MSBuild (MSBuild.exe)|MSBuild.exe");
            if (path != null) MsBuildBox.Text = path;
        }

        private void OnBrowsePackageFolder(object sender, RoutedEventArgs e)
        {
            var folder = Dialogs.BrowseFolder(this, "Folder to write the .alibreaddon package to", PackageFolderBox.Text);
            if (folder != null) PackageFolderBox.Text = folder;
        }

        private async void OnBuildAndPackage(object sender, RoutedEventArgs e) => await BuildAndPackageAsync();

        private void OnCancel(object sender, RoutedEventArgs e) => _building?.Cancel();

        private void OnShowPackage(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_packagePath)) Process.Start("explorer.exe", "/select,\"" + _packagePath + "\"");
        }

        private void OnOpenCatalog(object sender, RoutedEventArgs e) => _catalog.BrowseAndOpen();

        private void OnNewCatalog(object sender, RoutedEventArgs e) => _catalog.CreateCatalog();

        private void OnPublish(object sender, RoutedEventArgs e) => Publish();

        #endregion

        #region Project

        private void FillMsBuild()
        {
            var found = MsBuildLocator.Find();
            foreach (var path in found) MsBuildBox.Items.Add(path);
            MsBuildBox.Text = !string.IsNullOrEmpty(_settings.MsBuildPath) && File.Exists(_settings.MsBuildPath)
                ? _settings.MsBuildPath
                : found.FirstOrDefault() ?? string.Empty;
        }

        private void BrowseProject()
        {
            var initial = _addOn?.Folder ?? (File.Exists(_settings.LastProject) ? Path.GetDirectoryName(_settings.LastProject) : null);
            var path = Dialogs.OpenFile(this, "Add-on project",
                "Add-on project (*.csproj;*.vcxproj)|*.csproj;*.vcxproj|C# project (*.csproj)|*.csproj|C++ project (*.vcxproj)|*.vcxproj",
                initial);
            if (path != null) LoadProject(path);
        }

        private void LoadProject(string projectFile)
        {
            if (IsBusy) return;
            if (string.IsNullOrEmpty(projectFile))
            {
                _addOn = null;
                ProjectInfo.Text = string.Empty;
                UpdateUi();
                return;
            }

            if (_addOn != null && string.Equals(_addOn.ProjectFile, Path.GetFullPath(projectFile), StringComparison.OrdinalIgnoreCase))
                return;

            ProjectBox.Text = projectFile;
            try
            {
                _addOn = AddOnProject.Load(projectFile);
                var m = _addOn.Manifest;
                ProjectInfo.Text = $"{m.Name} — {ManagerPaths.NormalizeId(m.Id)} — folder {m.FolderName}, {m.DllType ?? "?"} {m.Arch ?? ""}".TrimEnd();
                ProjectInfo.Foreground = SystemColors.GrayTextBrush;
                VersionBox.Text = m.Version ?? string.Empty;
                PackageFolderBox.Text = Path.Combine(_addOn.Folder, "bin", "Packages");
                _settings.LastProject = _addOn.ProjectFile;
                _settings.Save();
                if (_addOn.ManifestCreated)
                    Log($"The project had no {Packages.ManifestEntry}; built {_addOn.ManifestPath} from its .adc " +
                        $"(version {m.Version}, no minAlibreBuild). Check it before publishing.");
            }
            catch (Exception e)
            {
                _addOn = null;
                ProjectInfo.Text = e.Message;
                ProjectInfo.Foreground = Brushes.Firebrick;
            }

            SetPackage(null, null);
        }

        #endregion

        #region Build and package

        private async Task BuildAndPackageAsync()
        {
            if (_addOn == null || IsBusy) return;
            LogBox.Clear();
            SetPackage(null, null);

            var version = VersionBox.Text.Trim();
            if (!AddOnProject.VersionFormat.IsMatch(version))
            {
                Warn("The version must be major.minor.patch, e.g. 1.2.0.");
                return;
            }

            if (version != _addOn.Manifest.Version)
            {
                if (BuildFirstBox.IsChecked != true)
                {
                    Warn("Changing the version needs a build: the existing build output still carries version " + _addOn.Manifest.Version + ".");
                    return;
                }

                // A C++ project has no AssemblyInfo.cs; only addon.json carries its version.
                var edited = File.Exists(_addOn.AssemblyInfoPath)
                    ? $"{Packages.ManifestEntry} and Properties\\AssemblyInfo.cs"
                    : Packages.ManifestEntry;
                if (Dialogs.Show(this, $"Change the project's version from {_addOn.Manifest.Version} to {version}?\n\n" +
                                       $"This edits {edited} in {_addOn.Folder}.",
                        "Change version", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
                try
                {
                    _addOn.SetVersion(version);
                    Log($"Version set to {version} in {edited}.");
                }
                catch (Exception e)
                {
                    Warn(e.Message);
                    return;
                }
            }

            var configuration = (string) ((ComboBoxItem) ConfigurationBox.SelectedItem).Content;
            if (BuildFirstBox.IsChecked == true && !await BuildAsync(configuration)) return;

            try
            {
                var output = _addOn.PrepareOutput(configuration);
                Log("Packing " + output);
                var package = PackageBuilder.Pack(output, PackageFolderBox.Text.Trim(), out _);
                Log("Wrote " + package);

                var validation = Packages.Validate(package, _addOn.Manifest.Id);
                if (validation.Manifest != null && Packages.CompareVersions(validation.Manifest.Version, version) != 0)
                    validation.Errors.Add($"The build output's {Packages.ManifestEntry} says version {validation.Manifest.Version}, not {version}.");
                foreach (var error in validation.Errors) Log("ERROR: " + error);
                foreach (var warning in validation.Warnings) Log("Warning: " + warning);

                SetPackage(package, validation);
                Log(validation.IsValid ? "Package is valid." : "Package is NOT valid; it cannot be published.");
            }
            catch (Exception e)
            {
                Log("ERROR: " + e.Message);
                Warn(e.Message);
            }
        }

        private async Task<bool> BuildAsync(string configuration)
        {
            var msbuild = MsBuildBox.Text.Trim();
            if (!File.Exists(msbuild))
            {
                Warn("Choose MSBuild.exe (Visual Studio, Build Tools or Rider), or untick the build option.");
                return false;
            }

            _settings.MsBuildPath = msbuild;
            _settings.Save();

            if (_addOn.IsNative && msbuild.IndexOf("Rider", StringComparison.OrdinalIgnoreCase) >= 0)
                Log("Warning: Rider's bundled MSBuild has no C++ targets; a .vcxproj needs MSBuild from Visual Studio or Build Tools with the C++ workload.");

            _building = new CancellationTokenSource();
            UpdateUi();
            try
            {
                Log($"Building {Path.GetFileName(_addOn.ProjectFile)} ({configuration}) with {msbuild}");
                var code = await ProjectBuilder.BuildAsync(msbuild, _addOn.ProjectFile, configuration, LogFromAnyThread, _building.Token);
                if (code == 0) return true;
                Log($"Build failed (exit code {code}).");
                Warn("The build failed; see the output for the errors.");
                return false;
            }
            catch (TaskCanceledException)
            {
                Log("Build cancelled.");
                return false;
            }
            catch (Exception e)
            {
                Log("ERROR: " + e.Message);
                Warn("Cannot run MSBuild: " + e.Message);
                return false;
            }
            finally
            {
                _building.Dispose();
                _building = null;
                UpdateUi();
            }
        }

        private void SetPackage(string path, PackageValidationResult validation)
        {
            _packagePath = validation != null && validation.IsValid ? path : null;
            if (path == null)
                PackageInfo.Text = string.Empty;
            else
            {
                var info = new FileInfo(path);
                PackageInfo.Text = $"{info.Name} — {info.Length:N0} bytes — sha256 {Packages.Sha256File(path)}" +
                                   (validation.IsValid ? string.Empty : $" — {validation.Errors.Count} error(s)");
                PackageInfo.Foreground = validation.IsValid ? SystemColors.ControlTextBrush : Brushes.Firebrick;
            }

            UpdateUi();
        }

        #endregion

        #region Publish

        private void Publish()
        {
            if (_packagePath == null || !File.Exists(_packagePath)) return;

            if (_catalog.CatalogFolder == null)
            {
                var answer = Dialogs.Show(this, "No catalog is open.\n\nYes = open an existing catalog, No = create a new one.",
                    "Choose a catalog", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (answer == MessageBoxResult.Yes) _catalog.BrowseAndOpen();
                else if (answer == MessageBoxResult.No) _catalog.CreateCatalog();
                if (_catalog.CatalogFolder == null) return;
            }

            var hadChanges = _catalog.HasUnsavedChanges;
            if (_catalog.Publish(_packagePath, ReleaseNotesBox.Text))
            {
                Log($"Published {Path.GetFileName(_packagePath)} to {_catalog.CatalogFolder}.");
                ReleaseNotesBox.Clear();
                _showCatalog();
            }
            else if (!hadChanges && _catalog.HasUnsavedChanges)
            {
                Log("Added to the catalog but not saved; review and save it on the Catalog tab.");
                _showCatalog();
            }
        }

        #endregion

        #region Helpers

        private void UpdateUi()
        {
            var busy = IsBusy;
            ProjectBox.IsEnabled = BrowseProjectButton.IsEnabled = !busy;
            VersionBox.IsEnabled = !busy && _addOn != null;
            ConfigurationBox.IsEnabled = !busy;
            MsBuildBox.IsEnabled = BrowseMsBuildButton.IsEnabled = !busy;
            BuildFirstBox.IsEnabled = !busy;
            PackageFolderBox.IsEnabled = BrowsePackageButton.IsEnabled = !busy;
            PackButton.IsEnabled = !busy && _addOn != null;
            CancelButton.IsEnabled = busy;
            ShowPackageButton.IsEnabled = !busy && _packagePath != null;

            var folder = _catalog.CatalogFolder;
            CatalogInfo.Text = folder == null
                ? "None open — open or create one here or on the Catalog tab"
                : folder + (_catalog.HasUnsavedChanges ? " (unsaved changes)" : string.Empty);
            CatalogInfo.Foreground = folder == null ? SystemColors.GrayTextBrush : SystemColors.ControlTextBrush;
            OpenCatalogButton.IsEnabled = NewCatalogButton.IsEnabled = !busy;
            PublishButton.IsEnabled = !busy && _packagePath != null;
        }

        /// <summary>MSBuild output arrives on a worker thread.</summary>
        private void LogFromAnyThread(string line)
        {
            if (Dispatcher.CheckAccess()) Log(line);
            else Dispatcher.BeginInvoke(new Action<string>(Log), line);
        }

        private void Log(string line)
        {
            LogBox.AppendText(line + Environment.NewLine);
            LogBox.ScrollToEnd();
        }

        private void Warn(string text) => Dialogs.Show(this, text, "Package & Publish", MessageBoxButton.OK, MessageBoxImage.Warning);

        #endregion
    }
}
