using System;
using System.Windows.Forms;

namespace AlibreAddInManager.Ui
{
    /// <summary>Lets a WinForms dialog be owned by Alibre's main window handle.</summary>
    internal sealed class WindowWrapper : IWin32Window
    {
        public WindowWrapper(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; }
    }
}
