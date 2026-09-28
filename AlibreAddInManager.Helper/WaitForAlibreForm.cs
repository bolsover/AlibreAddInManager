using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AlibreAddInManager.Models;

namespace AlibreAddInManager.Helper
{
    /// <summary>
    /// Shown while changes wait for Alibre Design to exit: the DLLs being replaced
    /// or deleted are locked while Alibre has them loaded. Closes itself with OK
    /// as soon as no Alibre process remains.
    /// </summary>
    internal class WaitForAlibreForm : Form
    {
        private readonly Timer _timer = new Timer { Interval = 1000 };

        public WaitForAlibreForm(InstallPlan plan)
        {
            Text = "Alibre Add-On Manager";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(460, 220);
            TopMost = true;

            var label = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10),
                Text = "Waiting for Alibre Design to close.\n" +
                       "Save your work and exit Alibre Design; the changes below will then be applied."
            };

            var list = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            list.Items.AddRange(plan.Operations.Select(o => (object) o.Describe()).ToArray());

            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(6)
            };
            buttons.Controls.Add(cancel);

            Controls.Add(list);
            Controls.Add(buttons);
            Controls.Add(label);
            CancelButton = cancel;

            _timer.Tick += (s, e) =>
            {
                if (!Program.IsAlibreRunning())
                {
                    _timer.Stop();
                    DialogResult = DialogResult.OK;
                }
            };
            _timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // WaitForAlibreForm
            // 
            this.ClientSize = new System.Drawing.Size(409, 320);
            this.Name = "WaitForAlibreForm";
            this.ResumeLayout(false);
        }
    }
}
