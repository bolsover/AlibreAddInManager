using System;
using System.Drawing;
using System.Windows.Forms;
using AlibreX;

namespace AdvancedSweepGenerator.Ui
{
    /// <summary>
    /// The sweep dialog. Like the original AlibreScript utility dialog it stays
    /// open after "Create Sweep", so several sweeps can be made in a row.
    /// </summary>
    internal sealed class SweepForm : Form
    {
        private readonly IADRoot _root;
        private readonly IADPartSession _part;

        private readonly CheckBox _use3DPath = new CheckBox { Text = "Use 3D path", AutoSize = true };
        private readonly ComboBox _path2D = NewDropDown();
        private readonly ComboBox _path3D = NewDropDown();
        private readonly ComboBox _profile = NewDropDown();
        private readonly NumericUpDown _size = NewMillimetres(10.0m);
        private readonly CheckBox _hollow = new CheckBox { Text = "Hollow", AutoSize = true };
        private readonly NumericUpDown _thickness = NewMillimetres(2.0m);
        private readonly Label _status = new Label { AutoSize = true, ForeColor = SystemColors.GrayText };
        private readonly Button _create = new Button { Text = "Create Sweep", AutoSize = true };
        private readonly Button _close = new Button { Text = "Close", AutoSize = true, DialogResult = DialogResult.Cancel };

        public SweepForm(IADRoot root, IADPartSession part)
        {
            _root = root;
            _part = part;

            Text = AdvancedSweepAddOn.Title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Font = SystemFonts.MessageBoxFont;
            AcceptButton = _create;
            CancelButton = _close;

            _profile.Items.AddRange(new object[] { "Circle", "Square" });
            _profile.SelectedIndex = 0;

            var tips = new ToolTip();
            tips.SetToolTip(_size, "Outer diameter of a circle, or side length of a square");
            tips.SetToolTip(_thickness, "Wall thickness of a hollow profile");

            var grid = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10),
                Dock = DockStyle.Fill
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));

            AddRow(grid, null, _use3DPath);
            AddRow(grid, "2D path sketch", _path2D);
            AddRow(grid, "3D path sketch", _path3D);
            AddRow(grid, "Profile type", _profile);
            AddRow(grid, "Size (mm)", _size);
            AddRow(grid, null, _hollow);
            AddRow(grid, "Thickness (mm)", _thickness);
            AddRow(grid, null, _status);

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 0, 0)
            };
            buttons.Controls.Add(_close);
            buttons.Controls.Add(_create);
            grid.Controls.Add(buttons, 0, grid.RowCount);
            grid.SetColumnSpan(buttons, 2);
            grid.RowCount++;

            Controls.Add(grid);

            _use3DPath.CheckedChanged += (s, e) => UpdateInputs();
            _hollow.CheckedChanged += (s, e) => UpdateInputs();
            _create.Click += (s, e) => CreateSweep();

            LoadSketches();
            UpdateInputs();
        }

        private static ComboBox NewDropDown() =>
            new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };

        private static NumericUpDown NewMillimetres(decimal value) =>
            new NumericUpDown
            {
                DecimalPlaces = 3,
                Minimum = 0.001m,
                Maximum = 100000m,
                Increment = 0.5m,
                Value = value,
                Dock = DockStyle.Fill
            };

        private static void AddRow(TableLayoutPanel grid, string label, Control control)
        {
            var row = grid.RowCount++;
            if (label != null)
                grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 12, 3) }, 0, row);
            grid.Controls.Add(control, 1, row);
        }

        /// <summary>The script's InputChanged / UpdateInterface: enable only the inputs in use.</summary>
        private void UpdateInputs()
        {
            _path2D.Enabled = !_use3DPath.Checked;
            _path3D.Enabled = _use3DPath.Checked;
            _thickness.Enabled = _hollow.Checked;
        }

        /// <summary>(Re)fills the path lists, keeping the current choice by name where possible.</summary>
        private void LoadSketches()
        {
            var selected2D = (_path2D.SelectedItem as SketchItem)?.Name;
            var selected3D = (_path3D.SelectedItem as SketchItem)?.Name;

            _path2D.Items.Clear();
            foreach (IADSketch sketch in _part.Sketches)
                _path2D.Items.Add(new SketchItem(sketch.Name, sketch));

            _path3D.Items.Clear();
            foreach (IAD3DSketch sketch in _part.Sketches3D)
                _path3D.Items.Add(new SketchItem(sketch.Name, sketch));

            Reselect(_path2D, selected2D);
            Reselect(_path3D, selected3D);

            // Default to whichever kind of path the part actually has.
            if (selected2D == null && selected3D == null && _path2D.Items.Count == 0 && _path3D.Items.Count > 0)
                _use3DPath.Checked = true;
        }

        private static void Reselect(ComboBox combo, string name)
        {
            for (var i = 0; i < combo.Items.Count; i++)
            {
                if (((SketchItem) combo.Items[i]).Name != name) continue;
                combo.SelectedIndex = i;
                return;
            }

            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private void CreateSweep()
        {
            var request = new SweepRequest
            {
                Use3DPath = _use3DPath.Checked,
                PathSketch2D = (_path2D.SelectedItem as SketchItem)?.Sketch as IADSketch,
                PathSketch3D = (_path3D.SelectedItem as SketchItem)?.Sketch as IAD3DSketch,
                Shape = _profile.SelectedIndex == 1 ? ProfileShape.Square : ProfileShape.Circle,
                SizeMm = (double) _size.Value,
                Hollow = _hollow.Checked,
                ThicknessMm = (double) _thickness.Value
            };

            UseWaitCursor = true;
            _create.Enabled = false;
            try
            {
                var name = new SweepBuilder(_root, _part).Build(request);
                _status.ForeColor = SystemColors.GrayText;
                _status.Text = "Created " + name + ".";
            }
            catch (Exception e)
            {
                _status.ForeColor = Color.Firebrick;
                _status.Text = "Sweep failed.";
                MessageBox.Show(this, e.Message, "Advanced Sweep Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                _create.Enabled = true;
                try { LoadSketches(); }
                catch (Exception) { /* keep the existing lists */ }
            }
        }

        private sealed class SketchItem
        {
            public SketchItem(string name, object sketch)
            {
                Name = name;
                Sketch = sketch;
            }

            public string Name { get; }
            public object Sketch { get; }

            public override string ToString() => Name;
        }
    }
}
