using System;
using System.Windows.Forms;

namespace CustomPropertyExplorer.Ui
{
    /// <summary>Lets a WinForms window be owned by Alibre's main window handle.</summary>
    internal sealed class WindowWrapper : IWin32Window
    {
        public WindowWrapper(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; }
    }
}
