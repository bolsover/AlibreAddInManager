using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AlibreX;
using CustomPropertyExplorer.Model;

namespace CustomPropertyExplorer.Ui
{
    /// <summary>
    /// Fills the Property Explorer's model tree. Each object node's Tag is the
    /// AlibreX object; a collection node's Tag is a <see cref="CollectionTag"/>
    /// whose items are loaded the first time it is expanded, so a large part's
    /// faces and edges cost nothing until they are asked for.
    /// <para>
    /// The script had to find features, sketches, planes, axes and points by
    /// trying names such as "Sketch&lt;1&gt;" … "Sketch&lt;50&gt;". AlibreX exposes
    /// real collections for all of them, so every item is listed, whatever it is
    /// called and in any language.
    /// </para>
    /// </summary>
    internal sealed class ModelTreeBuilder
    {
        private const string LoadingText = "Loading…";

        private readonly TreeView _tree;
        private readonly DesignContext _context;
        private readonly CustomProperties _custom;

        public ModelTreeBuilder(TreeView tree, DesignContext context, CustomProperties custom)
        {
            _tree = tree;
            _context = context;
            _custom = custom;
            _tree.BeforeExpand += OnBeforeExpand;
        }

        /// <summary>A lazily loaded collection; selecting it shows its owner's properties.</summary>
        internal sealed class CollectionTag
        {
            public CollectionTag(object owner, string itemPrefix, Func<IEnumerable<object>> items, Action<TreeNode, object> addChildren)
            {
                Owner = owner;
                ItemPrefix = itemPrefix;
                Items = items;
                AddChildren = addChildren;
            }

            public object Owner { get; }
            public string ItemPrefix { get; }
            public Func<IEnumerable<object>> Items { get; }
            public Action<TreeNode, object> AddChildren { get; }
            public bool Loaded { get; set; }
        }

        /// <summary>The object whose properties a node shows.</summary>
        public static object ItemOf(TreeNode node)
        {
            var collection = node?.Tag as CollectionTag;
            return collection != null ? collection.Owner : node?.Tag;
        }

        public void Build()
        {
            _tree.BeginUpdate();
            try
            {
                _tree.Nodes.Clear();
                var design = _context.Design;
                var root = new TreeNode(_context.Name) { Tag = design };
                _tree.Nodes.Add(root);

                AddCollection(root, "Configurations", design, () => Items(design.Configurations.GetEnumerator()), () => design.Configurations.Count);
                AddCollection(root, "Parameters", design, () => Items(design.Parameters.GetEnumerator()), () => design.Parameters.Count);
                AddCustomProperties(root, design, _custom);

                if (_context.IsAssembly)
                {
                    var top = _context.Assembly.RootOccurrence;
                    AddOccurrences(root, top);
                    AddCollection(root, "Features", design, () => Items(_context.Assembly.Features.GetEnumerator()), () => _context.Assembly.Features.Count);
                }
                else
                {
                    AddTopology(root, _context.Part);
                    AddCollection(root, "Features", design, () => Items(_context.Part.Features.GetEnumerator()), () => _context.Part.Features.Count);
                }

                AddCollection(root, "Sketches", design, () => Items(design.Sketches.GetEnumerator()), () => design.Sketches.Count, AddSketchFigures);
                if (_context.Part != null)
                    AddCollection(root, "3D Sketches", design, () => Items(_context.Part.Sketches3D.GetEnumerator()), () => _context.Part.Sketches3D.Count, AddSketchFigures);
                AddCollection(root, "Planes", design, () => Items(design.DesignPlanes.GetEnumerator()), () => design.DesignPlanes.Count);
                AddCollection(root, "Axes", design, () => Items(design.DesignAxes.GetEnumerator()), () => design.DesignAxes.Count);
                AddCollection(root, "Points", design, () => Items(design.DesignPoints.GetEnumerator()), () => design.DesignPoints.Count);

                root.Expand();
            }
            finally
            {
                _tree.EndUpdate();
            }
        }

        /// <summary>"Custom Properties (n)" with a "name = value" child per tracked property.</summary>
        public static TreeNode AddCustomProperties(TreeNode parent, IADDesignSession design, CustomProperties custom)
        {
            var values = custom.Values().ToList();
            var node = new TreeNode("Custom Properties (" + values.Count + ")") { Tag = design };
            parent.Nodes.Add(node);
            foreach (var pair in values)
                node.Nodes.Add(new TreeNode(pair.Key + " = " + pair.Value) { Tag = design });
            return node;
        }

        // --- Structure --------------------------------------------------------------------

        /// <summary>Faces, edges and vertices of every body in the part.</summary>
        private void AddTopology(TreeNode parent, IADPartSession part)
        {
            if (part == null) return;
            AddCollection(parent, "Faces", part,
                () => Bodies(part).SelectMany(b => Items(b.Faces.GetEnumerator())),
                () => Bodies(part).Sum(b => b.Faces.Count), null, "Face");
            AddCollection(parent, "Edges", part,
                () => Bodies(part).SelectMany(b => Items(b.Edges.GetEnumerator())),
                () => Bodies(part).Sum(b => b.Edges.Count), null, "Edge");
            AddCollection(parent, "Vertices", part,
                () => Bodies(part).SelectMany(b => Items(b.Vertices.GetEnumerator())),
                () => Bodies(part).Sum(b => b.Vertices.Count), null, "Vertex");
        }

        private static IEnumerable<IADBody> Bodies(IADPartSession part) => Items(part.Bodies.GetEnumerator()).Cast<IADBody>();

        /// <summary>The occurrence's children, split into parts and sub-assemblies as in the script.</summary>
        private void AddOccurrences(TreeNode parent, IADOccurrence owner)
        {
            Func<IEnumerable<IADOccurrence>> children = () => Items(owner.Occurrences.GetEnumerator()).Cast<IADOccurrence>();
            AddCollection(parent, "Parts", parent.Tag,
                () => children().Where(o => !PropertyReader.IsSubAssembly(o)),
                () => children().Count(o => !PropertyReader.IsSubAssembly(o)),
                AddOccurrenceChildren);
            AddCollection(parent, "SubAssemblies", parent.Tag,
                () => children().Where(PropertyReader.IsSubAssembly),
                () => children().Count(PropertyReader.IsSubAssembly),
                AddOccurrenceChildren);
        }

        private void AddOccurrenceChildren(TreeNode node, object item)
        {
            var occurrence = item as IADOccurrence;
            if (occurrence == null) return;
            if (PropertyReader.IsSubAssembly(occurrence))
            {
                AddOccurrences(node, occurrence);
                return;
            }

            IADPartSession part = null;
            try { part = occurrence.DesignSession as IADPartSession; }
            catch (Exception) { }
            AddTopology(node, part);
        }

        private static void AddSketchFigures(TreeNode node, object item)
        {
            var sketch = item as IADSketch;
            if (sketch != null)
            {
                AddCollection(node, "Figures", sketch, () => Items(sketch.Figures.GetEnumerator()), () => sketch.Figures.Count);
                return;
            }

            var sketch3D = item as IAD3DSketch;
            if (sketch3D != null)
                AddCollection(node, "Figures", sketch3D, () => Items(sketch3D.Figures.GetEnumerator()), () => sketch3D.Figures.Count);
        }

        // --- Collections ------------------------------------------------------------------

        /// <summary>
        /// Adds "Name (count)" with a placeholder child; empty or unreadable
        /// collections are left out, as in the script.
        /// </summary>
        private static void AddCollection(TreeNode parent, string name, object owner, Func<IEnumerable<object>> items, Func<int> count,
            Action<TreeNode, object> addChildren = null, string itemPrefix = null)
        {
            int n;
            try { n = count(); }
            catch (Exception) { return; }
            if (n == 0) return;

            var node = new TreeNode(name + " (" + n + ")") { Tag = new CollectionTag(owner, itemPrefix, items, addChildren) };
            node.Nodes.Add(new TreeNode(LoadingText));
            parent.Nodes.Add(node);
        }

        private static void OnBeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var collection = e.Node.Tag as CollectionTag;
            if (collection == null || collection.Loaded) return;
            collection.Loaded = true;

            var tree = (TreeView) sender;
            tree.BeginUpdate();
            try
            {
                e.Node.Nodes.Clear();
                var index = 0;
                foreach (var item in collection.Items())
                {
                    if (item == null) continue;
                    var text = collection.ItemPrefix != null
                        ? collection.ItemPrefix + "[" + index + "]"
                        : PropertyReader.DisplayName(item);
                    var child = new TreeNode(text) { Tag = item };
                    e.Node.Nodes.Add(child);
                    collection.AddChildren?.Invoke(child, item);
                    index++;
                }
            }
            catch (Exception ex)
            {
                e.Node.Nodes.Add(new TreeNode("Error: " + ex.Message));
            }
            finally
            {
                tree.EndUpdate();
            }
        }

        private static IEnumerable<object> Items(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }
    }
}
