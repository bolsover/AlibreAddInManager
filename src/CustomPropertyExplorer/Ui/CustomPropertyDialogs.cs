using System;
using System.Drawing;
using System.Windows.Forms;
using CustomPropertyExplorer.Model;

namespace CustomPropertyExplorer.Ui
{
    /// <summary>
    /// The scripts' "Lookup Custom Property" and "Set Custom Property" dialogs,
    /// shared by both explorers. Each name looked up or set successfully is added
    /// to the explorer's tracked list, so it then shows in the tree and the grid.
    /// </summary>
    internal static class CustomPropertyDialogs
    {
        /// <summary>
        /// Looks custom properties up by name until closed. <paramref name="looked"/>
        /// is called with each name found.
        /// </summary>
        public static void ShowLookup(IWin32Window owner, CustomProperties custom, Action<string> looked)
        {
            using (var dialog = NewDialog("Lookup Custom Property", 220))
            {
                var name = AddField(dialog, "Property Name:", 20);
                var result = AddResult(dialog, 90);
                var lookup = AddButtons(dialog, "Lookup", 140);

                lookup.Click += (s, e) =>
                {
                    var propertyName = name.Text.Trim();
                    if (propertyName.Length == 0)
                    {
                        Show(result, "Please enter a property name.", Theme.Error);
                        return;
                    }

                    try
                    {
                        var value = custom.Lookup(propertyName);
                        custom.Track(propertyName);
                        if (value != null)
                            Show(result, propertyName + " = " + Convert.ToString(value), Theme.Success);
                        else
                            Show(result, propertyName + " = (empty)", Theme.TextSecondary);
                        looked(propertyName);
                    }
                    catch (Exception ex)
                    {
                        Show(result, "Not found or error: " + ex.Message, Theme.Error);
                    }
                };

                dialog.ShowDialog(owner);
            }
        }

        /// <summary>
        /// Sets custom properties until closed. <paramref name="changed"/> is called
        /// with each name set.
        /// </summary>
        public static void ShowSet(IWin32Window owner, CustomProperties custom, Action<string> changed)
        {
            using (var dialog = NewDialog("Set Custom Property", 280))
            {
                var name = AddField(dialog, "Property Name:", 20);
                var value = AddField(dialog, "Property Value:", 90);
                var result = AddResult(dialog, 160);
                var set = AddButtons(dialog, "Set", 210);

                set.Click += (s, e) =>
                {
                    var propertyName = name.Text.Trim();
                    if (propertyName.Length == 0)
                    {
                        Show(result, "Please enter a property name.", Theme.Error);
                        return;
                    }

                    try
                    {
                        custom.Set(propertyName, value.Text);
                        custom.Track(propertyName);
                        Show(result, "Set " + propertyName + " = " + value.Text, Theme.Success);
                        changed(propertyName);
                    }
                    catch (Exception ex)
                    {
                        Show(result, "Error: " + ex.Message, Theme.Error);
                    }
                };

                dialog.ShowDialog(owner);
            }
        }

        private static Form NewDialog(string title, int height)
        {
            var dialog = new Form
            {
                Text = title,
                ClientSize = new Size(Theme.Scale(380), Theme.Scale(height - 30)),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false
            };
            Theme.ApplyTo(dialog);
            return dialog;
        }

        /// <summary>A label at <paramref name="top"/> with a text box under it.</summary>
        private static TextBox AddField(Form dialog, string label, int top)
        {
            dialog.Controls.Add(new Label
            {
                Text = label,
                Location = new Point(Theme.Scale(20), Theme.Scale(top)),
                Size = new Size(Theme.Scale(340), Theme.Scale(25)),
                ForeColor = Theme.TextPrimary
            });
            var box = Theme.TextBox();
            box.Location = new Point(Theme.Scale(20), Theme.Scale(top + 30));
            box.Width = Theme.Scale(340);
            dialog.Controls.Add(box);
            return box;
        }

        private static Label AddResult(Form dialog, int top)
        {
            var result = new Label
            {
                Location = new Point(Theme.Scale(20), Theme.Scale(top)),
                Size = new Size(Theme.Scale(340), Theme.Scale(40)),
                ForeColor = Theme.TextSecondary
            };
            dialog.Controls.Add(result);
            return result;
        }

        /// <summary>The action button (the dialog's Accept button) and Close (its Cancel button).</summary>
        private static Button AddButtons(Form dialog, string action, int top)
        {
            var button = Theme.Button(action, Theme.Accent);
            button.Location = new Point(Theme.Scale(20), Theme.Scale(top));
            dialog.Controls.Add(button);
            dialog.AcceptButton = button;

            var close = Theme.Button("Close", Theme.Secondary);
            close.Location = new Point(Theme.Scale(260), Theme.Scale(top));
            close.DialogResult = DialogResult.Cancel;
            dialog.Controls.Add(close);
            dialog.CancelButton = close;
            return button;
        }

        private static void Show(Label result, string text, Color color)
        {
            result.Text = text;
            result.ForeColor = color;
        }
    }
}
