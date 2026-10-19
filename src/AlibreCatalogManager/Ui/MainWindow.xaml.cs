using System.ComponentModel;
using System.Windows;

namespace AlibreCatalogManager.Ui
{
    /// <summary>
    /// Main window. Each catalog task gets its own tab: browsing and editing a
    /// catalog, creating a new, empty add-on project, and building, packaging and
    /// publishing a project to the catalog.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly AppSettings _settings = AppSettings.Load();
        private readonly CatalogView _catalog;
        private readonly PublishView _publish;

        public MainWindow()
        {
            InitializeComponent();

            _catalog = new CatalogView(_settings);
            CatalogHost.Content = _catalog;

            var generator = new NewAddOnView();
            NewAddOnHost.Content = generator;

            _publish = new PublishView(_settings, _catalog, () => Tabs.SelectedItem = CatalogTab);
            PublishHost.Content = _publish;
            generator.ProjectGenerated += project => _publish.SetProject(project);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (e.Cancel) return;

            if (_publish.IsBusy)
            {
                Dialogs.Show(this, "A build is running. Cancel it on the Package & Publish tab first.", Title,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                e.Cancel = true;
                return;
            }

            if (!_catalog.ConfirmDiscard()) e.Cancel = true;
        }
    }
}
