using System;
using System.Windows.Forms;
using AdvancedSweepGenerator.Ui;
using AlibreAddOn;
using AlibreX;

namespace AdvancedSweepGenerator
{
    /// <summary>
    /// IAlibreAddOn for the sweep generator: one root menu with one stateless
    /// command that opens the sweep dialog modally and returns no command object.
    /// Loaded in every workspace; the command is enabled only in Part and Sheet
    /// Metal workspaces, which are the ones that expose IADPartSession.
    /// </summary>
    public class AdvancedSweepAddOn : IAlibreAddOn
    {
        public const string Title = "Advanced Sweep Generator";

        private const int MenuIdRoot = 7401;
        private const int MenuIdSweep = 7402;
        private static readonly int[] SubMenuIds = { MenuIdSweep };

        private readonly IADRoot _root;
        private readonly IntPtr _parentWindow;
        private readonly int _build;

        public AdvancedSweepAddOn(IADRoot root, IntPtr parentWindow)
        {
            _root = root;
            _parentWindow = parentWindow;
            _build = AlibreVersion.GetBuild(root);
        }

        private bool IsSupportedVersion => _build >= AlibreVersion.MinimumBuild;

        public int RootMenuItem => MenuIdRoot;

        public bool HasSubMenus(int menuId) => menuId == MenuIdRoot;

        public Array SubMenuItems(int menuId) => menuId == MenuIdRoot ? SubMenuIds : new int[0];

        public string MenuItemText(int menuId)
        {
            switch (menuId)
            {
                case MenuIdRoot: return "Advanced Sweep";
                case MenuIdSweep: return "Advanced Sweep Generator";
                default: return string.Empty;
            }
        }

        public bool PopupMenu(int menuId) => menuId == MenuIdRoot;

        public ADDONMenuStates MenuItemState(int menuId, string sessionIdentifier)
        {
            // The root stays enabled so the drop-down (and the tooltip explaining
            // why the command is greyed) can always be opened.
            if (menuId == MenuIdRoot) return ADDONMenuStates.ADDON_MENU_ENABLED;
            if (menuId != MenuIdSweep || !IsSupportedVersion) return ADDONMenuStates.ADDON_MENU_GRAYED;
            return FindPartSession(sessionIdentifier) != null
                ? ADDONMenuStates.ADDON_MENU_ENABLED
                : ADDONMenuStates.ADDON_MENU_GRAYED;
        }

        public string MenuItemToolTip(int menuId)
        {
            if (menuId != MenuIdSweep) return string.Empty;
            if (!IsSupportedVersion)
                return "Requires Alibre Design build " + AlibreVersion.MinimumBuild + " or later (this is build " + _build + ")";
            return "Sweep a circular or square, solid or hollow profile along a 2D or 3D path sketch (Part workspace)";
        }

        /// <summary>Icon file name, looked up in the folder holding the .adc.</summary>
        public string MenuIcon(int menuId) => "AdvancedSweepGenerator.ico";

        public bool HasPersistentDataToSave(string sessionIdentifier) => false;

        public IAlibreAddOnCommand InvokeCommand(int menuId, string sessionIdentifier)
        {
            if (menuId != MenuIdSweep) return null;
            try
            {
                if (!IsSupportedVersion)
                {
                    MessageBox.Show(MenuItemToolTip(MenuIdSweep), Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }

                var part = FindPartSession(sessionIdentifier);
                if (part == null)
                {
                    MessageBox.Show("Open a part to use the Advanced Sweep Generator.", Title,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return null;
                }

                using (var form = new SweepForm(_root, part))
                    form.ShowDialog(new WindowWrapper(_parentWindow));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return null;
        }

        /// <summary>
        /// Resolves the workspace a call is about. The Home window passes an empty
        /// identifier and Sessions.Item throws for unknown ones, so both yield null.
        /// Part and Sheet Metal sessions both implement IADPartSession.
        /// </summary>
        private IADPartSession FindPartSession(string sessionIdentifier)
        {
            if (_root == null || string.IsNullOrEmpty(sessionIdentifier)) return null;
            try
            {
                var session = _root.Sessions.Item(sessionIdentifier);
                if (session == null) return null;
                var type = session.SessionType;
                if (type != ADObjectSubType.AD_PART && type != ADObjectSubType.AD_SHEET_METAL) return null;
                return session as IADPartSession;
            }
            catch (Exception)
            {
                return null;
            }
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
    }
}
