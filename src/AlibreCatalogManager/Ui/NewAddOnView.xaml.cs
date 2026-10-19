using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AlibreAddOnManager;
using AlibreCatalogManager.Generation;

namespace AlibreCatalogManager.Ui
{
    /// <summary>Form for generating a new, empty Alibre add-on project in C# or C++.</summary>
    public partial class NewAddOnView : UserControl
    {
        /// <summary>The language choices, in display order.</summary>
        private static readonly (AddOnLanguage Language, string Text)[] Languages =
        {
            (AddOnLanguage.CSharp, "C# — managed .NET Framework DLL"),
            (AddOnLanguage.Cpp, "C++ — native x64 DLL")
        };

        /// <summary>Icon colours offered in the colour popup; any other can be typed as hex.</summary>
        private static readonly Color[] Palette =
        {
            Color.FromRgb(30, 110, 180), Color.FromRgb(0, 120, 215), Color.FromRgb(0, 153, 188), Color.FromRgb(0, 140, 110),
            Color.FromRgb(16, 124, 16), Color.FromRgb(107, 142, 35), Color.FromRgb(202, 80, 16), Color.FromRgb(232, 17, 35),
            Color.FromRgb(194, 57, 179), Color.FromRgb(135, 100, 184), Color.FromRgb(92, 45, 145), Color.FromRgb(118, 118, 118),
            Color.FromRgb(73, 76, 80), Color.FromRgb(45, 45, 48), Color.FromRgb(255, 185, 0), Color.FromRgb(126, 115, 95)
        };

        private Guid _guid = Guid.NewGuid();
        private Color _color = Color.FromRgb(30, 110, 180);
        private string _solutionPath;

        // The last values derived from the project name, so a user edit is never overwritten.
        private string _autoFriendlyName = string.Empty;
        private string _autoMenuText = string.Empty;
        private bool _initialised;

        public NewAddOnView()
        {
            InitializeComponent();

            foreach (var language in Languages) LanguageBox.Items.Add(language.Text);
            LanguageBox.SelectedIndex = 0;
            foreach (var workspace in AddOnSpec.Workspaces) WorkspaceBox.Items.Add(workspace);
            WorkspaceBox.SelectedItem = "Always";
            AuthorBox.Text = Environment.UserName;
            IdBox.Text = _guid.ToString("B").ToUpperInvariant();
            BuildSwatches();
            UpdateIconButton();

            FillAlibreFolders();
            OutputRootBox.Text = DefaultOutputRoot();
            _initialised = true;
            UpdateSolution();
            UpdateTarget();
        }

        /// <summary>Raised with the new .csproj or .vcxproj after a project is generated.</summary>
        public event Action<string> ProjectGenerated;

        private AddOnLanguage SelectedLanguage => Languages[Math.Max(0, LanguageBox.SelectedIndex)].Language;

        #region Events

        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initialised) UpdateTarget();
        }

        private void OnNameChanged(object sender, TextChangedEventArgs e)
        {
            if (!_initialised) return;
            DeriveNames();
            UpdateTarget();
        }

        private void OnOutputRootChanged(object sender, TextChangedEventArgs e)
        {
            if (!_initialised) return;
            UpdateSolution();
            UpdateTarget();
        }

        private void OnNewId(object sender, RoutedEventArgs e)
        {
            _guid = Guid.NewGuid();
            IdBox.Text = _guid.ToString("B").ToUpperInvariant();
        }

        private void OnBrowseAlibre(object sender, RoutedEventArgs e)
        {
            var folder = Dialogs.BrowseFolder(this, "Alibre Design's Program folder (contains AlibreX.dll)", AlibreDirBox.Text);
            if (folder != null) AlibreDirBox.Text = folder;
        }

        private void OnBrowseOutput(object sender, RoutedEventArgs e)
        {
            var folder = Dialogs.BrowseFolder(this, "Folder to create the project folder in", OutputRootBox.Text);
            if (folder != null) OutputRootBox.Text = folder;
        }

        private void OnPickColor(object sender, RoutedEventArgs e)
        {
            HexBox.Text = $"{_color.R:X2}{_color.G:X2}{_color.B:X2}";
            ColorPopup.IsOpen = true;
        }

        private void OnApplyHex(object sender, RoutedEventArgs e) => ApplyHex();

        private void OnHexKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ApplyHex();
        }

        private void OnGenerate(object sender, RoutedEventArgs e) => Generate();

        #endregion

        #region Defaults

        private void FillAlibreFolders()
        {
            try
            {
                // Ordered oldest first; default to the newest. Compile against the oldest
                // version you intend to support if several are installed.
                var folders = AlibreInstallDetector.Find().Select(i => Path.Combine(i.Folder, "Program")).ToList();
                foreach (var folder in folders) AlibreDirBox.Items.Add(folder);
                if (folders.Count > 0) AlibreDirBox.Text = folders.Last();
            }
            catch (Exception)
            {
                // No detection: the user browses.
            }
        }

        /// <summary>The repository's src folder when run from a build tree, else My Documents.</summary>
        private static string DefaultOutputRoot()
        {
            for (var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory); dir != null; dir = dir.Parent)
            {
                var src = Path.Combine(dir.FullName, "src");
                if (Directory.Exists(src) && dir.GetFiles("*.slnx").Length > 0) return src;
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        /// <summary>"MyCoolTool" → "My Cool Tool", until the user types their own.</summary>
        private void DeriveNames()
        {
            var spaced = Regex.Replace(NameBox.Text.Replace('_', ' '), "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ").Trim();
            if (FriendlyNameBox.Text == _autoFriendlyName) FriendlyNameBox.Text = spaced;
            if (MenuTextBox.Text == _autoMenuText) MenuTextBox.Text = spaced;
            _autoFriendlyName = spaced;
            _autoMenuText = spaced;
        }

        private void UpdateSolution()
        {
            var root = OutputRootBox.Text.Trim();
            _solutionPath = Directory.Exists(root) ? SolutionFile.FindNearest(root) : null;
            AddToSolutionBox.IsEnabled = _solutionPath != null;
            AddToSolutionBox.IsChecked = _solutionPath != null;
            AddToSolutionBox.Content = _solutionPath != null
                ? "Add the project to " + Path.GetFileName(_solutionPath)
                : "Add the project to a solution (no .slnx above the output folder)";
            AddToSolutionBox.ToolTip = _solutionPath;
        }

        private void UpdateTarget()
        {
            var root = OutputRootBox.Text.Trim();
            var name = NameBox.Text.Trim();
            var project = name + (SelectedLanguage == AddOnLanguage.Cpp ? ".vcxproj" : ".csproj");
            TargetText.Text = name.Length > 0 && root.Length > 0 ? "Creates " + Path.Combine(root, name, project) : string.Empty;
        }

        #endregion

        #region Icon colour

        private void BuildSwatches()
        {
            foreach (var color in Palette)
            {
                var swatch = new Button
                {
                    Width = 22,
                    Height = 22,
                    MinWidth = 0,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0, 0, 3, 3),
                    Background = new SolidColorBrush(color),
                    ToolTip = $"#{color.R:X2}{color.G:X2}{color.B:X2}"
                };
                swatch.Click += (s, e) => SetColor(color);
                Swatches.Children.Add(swatch);
            }
        }

        private void ApplyHex()
        {
            var hex = HexBox.Text.Trim().TrimStart('#');
            if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            {
                Dialogs.Show(this, "Enter the colour as six hex digits, RRGGBB, e.g. 1E6EB4.", "Icon colour",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetColor(Color.FromRgb((byte) (rgb >> 16), (byte) (rgb >> 8), (byte) rgb));
        }

        private void SetColor(Color color)
        {
            _color = color;
            ColorPopup.IsOpen = false;
            UpdateIconButton();
        }

        private void UpdateIconButton()
        {
            IconColorButton.Background = new SolidColorBrush(_color);
            // Perceived brightness, as System.Drawing.Color.GetBrightness was used for before.
            var brightness = (Math.Max(_color.R, Math.Max(_color.G, _color.B)) + Math.Min(_color.R, Math.Min(_color.G, _color.B))) / 510.0;
            IconColorButton.Foreground = brightness > 0.6 ? Brushes.Black : Brushes.White;
        }

        #endregion

        #region Generate

        private void Generate()
        {
            if (!int.TryParse(MinBuildBox.Text.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out var minBuild))
                minBuild = 0; // AddOnSpec.Validate reports it.

            var spec = new AddOnSpec
            {
                Language = SelectedLanguage,
                Name = NameBox.Text.Trim(),
                FriendlyName = FriendlyNameBox.Text,
                MenuText = MenuTextBox.Text,
                AuthorName = AuthorBox.Text,
                AuthorLink = AuthorLinkBox.Text,
                Description = DescriptionBox.Text,
                Workspace = (string) WorkspaceBox.SelectedItem,
                Version = VersionBox.Text.Trim(),
                MinAlibreBuild = minBuild,
                Id = _guid,
                AlibreProgramDir = AlibreDirBox.Text.Trim(),
                OutputRoot = OutputRootBox.Text.Trim(),
                IncludeDeployScripts = DeployScriptsBox.IsChecked == true,
                SolutionPath = AddToSolutionBox.IsEnabled && AddToSolutionBox.IsChecked == true ? _solutionPath : null,
                IconColor = System.Drawing.Color.FromArgb(_color.R, _color.G, _color.B)
            };

            var errors = spec.Validate();
            if (errors.Count > 0)
            {
                Dialogs.Show(this, string.Join(Environment.NewLine, errors.Select(e => "• " + e)),
                    "Cannot generate the add-on", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            GenerationResult result;
            try
            {
                using (Dialogs.Busy()) result = AddOnGenerator.Generate(spec);
            }
            catch (Exception e)
            {
                Dialogs.Error(this, "Generation failed", e);
                return;
            }

            var summary = new StringBuilder();
            summary.AppendLine("Created " + result.ProjectFolder + ":").AppendLine();
            foreach (var file in result.Files) summary.AppendLine("    " + file);
            summary.AppendLine();
            if (result.SolutionUpdated != null) summary.AppendLine("Added to " + result.SolutionUpdated + ".").AppendLine();
            summary.AppendLine("Identifier " + spec.AdcId);
            if (spec.Language == AddOnLanguage.Cpp)
                summary.AppendLine().AppendLine("Build it with MSBuild from Visual Studio or Build Tools (C++ workload); Rider's bundled MSBuild has no C++ targets.");
            summary.AppendLine().AppendLine("Build, package and publish it from the Package & Publish tab.");
            summary.AppendLine().Append("Open the project folder?");

            ProjectGenerated?.Invoke(result.ProjectFile);
            if (Dialogs.Show(this, summary.ToString(), "Add-on created", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                Process.Start("explorer.exe", "\"" + result.ProjectFolder + "\"");

            // A second add-on must never reuse this one's identifier.
            _guid = Guid.NewGuid();
            IdBox.Text = _guid.ToString("B").ToUpperInvariant();
        }

        #endregion
    }
}
