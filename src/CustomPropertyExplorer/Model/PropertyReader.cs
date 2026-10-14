using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlibreX;

namespace CustomPropertyExplorer.Model
{
    /// <summary>
    /// Builds the PropertyGrid contents for any item in the model tree or the
    /// Alibre selection — the scripts' PropertyWrapper. The scripts read
    /// AlibreScript objects; this reads the AlibreX objects they wrap, so a few
    /// script properties map to different members or have no AlibreX equivalent
    /// (see README). Every read is guarded: one failing member shows its error
    /// and the rest of the grid still loads.
    /// </summary>
    internal sealed class PropertyReader
    {
        public const string CustomCategory = "Custom Properties";

        private const string General = "General";
        private const string DesignCategory = "Design Properties";
        private const string Physical = "Physical Properties";
        private const string Geometry = "Geometry";

        private static readonly KeyValuePair<string, ADExtendedDesignProperty>[] ExtendedProperties =
        {
            Pair("Comment", ADExtendedDesignProperty.AD_COMMENT),
            Pair("Cost Center", ADExtendedDesignProperty.AD_COST_CENTER),
            Pair("Created By", ADExtendedDesignProperty.AD_CREATED_BY),
            Pair("Created Date", ADExtendedDesignProperty.AD_CREATED_DATE),
            Pair("Creating Application", ADExtendedDesignProperty.AD_CREATING_APPLICATION),
            Pair("Document Number", ADExtendedDesignProperty.AD_DOCUMENT_NUMBER),
            Pair("Engineering Approval Date", ADExtendedDesignProperty.AD_ENG_APPROVAL_DATE),
            Pair("Engineering Approved By", ADExtendedDesignProperty.AD_ENG_APPROVED_BY),
            Pair("Estimated Cost", ADExtendedDesignProperty.AD_ESTIMATED_COST),
            Pair("Keywords", ADExtendedDesignProperty.AD_KEYWORDS),
            Pair("Last Author", ADExtendedDesignProperty.AD_LAST_AUTHOR),
            Pair("Last Update Date", ADExtendedDesignProperty.AD_LAST_UPDATE_DATE),
            Pair("Extended Material Information", ADExtendedDesignProperty.AD_MATERIAL),
            Pair("Manufacturing Approved By", ADExtendedDesignProperty.AD_MFG_APPROVED_BY),
            Pair("Manufacturing Approved Date", ADExtendedDesignProperty.AD_MFG_APPROVED_DATE),
            Pair("Modified Information", ADExtendedDesignProperty.AD_MODIFIED),
            Pair("Product", ADExtendedDesignProperty.AD_PRODUCT),
            Pair("Received From", ADExtendedDesignProperty.AD_RECEIVED_FROM),
            Pair("Revision", ADExtendedDesignProperty.AD_REVISION),
            Pair("Stock Size", ADExtendedDesignProperty.AD_STOCK_SIZE),
            Pair("Supplier", ADExtendedDesignProperty.AD_SUPPLIER),
            Pair("Title", ADExtendedDesignProperty.AD_TITLE),
            Pair("Vendor", ADExtendedDesignProperty.AD_VENDOR),
            Pair("Web Link", ADExtendedDesignProperty.AD_WEBLINK)
        };

        private readonly DesignContext _context;
        private readonly CustomProperties _custom;

        public PropertyReader(DesignContext context, CustomProperties custom)
        {
            _context = context;
            _custom = custom;
        }

        private static KeyValuePair<string, ADExtendedDesignProperty> Pair(string name, ADExtendedDesignProperty id) =>
            new KeyValuePair<string, ADExtendedDesignProperty>(name, id);

        /// <summary>The properties of <paramref name="item"/>; selection proxies are unwrapped.</summary>
        public PropertyBag Read(object item)
        {
            var proxy = item as IADTargetProxy;
            if (proxy != null)
            {
                object target = null;
                try { target = proxy.Target; }
                catch (Exception) { }
                item = target ?? SafeOccurrence(proxy);
            }

            if (item is IADDesignSession) return ReadDesign((IADDesignSession) item);
            if (item is IADOccurrence) return ReadOccurrence((IADOccurrence) item);
            if (item is IADFace) return ReadFace((IADFace) item);
            if (item is IADEdge) return ReadEdge((IADEdge) item);
            if (item is IADVertex) return ReadVertex((IADVertex) item);
            if (item is IADBody) return ReadBody((IADBody) item);
            if (item is IADSketch) return ReadSketch((IADSketch) item);
            if (item is IAD3DSketch) return Read3DSketch((IAD3DSketch) item);
            if (item is IADParameter) return ReadParameter((IADParameter) item);
            if (item is IADConfiguration) return ReadConfiguration((IADConfiguration) item);
            if (item is IADPartFeature) return ReadFeature((IADPartFeature) item);
            if (item is IADAssemblyFeature) return ReadAssemblyFeature((IADAssemblyFeature) item);
            if (item is IADDesignPlane) return ReadPlane((IADDesignPlane) item);
            if (item is IADDesignAxis) return ReadAxis((IADDesignAxis) item);
            if (item is IADDesignPoint) return ReadPoint((IADDesignPoint) item);
            if (item is IADSketchFigure) return ReadFigure((IADSketchFigure) item);
            if (item is IAD3DSketchFigure) return Read3DFigure((IAD3DSketchFigure) item);
            return ReadGeneric(item);
        }

        private static IADOccurrence SafeOccurrence(IADTargetProxy proxy)
        {
            try { return proxy.Occurrence; }
            catch (Exception) { return null; }
        }

        // --- Naming, shared with the tree ------------------------------------------------

        /// <summary>The item's own name, or its type name when it has none (topology, figures).</summary>
        public static string DisplayName(object item)
        {
            try
            {
                string name = null;
                if (item is IADSession) name = ((IADSession) item).Name;
                else if (item is IADOccurrence) name = ((IADOccurrence) item).Name;
                else if (item is IADSketch) name = ((IADSketch) item).Name;
                else if (item is IAD3DSketch) name = ((IAD3DSketch) item).Name;
                else if (item is IADParameter) name = ((IADParameter) item).Name;
                else if (item is IADConfiguration) name = ((IADConfiguration) item).Name;
                else if (item is IADPartFeature) name = ((IADPartFeature) item).Name;
                else if (item is IADAssemblyFeature) name = ((IADAssemblyFeature) item).Name;
                else if (item is IADDesignPlane) name = ((IADDesignPlane) item).Name;
                else if (item is IADDesignAxis) name = ((IADDesignAxis) item).Name;
                else if (item is IADDesignPoint) name = ((IADDesignPoint) item).Name;
                else if (item is IADTargetProxy) name = ((IADTargetProxy) item).DisplayName;
                if (!string.IsNullOrEmpty(name)) return name;
            }
            catch (Exception)
            {
            }

            return TypeName(item);
        }

        /// <summary>A readable type name: "Face", "Line", "Sketch"…</summary>
        public static string TypeName(object item)
        {
            if (item == null) return "None";
            if (item is IADPartSession) return "Part";
            if (item is IADAssemblySession) return "Assembly";
            if (item is IADOccurrence) return IsSubAssembly((IADOccurrence) item) ? "Sub-Assembly" : "Assembled Part";
            if (item is IADFace) return "Face";
            if (item is IADEdge) return "Edge";
            if (item is IADVertex) return "Vertex";
            if (item is IADBody) return "Body";
            if (item is IADSketch) return "Sketch";
            if (item is IAD3DSketch) return "3D Sketch";
            if (item is IADParameter) return "Parameter";
            if (item is IADConfiguration) return "Configuration";
            if (item is IADPartFeature || item is IADAssemblyFeature) return "Feature";
            if (item is IADDesignPlane) return "Plane";
            if (item is IADDesignAxis) return "Axis";
            if (item is IADDesignPoint) return "Point";
            if (item is IADSketchFigure || item is IAD3DSketchFigure) return FigureTypeName(item);
            try { return Microsoft.VisualBasic.Information.TypeName(item); }
            catch (Exception) { return item.GetType().Name; }
        }

        private static string FigureTypeName(object figure)
        {
            if (figure is IADSketchLine) return "Line";
            if (figure is IADSketchCircle) return "Circle";
            if (figure is IADSketchCircularArc) return "Circular Arc";
            if (figure is IADSketchEllipse) return "Ellipse";
            if (figure is IADSketchEllipticArc) return "Elliptical Arc";
            if (figure is IADSketchBspline) return "B-spline";
            if (figure is IADSketchPoint) return "Sketch Point";
            if (figure is IAD3DSketchLine) return "3D Line";
            if (figure is IAD3DSketchCircle) return "3D Circle";
            if (figure is IAD3DSketchCircularArc) return "3D Circular Arc";
            if (figure is IAD3DSketchEllipticArc) return "3D Elliptical Arc";
            if (figure is IAD3DSketchBspline) return "3D B-spline";
            if (figure is IAD3DSketchPoint) return "3D Sketch Point";
            return "Sketch Figure";
        }

        public static bool IsSubAssembly(IADOccurrence occurrence)
        {
            try { return occurrence.DesignSession is IADAssemblySession; }
            catch (Exception) { return false; }
        }

        /// <summary>Topology has no stable name; its Key identifies it (compare as a string).</summary>
        public static string KeyString(Array key)
        {
            if (key == null) return string.Empty;
            var text = new StringBuilder(key.Length * 2);
            foreach (var b in key) text.Append(Convert.ToByte(b).ToString("X2"));
            return text.ToString();
        }

        // --- Designs ----------------------------------------------------------------------

        private PropertyBag ReadDesign(IADDesignSession design)
        {
            var bag = new PropertyBag(DisplayName(design));
            bag.Add(General, "Name", () => design.Name);
            bag.Add(General, "Type", () => TypeName(design));
            bag.Add(General, "File Path", () => design.FilePath);
            bag.Add(General, "Length Display Units", () => DesignContext.UnitName(design.DesignProperties.LengthDisplayUnits));
            bag.Add(General, "Angle Display Units", () => DesignContext.UnitName(design.DesignProperties.AngleDisplayUnits));
            bag.Add(General, "Mass Units", () => DesignContext.UnitName(design.DesignProperties.MassUnits));
            bag.Add(General, "Active Configuration", () => design.ActiveConfiguration.Name);

            bag.Add(DesignCategory, "Description", () => design.DesignProperties.Description);
            bag.Add(DesignCategory, "Number", () => design.DesignProperties.Number);
            bag.Add(DesignCategory, "Material", () => design.DesignProperties.Material);
            bag.Add(DesignCategory, "Density", () => DesignContext.Number(design.DesignProperties.Density));
            foreach (var extended in ExtendedProperties)
            {
                var id = extended.Value;
                bag.Add(DesignCategory, extended.Key, () => Text(design.DesignProperties.ExtendedDesignProperty(id)));
            }

            AddPhysicalProperties(bag, design);
            AddCustomProperties(bag, _custom);
            return bag;
        }

        private void AddPhysicalProperties(PropertyBag bag, IADDesignSession design)
        {
            IADPhysicalProperties physical;
            try
            {
                physical = design.PhysicalProperties(ADAccuracySetting.AD_MEDIUM);
            }
            catch (Exception e)
            {
                bag.Add(Physical, "Physical Properties", "Error: " + e.Message);
                return;
            }

            bag.Add(Physical, "Mass", () => DesignContext.Number(physical.Mass) + " " + DesignContext.UnitName(design.DesignProperties.MassUnits));
            bag.Add(Physical, "Volume", () => _context.Volume(physical.Volume));
            bag.Add(Physical, "Surface Area", () => _context.Area(physical.SurfaceArea));
            bag.Add(Physical, "Center of Gravity", () =>
            {
                double x, y, z;
                physical.GetCenterOfGravity(out x, out y, out z);
                return _context.Point(x, y, z);
            });
            AddExtents(bag, Physical, "Bounding Box", (out IADPoint lower, out IADPoint upper) => physical.GetExtents(out lower, out upper));
            bag.Add(Physical, "Face Count", () => physical.FacesCount.ToString());
            bag.Add(Physical, "Edge Count", () => physical.EdgesCount.ToString());
            bag.Add(Physical, "Vertex Count", () => physical.VerticesCount.ToString());
        }

        /// <summary>
        /// The tracked custom properties, editable. Writes go straight to the design;
        /// a refusal is reported by the PropertyGrid and the old value kept.
        /// </summary>
        public static void AddCustomProperties(PropertyBag bag, CustomProperties custom)
        {
            foreach (var pair in custom.Values())
            {
                var name = pair.Key;
                bag.Add(CustomCategory, name, pair.Value, value =>
                {
                    try
                    {
                        custom.Set(name, value);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException("Failed to set custom property '" + name + "':\n" + e.Message +
                                                            "\n\nThe property must already be defined on the part/assembly or on your PC.", e);
                    }
                }, "Custom property \"" + name + "\". Edit the value to write it to the design.");
            }
        }

        private PropertyBag ReadOccurrence(IADOccurrence occurrence)
        {
            var bag = new PropertyBag(DisplayName(occurrence));
            bag.Add(General, "Name", () => occurrence.Name);
            bag.Add(General, "Type", () => TypeName(occurrence));
            bag.Add(General, "Design", () => occurrence.DesignSession.Name);
            bag.Add(General, "File Path", () => occurrence.DesignSession.FilePath);
            bag.Add(General, "Configuration", () => occurrence.Configuration.Name);
            bag.Add(General, "Configuration Count", () => occurrence.DesignSession.Configurations.Count.ToString());
            bag.Add(General, "Child Occurrences", () => occurrence.Occurrences.Count.ToString());
            bag.Add(General, "Nest Level", () => occurrence.NestLevel.ToString());
            bag.Add(General, "Is Anchored", () => occurrence.IsAnchored.ToString());
            bag.Add(General, "Is Hidden", () => occurrence.IsHidden.ToString());
            bag.Add(General, "Is Suppressed", () => occurrence.IsSuppressed.ToString());
            bag.Add(General, "Is Flexible", () => occurrence.IsFlexible.ToString());
            AddExtents(bag, Geometry, "Bounding Box (Assembly Coords)", (out IADPoint lower, out IADPoint upper) => occurrence.GetExtents(out lower, out upper));
            bag.Add(DesignCategory, "Description", () => occurrence.DesignSession.DesignProperties.Description);
            bag.Add(DesignCategory, "Number", () => occurrence.DesignSession.DesignProperties.Number);
            bag.Add(DesignCategory, "Material", () => occurrence.DesignSession.DesignProperties.Material);
            return bag;
        }

        // --- Topology ---------------------------------------------------------------------

        private PropertyBag ReadFace(IADFace face)
        {
            var bag = new PropertyBag("Face");
            bag.Add(General, "Type", () => "Face");
            bag.Add(Geometry, "Surface Type", () => GeometryName(face.Geometry.SurfaceType));
            bag.Add(Geometry, "Edge Count", () => face.Edges.Count.ToString());
            bag.Add(Geometry, "Vertex Count", () => face.Vertices.Count.ToString());
            bag.Add(Geometry, "Adjoining Face Count", () => AdjoiningFaceCount(face).ToString());
            bag.Add(Geometry, "Loop Count", () => face.Loops.Count.ToString());

            var plane = SafeGeometry(face) as IADPlane;
            if (plane != null)
            {
                bag.Add(Geometry, "Normal", () => DesignContext.Vector(plane.Normal));
                bag.Add(Geometry, "Root Point", () => _context.Point(plane.RootPoint));
            }

            var cylinder = SafeGeometry(face) as IADCylinder;
            if (cylinder != null)
            {
                bag.Add(Geometry, "Radius", () => _context.Length(cylinder.Radius));
                bag.Add(Geometry, "Diameter", () => _context.Length(2 * cylinder.Radius));
                bag.Add(Geometry, "Axis", () => DesignContext.Vector(cylinder.Axis));
            }

            AddExtents(bag, Geometry, "Bounding Box", (out IADPoint lower, out IADPoint upper) => face.GetExtents(out lower, out upper));
            bag.Add(General, "Color", () => ColorText(face.Color));
            return bag;
        }

        private static IADSurface SafeGeometry(IADFace face)
        {
            try { return face.Geometry; }
            catch (Exception) { return null; }
        }

        private static int AdjoiningFaceCount(IADFace face)
        {
            var own = KeyString(face.Key);
            var keys = new HashSet<string>();
            foreach (IADEdge edge in face.Edges)
            foreach (IADFace other in edge.Faces)
            {
                var key = KeyString(other.Key);
                if (key != own) keys.Add(key);
            }

            return keys.Count;
        }

        private PropertyBag ReadEdge(IADEdge edge)
        {
            var bag = new PropertyBag("Edge");
            bag.Add(General, "Type", () => "Edge");
            bag.Add(Geometry, "Curve Type", () => GeometryName(edge.Geometry.CurveType));
            bag.Add(Geometry, "Length", () => EdgeLength(edge));

            IADCurve curve = null;
            try { curve = edge.Geometry; }
            catch (Exception) { }
            var circle = curve as IADCircle;
            if (circle != null)
            {
                bag.Add(Geometry, "Radius", () => _context.Length(circle.Radius));
                bag.Add(Geometry, "Diameter", () => _context.Length(2 * circle.Radius));
                bag.Add(Geometry, "Center", () => _context.Point(circle.Center));
            }

            bag.Add(Geometry, "Is Closed", () => edge.Geometry.IsClosed.ToString());
            bag.Add(Geometry, "Vertex Count", () => VertexCount(edge).ToString());
            bag.Add(Geometry, "Start Vertex", () => edge.StartVertex == null ? "None" : _context.Point(edge.StartVertex.Point));
            bag.Add(Geometry, "End Vertex", () => edge.EndVertex == null ? "None" : _context.Point(edge.EndVertex.Point));
            bag.Add(Geometry, "Adjacent Face Count", () => edge.Faces.Count.ToString());
            return bag;
        }

        private static int VertexCount(IADEdge edge)
        {
            var start = edge.StartVertex;
            var end = edge.EndVertex;
            if (start == null) return end == null ? 0 : 1;
            if (end == null) return 1;
            return KeyString(start.Key) == KeyString(end.Key) ? 1 : 2;
        }

        /// <summary>
        /// AlibreX has no edge length; it is exact for straight edges and full
        /// circles, which covers what the script's Length showed for most edges.
        /// </summary>
        private string EdgeLength(IADEdge edge)
        {
            var curve = edge.Geometry;
            if (curve.CurveType == ADGeometryType.AD_LINE && edge.StartVertex != null && edge.EndVertex != null)
            {
                var a = edge.StartVertex.Point;
                var b = edge.EndVertex.Point;
                var dx = b.X - a.X;
                var dy = b.Y - a.Y;
                var dz = b.Z - a.Z;
                return _context.Length(Math.Sqrt(dx * dx + dy * dy + dz * dz));
            }

            var circle = curve as IADCircle;
            if (circle != null && VertexCount(edge) <= 1)
                return _context.Length(2 * Math.PI * circle.Radius);
            return "N/A (not a line or full circle)";
        }

        private PropertyBag ReadVertex(IADVertex vertex)
        {
            var bag = new PropertyBag("Vertex");
            bag.Add(General, "Type", () => "Vertex");
            bag.Add(Geometry, "X", () => _context.Length(vertex.Point.X));
            bag.Add(Geometry, "Y", () => _context.Length(vertex.Point.Y));
            bag.Add(Geometry, "Z", () => _context.Length(vertex.Point.Z));
            bag.Add(Geometry, "Edge Count", () => vertex.Edges.Count.ToString());
            bag.Add(Geometry, "Face Count", () => vertex.Faces.Count.ToString());
            return bag;
        }

        private PropertyBag ReadBody(IADBody body)
        {
            var bag = new PropertyBag("Body");
            bag.Add(General, "Type", () => "Body");
            bag.Add(Geometry, "Lump Count", () => body.Lumps.Count.ToString());
            bag.Add(Geometry, "Shell Count", () => body.Shells.Count.ToString());
            bag.Add(Geometry, "Face Count", () => body.Faces.Count.ToString());
            bag.Add(Geometry, "Edge Count", () => body.Edges.Count.ToString());
            bag.Add(Geometry, "Vertex Count", () => body.Vertices.Count.ToString());
            return bag;
        }

        // --- Sketches ---------------------------------------------------------------------

        private PropertyBag ReadSketch(IADSketch sketch)
        {
            var bag = new PropertyBag(DisplayName(sketch));
            bag.Add(General, "Name", () => sketch.Name);
            bag.Add(General, "Type", () => "Sketch");
            bag.Add(General, "Figure Count", () => sketch.Figures.Count.ToString());
            bag.Add(General, "Surface", () => sketch.SketchPlane.DisplayName);
            bag.Add(Geometry, "Plane Normal", () => DesignContext.Vector(sketch.SketchPlaneNormal));
            bag.Add(General, "Is Closed", () => sketch.IsClosed.ToString());
            bag.Add(General, "Is Active", () => sketch.IsActive.ToString());
            bag.Add(General, "Is Suppressed", () => sketch.IsSuppressed.ToString());
            bag.Add(General, "Is Consumed", () => sketch.IsConsumed.ToString());
            bag.Add(General, "Consuming Feature", () => sketch.IsConsumed ? sketch.ConsumingFeature.Name : "None");
            return bag;
        }

        private PropertyBag Read3DSketch(IAD3DSketch sketch)
        {
            var bag = new PropertyBag(DisplayName(sketch));
            bag.Add(General, "Name", () => sketch.Name);
            bag.Add(General, "Type", () => "3D Sketch");
            bag.Add(General, "Figure Count", () => sketch.Figures.Count.ToString());
            bag.Add(General, "Is Active", () => sketch.IsActive.ToString());
            bag.Add(General, "Is Suppressed", () => sketch.IsSuppressed.ToString());
            bag.Add(General, "Is Consumed", () => sketch.IsConsumed.ToString());
            bag.Add(General, "Consuming Feature", () => sketch.IsConsumed ? sketch.ConsumingFeature.Name : "None");
            return bag;
        }

        /// <summary>2D figure coordinates are in the sketch's own plane coordinates.</summary>
        private PropertyBag ReadFigure(IADSketchFigure figure)
        {
            var bag = new PropertyBag(FigureTypeName(figure));
            bag.Add(General, "Type", () => FigureTypeName(figure));

            var line = figure as IADSketchLine;
            var circle = figure as IADSketchCircle;
            var arc = figure as IADSketchCircularArc;
            var ellipse = figure as IADSketchEllipse;
            var ellipticArc = figure as IADSketchEllipticArc;
            var spline = figure as IADSketchBspline;
            var point = figure as IADSketchPoint;

            if (line != null)
            {
                bag.Add(Geometry, "Start Point", () => SketchPoint(line.Start));
                bag.Add(Geometry, "End Point", () => SketchPoint(line.End));
                bag.Add(Geometry, "Length", () => _context.Length(line.Length));
            }
            else if (circle != null)
            {
                bag.Add(Geometry, "Center", () => SketchPoint(circle.Center));
                bag.Add(Geometry, "Radius", () => _context.Length(circle.Radius));
                bag.Add(Geometry, "Length", () => _context.Length(2 * Math.PI * circle.Radius));
            }
            else if (arc != null)
            {
                bag.Add(Geometry, "Center", () => SketchPoint(arc.Center));
                bag.Add(Geometry, "Start Point", () => SketchPoint(arc.Start));
                bag.Add(Geometry, "End Point", () => SketchPoint(arc.End));
                bag.Add(Geometry, "Radius", () => _context.Length(arc.Radius));
                bag.Add(Geometry, "Angle", () => _context.Angle(arc.IncludedAngle));
                bag.Add(Geometry, "Length", () => _context.Length(arc.Radius * Math.Abs(arc.IncludedAngle)));
            }
            else if (ellipse != null)
            {
                bag.Add(Geometry, "Center", () => SketchPoint(ellipse.Center));
                bag.Add(Geometry, "Major Axis", () => _context.Length(ellipse.MajorAxis));
                bag.Add(Geometry, "Major Axis Angle", () => _context.Angle(ellipse.MajorAxisAngle));
                bag.Add(Geometry, "Minor/Major Ratio", () => DesignContext.Number(ellipse.MinorMajorRatio));
            }
            else if (ellipticArc != null)
            {
                bag.Add(Geometry, "Center", () => SketchPoint(ellipticArc.Center));
                bag.Add(Geometry, "Start Point", () => SketchPoint(ellipticArc.Start));
                bag.Add(Geometry, "End Point", () => SketchPoint(ellipticArc.End));
                bag.Add(Geometry, "Major Axis", () => _context.Length(ellipticArc.MajorAxis));
                bag.Add(Geometry, "Major Axis Angle", () => _context.Angle(ellipticArc.MajorAxisAngle));
                bag.Add(Geometry, "Minor/Major Ratio", () => DesignContext.Number(ellipticArc.MinorMajorRatio));
            }
            else if (spline != null)
            {
                bag.Add(Geometry, "Start Point", () => _context.Point(spline.StartPoint.X, spline.StartPoint.Y));
                bag.Add(Geometry, "End Point", () => _context.Point(spline.EndPoint.X, spline.EndPoint.Y));
                AddSplineData(bag,
                    (out int order, out int controlPoints, out int knots, out bool rational, out bool closed) =>
                    {
                        bool periodic;
                        spline.GetDefinition(out order, out controlPoints, out knots, out rational, out closed, out periodic);
                    },
                    (ref Array controlPoints, ref Array knots, ref Array weights) => spline.GetBsplineData(ref controlPoints, ref knots, ref weights),
                    2);
            }
            else if (point != null)
            {
                bag.Add(Geometry, "X", () => _context.Length(point.X));
                bag.Add(Geometry, "Y", () => _context.Length(point.Y));
            }

            bag.Add(General, "Is Reference", () => figure.IsReference.ToString());
            bag.Add(General, "Sketch", () => figure.Sketch.Name);
            return bag;
        }

        private string SketchPoint(IADSketchPoint p) => p == null ? "None" : _context.Point(p.X, p.Y);

        private PropertyBag Read3DFigure(IAD3DSketchFigure figure)
        {
            var bag = new PropertyBag(FigureTypeName(figure));
            bag.Add(General, "Type", () => FigureTypeName(figure));

            var line = figure as IAD3DSketchLine;
            var circle = figure as IAD3DSketchCircle;
            var arc = figure as IAD3DSketchCircularArc;
            var ellipticArc = figure as IAD3DSketchEllipticArc;
            var spline = figure as IAD3DSketchBspline;
            var point = figure as IAD3DSketchPoint;

            if (line != null)
            {
                bag.Add(Geometry, "Start Point", () => SketchPoint(line.Start));
                bag.Add(Geometry, "End Point", () => SketchPoint(line.End));
                bag.Add(Geometry, "Length", () => _context.Length(line.Length));
            }
            else if (circle != null)
            {
                bag.Add(Geometry, "Center", () => SketchPoint(circle.Center));
                bag.Add(Geometry, "Radius", () => _context.Length(circle.Radius));
                bag.Add(Geometry, "Normal", () => DesignContext.Vector(circle.Normal));
            }
            else if (arc != null)
            {
                bag.Add(Geometry, "Center", () => SketchPoint(arc.Center));
                bag.Add(Geometry, "Start Point", () => SketchPoint(arc.Start));
                bag.Add(Geometry, "End Point", () => SketchPoint(arc.End));
                bag.Add(Geometry, "Radius", () => _context.Length(arc.Radius));
                bag.Add(Geometry, "Angle", () => _context.Angle(arc.IncludedAngle));
            }
            else if (ellipticArc != null)
            {
                bag.Add(Geometry, "Center", () => SketchPoint(ellipticArc.Center));
                bag.Add(Geometry, "Start Point", () => SketchPoint(ellipticArc.Start));
                bag.Add(Geometry, "End Point", () => SketchPoint(ellipticArc.End));
                bag.Add(Geometry, "Major Radius Point", () => SketchPoint(ellipticArc.MajorRadiusPoint));
                bag.Add(Geometry, "Minor Radius Point", () => SketchPoint(ellipticArc.MinorRadiusPoint));
            }
            else if (spline != null)
            {
                bag.Add(Geometry, "Start Point", () => _context.Point(spline.StartPoint));
                bag.Add(Geometry, "End Point", () => _context.Point(spline.EndPoint));
                AddSplineData(bag,
                    (out int order, out int controlPoints, out int knots, out bool rational, out bool closed) =>
                    {
                        bool planar;
                        spline.GetDefinition(out order, out controlPoints, out knots, out rational, out closed, out planar);
                    },
                    (ref Array controlPoints, ref Array knots, ref Array weights) => spline.GetData(ref controlPoints, ref knots, ref weights),
                    3);
            }
            else if (point != null)
            {
                bag.Add(Geometry, "X", () => _context.Length(point.X));
                bag.Add(Geometry, "Y", () => _context.Length(point.Y));
                bag.Add(Geometry, "Z", () => _context.Length(point.Z));
            }

            bag.Add(General, "Is Reference", () => figure.IsReference.ToString());
            bag.Add(General, "Sketch", () => figure.Sketch.Name);
            return bag;
        }

        private string SketchPoint(IAD3DSketchPoint p) => p == null ? "None" : _context.Point(p.X, p.Y, p.Z);

        private delegate void SplineDefinition(out int order, out int controlPoints, out int knots, out bool rational, out bool closed);

        private delegate void SplineData(ref Array controlPoints, ref Array knots, ref Array weights);

        private void AddSplineData(PropertyBag bag, SplineDefinition definition, SplineData data, int dimensions)
        {
            bag.Add(Geometry, "Order", () =>
            {
                int order, controlPoints, knots;
                bool rational, closed;
                definition(out order, out controlPoints, out knots, out rational, out closed);
                return order.ToString();
            });
            bag.Add(Geometry, "Is Closed", () =>
            {
                int order, controlPoints, knots;
                bool rational, closed;
                definition(out order, out controlPoints, out knots, out rational, out closed);
                return closed.ToString();
            });

            Array ctl = null, knotVector = null, weights = null;
            string error = null;
            try { data(ref ctl, ref knotVector, ref weights); }
            catch (Exception e) { error = "Error: " + e.Message; }

            // Control points are a flat x, y[, z] list in model units.
            bag.Add(Geometry, "Control Points", () => error ?? JoinPoints(ctl, dimensions));
            bag.Add(Geometry, "Knot Vector", () => error ?? Join(knotVector));
            bag.Add(Geometry, "Weights", () => error ?? Join(weights));
        }

        private string JoinPoints(Array flat, int dimensions)
        {
            if (flat == null) return "None";
            var values = flat.Cast<object>().Select(Convert.ToDouble).ToArray();
            var points = new List<string>();
            for (var i = 0; i + dimensions <= values.Length; i += dimensions)
                points.Add(dimensions == 3
                    ? _context.Point(values[i], values[i + 1], values[i + 2])
                    : _context.Point(values[i], values[i + 1]));
            return points.Count + ": " + string.Join("; ", points);
        }

        private static string Join(Array values)
        {
            if (values == null) return "None";
            return string.Join(", ", values.Cast<object>().Select(v => DesignContext.Number(Convert.ToDouble(v))));
        }

        // --- Parameters, configurations, features, design geometry -------------------------

        private PropertyBag ReadParameter(IADParameter parameter)
        {
            var bag = new PropertyBag(DisplayName(parameter));
            bag.Add(General, "Name", () => parameter.Name);
            bag.Add(General, "Value", () => _context.ParameterValue(parameter));
            bag.Add(General, "Equation", () => parameter.Equation);
            bag.Add(General, "Comment", () => parameter.comment);
            bag.Add(General, "Type", () => ParameterTypeName(parameter.ParameterType));
            bag.Add(General, "Units", () => DesignContext.UnitName(parameter.Units));
            bag.Add(General, "Raw Value (model units)", () => RawParameterValue(parameter));
            bag.Add(General, "Externally Driven", () => parameter.ExternallyDriven.ToString());
            bag.Add(General, "Missing Global", () => parameter.IsMissingGlobal.ToString());
            bag.Add(General, "Conflicting Global", () => parameter.IsConflictingGlobal.ToString());
            return bag;
        }

        private static string RawParameterValue(IADParameter parameter)
        {
            var value = DesignContext.Number(parameter.Value);
            switch (parameter.ParameterType)
            {
                case ADParameterType.AD_DISTANCE: return value + " cm";
                case ADParameterType.AD_ANGLE: return value + " rad";
                default: return value;
            }
        }

        private static string ParameterTypeName(ADParameterType type)
        {
            switch (type)
            {
                case ADParameterType.AD_DISTANCE: return "Distance";
                case ADParameterType.AD_ANGLE: return "Angle";
                case ADParameterType.AD_COUNT: return "Count";
                case ADParameterType.AD_SCALE: return "Scale";
                default: return type.ToString();
            }
        }

        private static PropertyBag ReadConfiguration(IADConfiguration configuration)
        {
            var bag = new PropertyBag(DisplayName(configuration));
            bag.Add(General, "Name", () => configuration.Name);
            bag.Add(General, "Is Active", () => (configuration.DesignSession.ActiveConfiguration.ID == configuration.ID).ToString());
            bag.Add(General, "ID", () => configuration.ID.ToString());
            bag.Add(General, "Locks", () => LockNames(configuration.Locks));
            return bag;
        }

        private static string LockNames(ADConfigurationLockType locks)
        {
            // The values are single bits (1, 2, 4 … 2048) though the enum isn't [Flags].
            var names = Enum.GetValues(typeof(ADConfigurationLockType)).Cast<ADConfigurationLockType>()
                .Where(flag => (int) flag != 0 && (locks & flag) == flag)
                .Select(flag => flag.ToString().Replace("AD_", string.Empty).Replace('_', ' ').ToLowerInvariant())
                .ToList();
            return names.Count == 0 ? "None" : string.Join(", ", names);
        }

        private PropertyBag ReadFeature(IADPartFeature feature)
        {
            var bag = new PropertyBag(DisplayName(feature));
            bag.Add(General, "Name", () => feature.Name);
            bag.Add(General, "Feature Type", () => EnumName(feature.FeatureType.ToString(), "_FEATURE"));
            bag.Add(General, "Is Suppressed", () => feature.IsSuppressed.ToString());
            bag.Add(General, "Has Error", () => feature.HasError.ToString());
            bag.Add(General, "Is Active", () => feature.IsActive.ToString());
            bag.Add(General, "Is Sheet Metal Feature", () => feature.IsSheetMetalFeature.ToString());
            bag.Add(Geometry, "Face Count", () => feature.Faces.Count.ToString());
            AddExtents(bag, Geometry, "Bounding Box", (out IADPoint lower, out IADPoint upper) => feature.GetExtents(out lower, out upper));
            bag.Add(General, "Uses Part Color", () => feature.UsePartColor.ToString());
            bag.Add(General, "Face Color", () => ColorText(feature.FaceColor));
            return bag;
        }

        private static PropertyBag ReadAssemblyFeature(IADAssemblyFeature feature)
        {
            var bag = new PropertyBag(DisplayName(feature));
            bag.Add(General, "Name", () => feature.Name);
            bag.Add(General, "Feature Type", () => EnumName(feature.FeatureType.ToString(), "_FEATURE"));
            return bag;
        }

        private PropertyBag ReadPlane(IADDesignPlane plane)
        {
            var bag = new PropertyBag(DisplayName(plane));
            bag.Add(General, "Name", () => plane.Name);
            bag.Add(General, "Plane Type", () => EnumName(plane.PlaneType.ToString(), null));
            bag.Add(Geometry, "Normal", () => DesignContext.Vector(plane.Normal));
            bag.Add(Geometry, "Defining Points", () =>
            {
                IADPoint a, b, c;
                plane.GetGeometry(out a, out b, out c);
                return _context.Point(a) + "; " + _context.Point(b) + "; " + _context.Point(c);
            });
            return bag;
        }

        private PropertyBag ReadAxis(IADDesignAxis axis)
        {
            var bag = new PropertyBag(DisplayName(axis));
            bag.Add(General, "Name", () => axis.Name);
            bag.Add(General, "Axis Type", () => EnumName(axis.AxisType.ToString(), null));
            bag.Add(Geometry, "Direction", () => DesignContext.Vector(axis.Direction));
            bag.Add(Geometry, "Point", () =>
            {
                IADPoint point, vector;
                axis.GetGeometry(out point, out vector);
                return _context.Point(point);
            });
            return bag;
        }

        private PropertyBag ReadPoint(IADDesignPoint point)
        {
            var bag = new PropertyBag(DisplayName(point));
            bag.Add(General, "Name", () => point.Name);
            bag.Add(General, "Point Type", () => EnumName(point.PointType.ToString(), null));
            bag.Add(Geometry, "X", () => _context.Length(point.Geometry.X));
            bag.Add(Geometry, "Y", () => _context.Length(point.Geometry.Y));
            bag.Add(Geometry, "Z", () => _context.Length(point.Geometry.Z));
            return bag;
        }

        private static PropertyBag ReadGeneric(object item)
        {
            var bag = new PropertyBag(DisplayName(item));
            bag.Add(General, "Name", () => DisplayName(item));
            bag.Add(General, "Object Type", () => TypeName(item));
            return bag;
        }

        // --- Formatting -------------------------------------------------------------------

        private delegate void Extents(out IADPoint lower, out IADPoint upper);

        private void AddExtents(PropertyBag bag, string category, string name, Extents read)
        {
            IADPoint lower = null, upper = null;
            string error = null;
            try { read(out lower, out upper); }
            catch (Exception e) { error = "Error: " + e.Message; }
            bag.Add(category, name + " Min", () => error ?? _context.Point(lower));
            bag.Add(category, name + " Max", () => error ?? _context.Point(upper));
        }

        private static string Text(object value)
        {
            if (value == null) return "None";
            var array = value as Array;
            if (array != null) return string.Join(", ", array.Cast<object>());
            return Convert.ToString(value);
        }

        /// <summary>COLORREF 0x00BBGGRR → "R, G, B".</summary>
        private static string ColorText(int colorRef)
        {
            return (colorRef & 0xFF) + ", " + ((colorRef >> 8) & 0xFF) + ", " + ((colorRef >> 16) & 0xFF);
        }

        private static string GeometryName(ADGeometryType type) => EnumName(type.ToString(), null);

        /// <summary>"AD_CIRCULAR_ARC" → "Circular Arc".</summary>
        private static string EnumName(string raw, string dropSuffix)
        {
            if (raw.StartsWith("AD_")) raw = raw.Substring(3);
            if (dropSuffix != null && raw.EndsWith(dropSuffix)) raw = raw.Substring(0, raw.Length - dropSuffix.Length);
            var words = raw.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w == "SM" || w == "XYZ" ? w : char.ToUpperInvariant(w[0]) + w.Substring(1).ToLowerInvariant());
            return string.Join(" ", words);
        }
    }
}
