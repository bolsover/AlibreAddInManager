using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AlibreX;
using CustomPropertyExplorer.Model;

namespace CustomPropertyExplorer.Ui
{
    /// <summary>
    /// The Property Explorer (PropertyExplorerCustomProperties.py, a superset of
    /// PropertyExplorer.py): the model tree on the left, the selected item's
    /// properties on the right, kept in step with the Alibre selection while
    /// "Real-time Sync" is on. Custom properties of the design are editable in the
    /// grid and through the lookup / set dialogs.
    /// <para>
    /// Modeless, so the user can keep selecting in Alibre. It closes itself when
    /// its workspace closes.
    /// </para>
    /// </summary>
    internal sealed class PropertyExplorerForm : Form
    {
        /// <summary>After a click in the tree, selection sync pauses this long so the click isn't overwritten.</summary>
        private static readonly TimeSpan ManualSelectionHold = TimeSpan.FromSeconds(1);

        private readonly IADRoot _root;
        private readonly DesignContext _context;
        private readonly CustomProperties _custom;
        private readonly PropertyReader _reader;
        private readonly ModelTreeBuilder _builder;

        private readonly TreeView _tree;
        private readonly PropertyGrid _grid;
        private readonly ToolStripButton _syncButton;
        private readonly ToolStripStatusLabel _status;
        private readonly ToolStripStatusLabel _selection;
        private readonly Timer _selectionTimer;

        private string _lastSelection;
        private DateTime _manualUntil = DateTime.MinValue;

        public PropertyExplorerForm(IADRoot root, DesignContext context)
        {
            _root = root;
            _context = context;
            _custom = new CustomProperties(context.Design);
            _reader = new PropertyReader(context, _custom);

            Text = "Alibre Property Explorer - " + context.Name;
            Theme.ApplyTo(this);
            var area = Screen.PrimaryScreen.WorkingArea;
            Size = new Size(Math.Min(Theme.Scale(1600), area.Width), Math.Min(Theme.Scale(1000), area.Height));
            MinimumSize = new Size(Math.Min(Theme.Scale(1000), area.Width), Math.Min(Theme.Scale(600), area.Height));
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;

            var statusStrip = Theme.StatusStrip(out _status, out _selection);

            var splitter = Theme.Splitter();
            var left = Theme.Panel(Theme.Scale(16, 16, 12, 16));
            var right = Theme.Panel(Theme.Scale(12, 16, 16, 16));
            splitter.Panel1.Controls.Add(left);
            splitter.Panel2.Controls.Add(right);

            var toolStrip = Theme.ToolStrip();
            var refreshButton = Theme.ToolButton("Refresh", "Refresh the model tree", Theme.TextPrimary);
            var expandButton = Theme.ToolButton("Expand All", "Expand all tree nodes", Theme.TextPrimary);
            var collapseButton = Theme.ToolButton("Collapse All", "Collapse all tree nodes", Theme.TextPrimary);
            _syncButton = Theme.ToolButton("Real-time Sync", "Automatically sync with Alibre selection", Theme.Success);
            _syncButton.CheckOnClick = true;
            _syncButton.Checked = true;
            var lookupButton = Theme.ToolButton("Custom Props", "Lookup a custom property by name", Theme.Warning);
            var setButton = Theme.ToolButton("Set Custom Prop", "Set a custom property value", Theme.Warning);
            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                refreshButton, Theme.ToolSeparator(), expandButton, collapseButton, Theme.ToolSeparator(),
                _syncButton, Theme.ToolSeparator(), lookupButton, setButton
            });

            _tree = Theme.TreeView();
            _grid = Theme.PropertyGrid();
            _builder = new ModelTreeBuilder(_tree, context, _custom);

            // Dock order: the Fill control first, then the Top controls; the last Top added sits highest.
            left.Controls.Add(_tree);
            left.Controls.Add(toolStrip);
            left.Controls.Add(Theme.Header("Model Structure"));
            right.Controls.Add(_grid);
            right.Controls.Add(Theme.Header("Properties"));

            Controls.Add(splitter);
            Controls.Add(statusStrip);

            _tree.AfterSelect += OnNodeSelect;
            refreshButton.Click += (s, e) =>
            {
                BuildTree();
                UpdateStatus("Model refreshed");
            };
            expandButton.Click += (s, e) => ExpandAll();
            collapseButton.Click += (s, e) =>
            {
                _tree.CollapseAll();
                UpdateStatus("All nodes collapsed");
            };
            _syncButton.CheckedChanged += OnSyncToggled;
            lookupButton.Click += (s, e) => CustomPropertyDialogs.ShowLookup(this, _custom, name =>
            {
                RefreshCustomProperties();
                UpdateStatus("Looked up: " + name);
            });
            setButton.Click += (s, e) => CustomPropertyDialogs.ShowSet(this, _custom, name =>
            {
                RefreshCustomProperties();
                UpdateStatus("Set: " + name);
            });
            _grid.PropertyValueChanged += OnPropertyValueChanged;

            _selectionTimer = new Timer { Interval = 500 };
            _selectionTimer.Tick += OnSelectionTimerTick;

            Load += (s, e) =>
            {
                splitter.SplitterDistance = splitter.Width / 2;
                BuildTree();
                UpdateStatus("Explorer initialized");
                _selectionTimer.Start();
            };
            FormClosed += (s, e) =>
            {
                _selectionTimer.Stop();
                _selectionTimer.Dispose();
            };
        }

        private void BuildTree()
        {
            UpdateStatus("Building model tree...");
            try
            {
                _builder.Build();
                UpdateStatus("Tree built successfully");
            }
            catch (Exception e)
            {
                UpdateStatus("Tree failed: " + e.Message);
            }
        }

        /// <summary>
        /// Rebuilds only the "Custom Properties" branch, so a custom-property edit
        /// doesn't collapse the rest of the tree (the script rebuilt it all).
        /// </summary>
        private void RefreshCustomProperties()
        {
            if (_tree.Nodes.Count == 0) return;
            var root = _tree.Nodes[0];
            var old = root.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text.StartsWith("Custom Properties ("));

            _tree.BeginUpdate();
            try
            {
                var wasExpanded = old?.IsExpanded ?? false;
                var index = old?.Index ?? root.Nodes.Count;
                old?.Remove();
                var node = ModelTreeBuilder.AddCustomProperties(root, _context.Design, _custom);
                node.Remove();
                root.Nodes.Insert(index, node);
                if (wasExpanded) node.Expand();
            }
            finally
            {
                _tree.EndUpdate();
            }

            // The grid shows the design's custom properties too; reload it if it is showing the design.
            if (ModelTreeBuilder.ItemOf(_tree.SelectedNode) is IADDesignSession || _tree.SelectedNode == null)
                ShowProperties(_context.Design);
        }

        private void ExpandAll()
        {
            UseWaitCursor = true;
            try
            {
                _tree.BeginUpdate();
                _tree.ExpandAll();
            }
            finally
            {
                _tree.EndUpdate();
                UseWaitCursor = false;
            }

            UpdateStatus("All nodes expanded");
        }

        private void ShowProperties(object item)
        {
            _grid.SelectedObject = item == null ? null : _reader.Read(item);
        }

        private void OnNodeSelect(object sender, TreeViewEventArgs e)
        {
            _manualUntil = DateTime.Now + ManualSelectionHold;

            var item = ModelTreeBuilder.ItemOf(_tree.SelectedNode);
            if (item == null)
            {
                _grid.SelectedObject = null;
                UpdateStatus("No selection");
                return;
            }

            ShowProperties(item);
            UpdateStatus("Selected: " + _tree.SelectedNode.Text);
        }

        private void OnSyncToggled(object sender, EventArgs e)
        {
            if (_syncButton.Checked)
            {
                _syncButton.ForeColor = Theme.Success;
                _lastSelection = null;
                _selectionTimer.Start();
                UpdateStatus("Real-time sync enabled");
            }
            else
            {
                _syncButton.ForeColor = Theme.TextPrimary;
                _selectionTimer.Stop();
                UpdateStatus("Real-time sync disabled");
            }
        }

        /// <summary>Shows the first item of the Alibre selection whenever it changes.</summary>
        private void OnSelectionTimerTick(object sender, EventArgs e)
        {
            if (!_context.IsOpen(_root))
            {
                _selectionTimer.Stop();
                Close();
                return;
            }

            if (!_syncButton.Checked || DateTime.Now < _manualUntil) return;

            try
            {
                object first = null;
                var selected = _context.Design.SelectedObjects;
                if (selected != null && selected.Count > 0)
                {
                    var items = selected.GetEnumerator();
                    if (items.MoveNext()) first = items.Current;
                }

                if (first != null)
                {
                    var signature = Signature(first);
                    if (signature == _lastSelection) return;
                    _lastSelection = signature;
                    ShowProperties(first);
                    UpdateStatus("Selected: " + PropertyReader.DisplayName(Unwrap(first)));
                }
                else if (_lastSelection != null)
                {
                    _lastSelection = null;
                    _grid.SelectedObject = null;
                    UpdateStatus("No selection");
                }
            }
            catch (Exception)
            {
                // Transient states (regeneration, an open dialog in Alibre) can refuse the read; try again next tick.
            }
        }

        private static object Unwrap(object item)
        {
            var proxy = item as IADTargetProxy;
            if (proxy == null) return item;
            try { return proxy.Target ?? proxy.Occurrence; }
            catch (Exception) { return item; }
        }

        /// <summary>
        /// Identifies a selection between ticks. Topology is compared by Key, as it
        /// has no name; everything else by type and name, qualified by occurrence.
        /// </summary>
        private static string Signature(object item)
        {
            string occurrence = null;
            var proxy = item as IADTargetProxy;
            if (proxy != null)
            {
                try { occurrence = proxy.Occurrence?.Name; }
                catch (Exception) { }
            }

            var target = Unwrap(item);
            string identity;
            if (target is IADFace) identity = PropertyReader.KeyString(((IADFace) target).Key);
            else if (target is IADEdge) identity = PropertyReader.KeyString(((IADEdge) target).Key);
            else if (target is IADVertex) identity = PropertyReader.KeyString(((IADVertex) target).Key);
            else if (target is IADSketchFigure) identity = ((IADSketchFigure) target).Sketch.Name + "/" + ((IADSketchFigure) target).ID;
            else if (target is IAD3DSketchFigure) identity = ((IAD3DSketchFigure) target).Sketch.Name + "/" + ((IAD3DSketchFigure) target).ID;
            else identity = PropertyReader.DisplayName(target);

            return PropertyReader.TypeName(target) + "|" + identity + "|" + occurrence;
        }

        private void OnPropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            var descriptor = e.ChangedItem?.PropertyDescriptor;
            if (descriptor == null || descriptor.Category != PropertyReader.CustomCategory) return;
            var name = e.ChangedItem.Label;
            RefreshCustomProperties();
            UpdateStatus("Updated: " + name + " = " + Convert.ToString(e.ChangedItem.Value));
        }

        private void UpdateStatus(string selection)
        {
            _status.Text = "Ready - Model: " + _context.Name;
            _selection.Text = selection;
        }
    }
}
