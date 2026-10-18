using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AlibreAddOn;
using AlibreAddOnManager;
using AlibreX;

// ReSharper disable once CheckNamespace
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
        private static AddOnManagerAddOn _addOn;

        public static void AddOnLoad(IntPtr hwnd, IAutomationHook pAutomationHook, IntPtr unused)
        {
            try
            {
                _root = (IADRoot) pAutomationHook.Root;
                _addOn = new AddOnManagerAddOn(_root, hwnd);
            }
            catch (Exception e)
            {
                MessageBox.Show("Alibre Add-On Manager failed to load:\n\n" + e.Message,
                    AddOnManagerAddOn.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static IADRoot GetRoot()
        {
            return _root;
        }

        public static void AddOnInvoke(IntPtr hwnd, IntPtr pAutomationHook, string sessionName, bool isLicensed, int reserved1, int reserved2)
        {
            // try
            // {
            //     IAutomationHook hook;
            //     if (pAutomationHook == IntPtr.Zero)
            //         return ;
            //     GCHandle handle = (GCHandle)pAutomationHook;
            //     hook = (IAutomationHook) handle.Target;
            //     // if (handles.unfreed.Contains(handle))
            //     //     handles.unfreed.Remove(handle);
            //     handle.Free();
            //    
            //     _root = (IADRoot) hook.Root;
            //     _addOn = new AddOnManagerAddOn(_root, hwnd);
            // }
            // catch (Exception e)
            // {
            //     MessageBox.Show("Alibre Add-On Manager failed to load:\n\n" + e.Message,
            //         AddOnManagerAddOn.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            // }
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
