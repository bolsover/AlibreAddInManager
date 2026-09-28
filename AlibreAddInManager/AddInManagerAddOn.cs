using System;
using System.Windows.Forms;
using AlibreAddOn;
using AlibreAddInManager.Ui;
using AlibreX;

namespace AlibreAddInManager
{
    /// <summary>
    /// IAlibreAddOn for the add-on manager: one root menu with one stateless
    /// command that opens the manager window modally and returns no command object.
    /// Available in every workspace, including Home — it never needs a session.
    /// </summary>
    public class AddInManagerAddOn : IAlibreAddOn
    {
        public const string Title = "Alibre Add-On Manager";

        private const int MenuIdRoot = 7301;
        private const int MenuIdManage = 7302;
        private static readonly int[] SubMenuIds = { MenuIdManage };

        private readonly IADRoot _root;
        private readonly IntPtr _parentWindow;

        public AddInManagerAddOn(IADRoot root, IntPtr parentWindow)
        {
            _root = root;
            _parentWindow = parentWindow;
        }

        public int RootMenuItem => MenuIdRoot;

        public bool HasSubMenus(int menuId) => menuId == MenuIdRoot;

        public Array SubMenuItems(int menuId) => menuId == MenuIdRoot ? SubMenuIds : new int[0];

        public string MenuItemText(int menuId)
        {
            switch (menuId)
            {
                case MenuIdRoot: return "Add-On Manager";
                case MenuIdManage: return "Manage Add-Ons";
                default: return string.Empty;
            }
        }

        public bool PopupMenu(int menuId) => menuId == MenuIdRoot;

        // Enabled everywhere, root included, without touching Sessions — the Home
        // window passes an empty session identifier, which Sessions.Item rejects.
        public ADDONMenuStates MenuItemState(int menuId, string sessionIdentifier) => ADDONMenuStates.ADDON_MENU_ENABLED;

        public string MenuItemToolTip(int menuId)
        {
            return menuId == MenuIdManage ? "Install, update, disable and remove Alibre Design add-ons" : string.Empty;
        }

        /// <summary>Icon file name, looked up in the folder holding the .adc.</summary>
        public string MenuIcon(int menuId) => "AlibreAddInManager.ico";

        public bool HasPersistentDataToSave(string sessionIdentifier) => false;

        public IAlibreAddOnCommand InvokeCommand(int menuId, string sessionIdentifier)
        {
            if (menuId != MenuIdManage) return null;
            try
            {
                string version = null;
                try { version = _root?.Version; }
                catch (Exception) { /* version is informational only */ }

                using (var form = new ManagerForm(version))
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
    }
}
