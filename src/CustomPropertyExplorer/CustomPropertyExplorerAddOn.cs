using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AlibreAddOn;
using AlibreX;
using CustomPropertyExplorer.Model;
using CustomPropertyExplorer.Ui;

namespace CustomPropertyExplorer
{
    /// <summary>
    /// IAlibreAddOn for the explorers: one root menu with two stateless commands.
    /// <list type="bullet">
    /// <item><b>Property Explorer</b> — model tree, property grid, live sync with the
    /// Alibre selection and custom-property lookup/set (PropertyExplorerCustomProperties.py,
    /// a superset of PropertyExplorer.py). Modeless, so the user can keep selecting in
    /// Alibre; at most one window per workspace.</item>
    /// <item><b>Custom Property Explorer</b> — custom properties only
    /// (CustomPropertyExplorer.py). Modal.</item>
    /// </list>
    /// Loaded in every workspace; the commands are enabled in Part, Sheet Metal and
    /// Assembly workspaces.
    /// </summary>
    public class CustomPropertyExplorerAddOn : IAlibreAddOn
    {
        public const string Title = "Property Explorer";

        private const int MenuIdRoot = 7501;
        private const int MenuIdPropertyExplorer = 7502;
        private const int MenuIdCustomPropertyExplorer = 7503;
        private static readonly int[] SubMenuIds = { MenuIdPropertyExplorer, MenuIdCustomPropertyExplorer };

        private readonly IADRoot _root;
        private readonly IntPtr _parentWindow;
        private readonly int _build;

        /// <summary>Open modeless explorers, keyed by session identifier.</summary>
        private readonly Dictionary<string, PropertyExplorerForm> _explorers = new Dictionary<string, PropertyExplorerForm>();

        public CustomPropertyExplorerAddOn(IADRoot root, IntPtr parentWindow)
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
                case MenuIdRoot: return "Property Explorer";
                case MenuIdPropertyExplorer: return "Property Explorer";
                case MenuIdCustomPropertyExplorer: return "Custom Property Explorer";
                default: return string.Empty;
            }
        }

        public bool PopupMenu(int menuId) => menuId == MenuIdRoot;

        public ADDONMenuStates MenuItemState(int menuId, string sessionIdentifier)
        {
            // The root stays enabled so the drop-down (and the tooltips explaining
            // why a command is greyed) can always be opened.
            if (menuId == MenuIdRoot) return ADDONMenuStates.ADDON_MENU_ENABLED;
            if (!SubMenuIds.Contains(menuId) || !IsSupportedVersion) return ADDONMenuStates.ADDON_MENU_GRAYED;
            return DesignContext.TryResolve(_root, sessionIdentifier) != null
                ? ADDONMenuStates.ADDON_MENU_ENABLED
                : ADDONMenuStates.ADDON_MENU_GRAYED;
        }

        public string MenuItemToolTip(int menuId)
        {
            if (!SubMenuIds.Contains(menuId)) return string.Empty;
            if (!IsSupportedVersion)
                return "Requires Alibre Design build " + AlibreVersion.MinimumBuild + " or later (this is build " + _build + ")";
            return menuId == MenuIdPropertyExplorer
                ? "Browse the model structure and the properties of any item, synced with the Alibre selection (Part or Assembly)"
                : "View, look up and set the custom properties of the part or assembly";
        }

        /// <summary>Icon file name, looked up in the folder holding the .adc.</summary>
        public string MenuIcon(int menuId) => "CustomPropertyExplorer.ico";

        public bool HasPersistentDataToSave(string sessionIdentifier) => false;

        public IAlibreAddOnCommand InvokeCommand(int menuId, string sessionIdentifier)
        {
            if (!SubMenuIds.Contains(menuId)) return null;
            try
            {
                if (!IsSupportedVersion)
                {
                    MessageBox.Show(MenuItemToolTip(menuId), Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }

                var context = DesignContext.TryResolve(_root, sessionIdentifier);
                if (context == null)
                {
                    MessageBox.Show("No active part or assembly found.\n\nPlease open or create a part/assembly first.", Title,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return null;
                }

                if (menuId == MenuIdPropertyExplorer)
                    ShowPropertyExplorer(context);
                else
                    using (var form = new CustomPropertyExplorerForm(context))
                        form.ShowDialog(new WindowWrapper(_parentWindow));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return null;
        }

        private void ShowPropertyExplorer(DesignContext context)
        {
            PropertyExplorerForm form;
            if (_explorers.TryGetValue(context.SessionIdentifier, out form) && !form.IsDisposed)
            {
                if (form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;
                form.Activate();
                return;
            }

            form = new PropertyExplorerForm(_root, context);
            var key = context.SessionIdentifier;
            form.FormClosed += (s, e) => _explorers.Remove(key);
            _explorers[key] = form;
            form.Show(new WindowWrapper(_parentWindow));
        }

        /// <summary>Closes every modeless explorer; called when the add-on unloads.</summary>
        public void CloseAllWindows()
        {
            foreach (var form in _explorers.Values.ToList())
                if (!form.IsDisposed) form.Close();
            _explorers.Clear();
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
