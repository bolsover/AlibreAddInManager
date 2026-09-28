using System;
using System.Windows.Forms;
using AlibreAddOn;
using AlibreAddInManager;
using AlibreX;

namespace AlibreAddOnAssembly
{
    /// <summary>
    /// Entry points for a managed (.adc &lt;DLL type="Managed"&gt;) add-on. Alibre
    /// looks for this exact type name, AlibreAddOnAssembly.AlibreAddOn, and calls
    /// its static methods — the managed counterpart of the C++ exports
    /// AddOnLoad / AddOnInvoke / AddOnUnload / GetAddOnInterface.
    /// </summary>
    public static class AlibreAddOn
    {
        private static IADRoot _root;
        private static AddInManagerAddOn _addOn;

        public static void AddOnLoad(IntPtr hwnd, IAutomationHook pAutomationHook, IntPtr unused)
        {
            try
            {
                _root = (IADRoot) pAutomationHook.Root;
                _addOn = new AddInManagerAddOn(_root, hwnd);
            }
            catch (Exception e)
            {
                MessageBox.Show("Alibre Add-On Manager failed to load:\n\n" + e.Message,
                    AddInManagerAddOn.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static IADRoot GetRoot()
        {
            return _root;
        }

        public static void AddOnInvoke(IntPtr hwnd, IntPtr pAutomationHook, string sessionName, bool isLicensed, int reserved1, int reserved2)
        {
        }

        public static void AddOnUnload(IntPtr hwnd, bool forceUnload, ref bool cancel, int reserved1, int reserved2)
        {
            _addOn = null;
            _root = null;
        }

        public static IAlibreAddOn GetAddOnInterface()
        {
            return _addOn;
        }
    }
}
