using System;
using System.Drawing;
using System.Windows.Forms;
using CustomPropertyExplorer.Model;

namespace CustomPropertyExplorer.Ui
{
    /// <summary>
    /// The Custom Property Explorer (CustomPropertyExplorer.py): the design's
    /// custom properties as a tree and an editable PropertyGrid, plus the lookup
    /// and set dialogs. No model structure and no selection tracking — the
    /// script removed those on purpose. Shown modally.
    /// </summary>
    internal sealed class CustomPropertyExplorerForm : Form
    {
        private readonly DesignContext _context;
        private readonly CustomProperties _custom;
        private readonly TreeView _tree;
        private readonly PropertyGrid _grid;
        private readonly ToolStripStatusLabel _status;
        private readonly ToolStripStatusLabel _selection;

        public CustomPropertyExplorerForm(DesignContext context)
        {
            _context = context;
            _custom = new CustomProperties(context.Design);

            Text = "Custom Property Explorer - " + context.Name;
            Theme.ApplyTo(this);
            Size = new Size(Theme.Scale(1000), Theme.Scale(700));
            MinimumSize = new Size(Theme.Scale(700), Theme.Scale(450));
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;

            var statusStrip = Theme.StatusStrip(out _status, out _selection);
            _selection.Text = "Custom properties only";

            var splitter = Theme.Splitter();
            var left = Theme.Panel(Theme.Scale(12, 12, 12, 12));
            var right = Theme.Panel(Theme.Scale(12, 12, 12, 12));
            splitter.Panel1.Controls.Add(left);
            splitter.Panel2.Controls.Add(right);

            var toolStrip = Theme.ToolStrip();
            var lookupButton = Theme.ToolButton("Lookup Custom Prop", "Lookup a custom property by name", Theme.Warning);
            var setButton = Theme.ToolButton("Set Custom Prop", "Set a custom property value", Theme.Warning);
            toolStrip.Items.AddRange(new ToolStripItem[] { lookupButton, setButton });

            _tree = Theme.TreeView();
            _grid = Theme.PropertyGrid();

            // Dock order: the Fill control first, then the Top controls; the last Top added sits highest.
            left.Controls.Add(_tree);
            left.Controls.Add(toolStrip);
            left.Controls.Add(Theme.Header("Custom Properties"));
            right.Controls.Add(_grid);
            right.Controls.Add(Theme.Header("Property Grid"));

            Controls.Add(splitter);
            Controls.Add(statusStrip);

            _tree.AfterSelect += OnNodeSelect;
            lookupButton.Click += (s, e) => CustomPropertyDialogs.ShowLookup(this, _custom, name =>
            {
                BuildTree();
                UpdateStatus("Looked up: " + name);
            });
            setButton.Click += (s, e) => CustomPropertyDialogs.ShowSet(this, _custom, name =>
            {
                BuildTree();
                UpdateStatus("Set: " + name);
            });
            _grid.PropertyValueChanged += OnPropertyValueChanged;

            Load += (s, e) =>
            {
                splitter.SplitterDistance = Theme.Scale(380);
                BuildTree();
            };
        }

        private PropertyBag CustomBag()
        {
            var bag = new PropertyBag("Custom Properties");
            PropertyReader.AddCustomProperties(bag, _custom);
            return bag;
        }

        private void BuildTree()
        {
            _tree.BeginUpdate();
            try
            {
                _tree.Nodes.Clear();
                var root = new TreeNode(_context.Name) { Tag = _context.Design };
                _tree.Nodes.Add(root);
                var customNode = ModelTreeBuilder.AddCustomProperties(root, _context.Design, _custom);
                root.Expand();
                customNode.Expand();
                _tree.SelectedNode = customNode;
            }
            finally
            {
                _tree.EndUpdate();
            }

            _grid.SelectedObject = CustomBag();
            UpdateStatus("Loaded custom properties");
        }

        private void OnNodeSelect(object sender, TreeViewEventArgs e)
        {
            if (_tree.SelectedNode == null)
            {
                _grid.SelectedObject = null;
                UpdateStatus("No selection");
                return;
            }

            _grid.SelectedObject = CustomBag();
            UpdateStatus("Selected: " + _tree.SelectedNode.Text);
        }

        private void OnPropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            var name = e.ChangedItem?.Label;
            if (name == null) return;
            BuildTree();
            UpdateStatus("Updated: " + name + " = " + Convert.ToString(e.ChangedItem.Value));
        }

        private void UpdateStatus(string selection)
        {
            _status.Text = "Ready - Model: " + _context.Name;
            _selection.Text = selection;
        }
    }
}
