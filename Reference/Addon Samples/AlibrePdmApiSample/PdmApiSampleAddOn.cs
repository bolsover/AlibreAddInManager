using System;
using System.Windows.Forms;
using AlibreAddOn;
using AlibreX;

namespace AlibrePdmApiSample
{
    /// <summary>
    /// IAlibreAddOn for the PDM API sample: one root menu with one stateless
    /// command that opens the PDM browser modally and returns no command object.
    /// Available in every workspace, including Home — it never needs a session.
    /// </summary>
    public class PdmApiSampleAddOn : IAlibreAddOn
    {
        public const string Title = "Alibre PDM API Sample";

        /// <summary>The PDM API (IADRoot.ConnectToPDM and friends) is V29+.</summary>
        public const int MinimumBuild = 29000;

        private const int MenuIdRoot = 7401;
        private const int MenuIdBrowse = 7402;
        private static readonly int[] SubMenuIds = { MenuIdBrowse };

        private readonly IADRoot _root;
        private readonly IntPtr _parentWindow;
        private readonly string _version;
        private readonly bool _supported;

        public PdmApiSampleAddOn(IADRoot root, IntPtr parentWindow)
        {
            _root = root;
            _parentWindow = parentWindow;

            try { _version = _root?.Version; }
            catch (Exception) { _version = null; }
            _supported = ParseBuild(_version) >= MinimumBuild;
        }

        public int RootMenuItem => MenuIdRoot;

        public bool HasSubMenus(int menuId) => menuId == MenuIdRoot;

        public Array SubMenuItems(int menuId) => menuId == MenuIdRoot ? SubMenuIds : new int[0];

        public string MenuItemText(int menuId)
        {
            switch (menuId)
            {
                case MenuIdRoot: return "PDM API Sample";
                case MenuIdBrowse: return "Browse PDM";
                default: return string.Empty;
            }
        }

        public bool PopupMenu(int menuId) => menuId == MenuIdRoot;

        // Root stays enabled so the tooltip can explain a disabled leaf. Never
        // touches Sessions — the Home window passes an empty session identifier.
        public ADDONMenuStates MenuItemState(int menuId, string sessionIdentifier)
        {
            if (menuId == MenuIdBrowse && !_supported) return ADDONMenuStates.ADDON_MENU_GRAYED;
            return ADDONMenuStates.ADDON_MENU_ENABLED;
        }

        public string MenuItemToolTip(int menuId)
        {
            if (menuId != MenuIdBrowse) return string.Empty;
            return _supported
                ? "Connect to a PDM server and browse safes, folders, files and properties"
                : $"Requires Alibre Design build {MinimumBuild} or later (this is {_version ?? "unknown"})";
        }

        /// <summary>Icon file name, looked up in the folder holding the .adc.</summary>
        public string MenuIcon(int menuId) => "AlibrePdmApiSample.ico";

        public bool HasPersistentDataToSave(string sessionIdentifier) => false;

        public IAlibreAddOnCommand InvokeCommand(int menuId, string sessionIdentifier)
        {
            if (menuId != MenuIdBrowse || !_supported) return null;
            try
            {
                using (var form = new MainForm(_root))
                    form.ShowDialog(new WindowWrapper(_parentWindow));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return null;
        }

        public void LoadData(IStream pCustomData, string sessionIdentifier)
        {
        }

        public void SaveData(IStream pCustomData, string sessionIdentifier)
        {
        }

        public void setIsAddOnLicensed(bool isLicensed)
        {
        }

        public bool UseDedicatedRibbonTab() => false;

        /// <summary>
        /// Build number from IADRoot.Version: the last comma- or dot-separated
        /// numeric field ("29,1,0,29126" → 29126). Returns 0 if it cannot be parsed.
        /// </summary>
        private static int ParseBuild(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return 0;
            var fields = version.Split(new[] { ',', '.', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = fields.Length - 1; i >= 0; i--)
                if (int.TryParse(fields[i].Trim(), out var build))
                    return build;
            return 0;
        }
    }
}
