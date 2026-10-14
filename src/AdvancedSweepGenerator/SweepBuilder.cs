using System;
using System.Collections.Generic;
using AlibreX;

namespace AdvancedSweepGenerator
{
    /// <summary>A user-facing failure: the message is shown as is.</summary>
    internal sealed class SweepException : Exception
    {
        public SweepException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Builds the sweep: finds where the path starts and which way it leaves,
    /// creates a design plane normal to the path there, sketches the profile on
    /// it and sweeps the profile along the path. Everything is in model units
    /// (cm); only <see cref="SweepRequest"/> carries millimetres.
    /// </summary>
    internal sealed class SweepBuilder
    {
        private const double MmToCm = 0.1;
        private const double Tolerance = 1e-9;

        private const string PlaneBaseName = "SweepProfilePlane";
        private const string SketchBaseName = "SweepProfile";
        private const string FeatureBaseName = "AdvancedSweep";

        private readonly IADRoot _root;
        private readonly IADPartSession _part;

        public SweepBuilder(IADRoot root, IADPartSession part)
        {
            _root = root;
            _part = part;
        }

        /// <summary>Creates the sweep and returns the new feature's name.</summary>
        public string Build(SweepRequest request)
        {
            var size = request.SizeMm * MmToCm;
            var thickness = request.ThicknessMm * MmToCm;
            Validate(request, size, thickness);

            object path;
            PathStart start;
            if (request.Use3DPath)
            {
                path = request.PathSketch3D;
                start = FindStart(request.PathSketch3D);
            }
            else
            {
                path = request.PathSketch2D;
                start = FindStart(request.PathSketch2D);
            }

            // Undo steps for the construction geometry, run in reverse if anything fails.
            var undo = new Stack<Action>();
            IADSketch profile = null;
            try
            {
                var plane = CreateProfilePlane(start, size, undo);
                profile = CreateProfileSketch(plane, start.Point, request, size, thickness, undo);

                var pathCollector = _root.NewObjectCollector();
                pathCollector.Add(path);

                var name = UniqueName(FeatureBaseName, FeatureNames());
                var feature = _part.Features.AddSweptBoss(profile, pathCollector, false,
                    ADPartFeatureEndCondition.AD_ENTIRE_PATH, null, null, 0.0, 0.0, false, name);
                return feature?.Name ?? name;
            }
            catch (Exception e)
            {
                // AddSweptBoss leaves a failed feature in the tree, consuming the
                // profile; then the plane and sketch belong to it and must stay.
                if (profile != null && IsConsumed(profile))
                    throw new SweepException("Alibre created the sweep but reported an error:\n\n" + e.Message +
                                             "\n\nEdit or delete the sweep in the Design Explorer.");
                while (undo.Count > 0)
                {
                    try { undo.Pop()(); }
                    catch (Exception) { /* best effort; report the original failure */ }
                }

                throw;
            }
        }

        private static void Validate(SweepRequest request, double size, double thickness)
        {
            if (request.Use3DPath ? request.PathSketch3D == null : request.PathSketch2D == null)
                throw new SweepException("A path sketch must be selected.");
            if (size <= 0)
                throw new SweepException("The profile size must be greater than zero.");
            if (request.Hollow)
            {
                if (thickness <= 0)
                    throw new SweepException("The wall thickness must be greater than zero.");
                if (thickness >= size / 2.0)
                    throw new SweepException("The wall thickness must be less than half the profile size.");
            }
        }

        // ---- Path start ---------------------------------------------------------

        /// <summary>A point on the path and the unit direction the path leaves it in.</summary>
        private struct PathStart
        {
            public PathStart(Vec3 point, Vec3 tangent)
            {
                if (tangent.Length < Tolerance)
                    throw new SweepException("Could not determine the direction of the path at its start point.");
                Point = point;
                Tangent = tangent.Normalized();
            }

            public readonly Vec3 Point;
            public readonly Vec3 Tangent;
        }

        /// <summary>
        /// Start of the first real figure of a 2D sketch, worked out in sketch (u, v)
        /// coordinates and mapped to world. The path need not start there: a sweep
        /// only needs its path to pass through the profile plane.
        /// </summary>
        private static PathStart FindStart(IADSketch sketch)
        {
            IADSketchFigure first = null;
            foreach (IADSketchFigure figure in sketch.Figures)
            {
                if (figure is IADSketchPoint || IsReference(figure)) continue;
                first = figure;
                break;
            }

            if (first == null)
                throw new SweepException("The 2D path sketch contains no figures.");

            double su, sv, tu, tv;
            if (first is IADSketchLine line)
            {
                su = line.Start.X;
                sv = line.Start.Y;
                tu = line.End.X - su;
                tv = line.End.Y - sv;
            }
            else if (first is IADSketchCircularArc arc)
            {
                su = arc.Start.X;
                sv = arc.Start.Y;
                var ru = su - arc.Center.X;
                var rv = sv - arc.Center.Y;
                // Right-hand rule about the sketch normal is counter-clockwise in (u, v).
                if (arc.IsRightHandRule) { tu = -rv; tv = ru; }
                else { tu = rv; tv = -ru; }
            }
            else if (first is IADSketchCircle circle)
            {
                su = circle.Center.X + circle.Radius;
                sv = circle.Center.Y;
                tu = 0;
                tv = 1;
            }
            else if (first is IADSketchBspline spline)
            {
                su = spline.StartPoint.X;
                sv = spline.StartPoint.Y;
                // A clamped b-spline leaves its start towards the second control point.
                var control = TryGetControlPoints(spline);
                if (control != null && control.Length >= 4)
                {
                    tu = control[2] - control[0];
                    tv = control[3] - control[1];
                }
                else
                {
                    tu = spline.EndPoint.X - su;
                    tv = spline.EndPoint.Y - sv;
                }
            }
            else if (first is IADSketchEllipticArc ellipticArc)
            {
                // No tangent is exposed for elliptic arcs; use the chord, as the
                // original script does for figures without GetPointAt.
                su = ellipticArc.Start.X;
                sv = ellipticArc.Start.Y;
                tu = ellipticArc.End.X - su;
                tv = ellipticArc.End.Y - sv;
            }
            else
            {
                throw new SweepException("The first figure of the 2D path sketch is not a line, arc, circle or spline.");
            }

            var start = SketchToWorld(sketch, su, sv);
            var length = Math.Sqrt(tu * tu + tv * tv);
            if (length < Tolerance)
                throw new SweepException("Could not determine the direction of the path at its start point.");
            var ahead = SketchToWorld(sketch, su + tu / length, sv + tv / length);
            return new PathStart(start, ahead - start);
        }

        /// <summary>Start of the first real figure of a 3D sketch, in world coordinates.</summary>
        private static PathStart FindStart(IAD3DSketch sketch)
        {
            IAD3DSketchFigure first = null;
            foreach (IAD3DSketchFigure figure in sketch.Figures)
            {
                if (figure is IAD3DSketchPoint || IsReference(figure)) continue;
                first = figure;
                break;
            }

            if (first == null)
                throw new SweepException("The 3D path sketch contains no figures.");

            if (first is IAD3DSketchLine line)
            {
                var s = ToVec(line.Start);
                return new PathStart(s, ToVec(line.End) - s);
            }

            if (first is IAD3DSketchCircularArc arc)
            {
                var c = ToVec(arc.Center);
                var r = ToVec(arc.Start) - c;
                // The arc's normal from its two radii, flipped for the major arc.
                var n = Vec3.Cross(r, ToVec(arc.End) - c);
                if (n.Length < Tolerance * Vec3.Dot(r, r))
                    throw new SweepException("The path starts with a 180° 3D arc, whose direction is ambiguous. " +
                                             "Split the arc or start the path with another figure.");
                if (arc.IncludedAngle > Math.PI) n = -n;
                return new PathStart(c + r, Vec3.Cross(n, r));
            }

            if (first is IAD3DSketchCircle circle)
            {
                var n = ToVec(circle.Normal).Normalized();
                var u = Vec3.AnyPerpendicular(n);
                return new PathStart(ToVec(circle.Center) + u * circle.Radius, Vec3.Cross(n, u));
            }

            if (first is IAD3DSketchBspline spline)
            {
                var s = ToVec(spline.StartPoint);
                var control = TryGetControlPoints(spline);
                if (control != null && control.Length >= 6)
                {
                    var tangent = new Vec3(control[3] - control[0], control[4] - control[1], control[5] - control[2]);
                    if (tangent.Length >= Tolerance) return new PathStart(s, tangent);
                }

                return new PathStart(s, ToVec(spline.EndPoint) - s);
            }

            throw new SweepException("The first figure of the 3D path sketch is not a line, arc, circle or spline.");
        }

        /// <summary>Flat (u, v) control-point coordinates, or null if they cannot be read.</summary>
        private static double[] TryGetControlPoints(IADSketchBspline spline)
        {
            try
            {
                // The data arrays are caller-allocated, sized from GetDefinition.
                int order, controlCount, knotCount;
                bool rational, closed, periodic;
                spline.GetDefinition(out order, out controlCount, out knotCount, out rational, out closed, out periodic);
                Array control = new double[2 * controlCount];
                Array knots = new double[knotCount];
                Array weights = new double[controlCount];
                spline.GetBsplineData(ref control, ref knots, ref weights);
                return ToDoubles(control);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Flat (x, y, z) control-point coordinates, or null if they cannot be read.</summary>
        private static double[] TryGetControlPoints(IAD3DSketchBspline spline)
        {
            try
            {
                int order, controlCount, knotCount;
                bool rational, closed, planar;
                spline.GetDefinition(out order, out controlCount, out knotCount, out rational, out closed, out planar);
                Array control = new double[3 * controlCount];
                Array knots = new double[knotCount];
                Array weights = new double[controlCount];
                spline.GetData(ref control, ref knots, ref weights);
                return ToDoubles(control);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static double[] ToDoubles(Array values)
        {
            if (values == null) return null;
            var result = new double[values.Length];
            var i = 0;
            foreach (var value in values) result[i++] = Convert.ToDouble(value);
            return result;
        }

        // ---- Construction geometry ----------------------------------------------

        /// <summary>
        /// A design plane through the path start, normal to the path. AlibreX has
        /// no point-and-normal plane, so three hidden design points define it:
        /// the start, and the start offset along two vectors perpendicular to the path.
        /// </summary>
        private IADDesignPlane CreateProfilePlane(PathStart start, double size, Stack<Action> undo)
        {
            var u = Vec3.AnyPerpendicular(start.Tangent);
            var v = Vec3.Cross(start.Tangent, u); // u × v = tangent
            var p0 = CreateHiddenPoint(start.Point, undo);
            var p1 = CreateHiddenPoint(start.Point + u * size, undo);
            var p2 = CreateHiddenPoint(start.Point + v * size, undo);

            var name = UniqueName(PlaneBaseName, PlaneNames());
            var plane = _part.DesignPlanes.CreateBy3Points(null, p0, null, p1, null, p2, name);
            undo.Push(() => plane.Delete());
            return plane;
        }

        private IADDesignPoint CreateHiddenPoint(Vec3 at, Stack<Action> undo)
        {
            var point = _part.DesignPoints.CreatePoint(at.X, at.Y, at.Z, null);
            undo.Push(() => point.Delete());
            try { point.Hide(); }
            catch (Exception) { /* cosmetic only */ }
            return point;
        }

        private IADSketch CreateProfileSketch(IADDesignPlane plane, Vec3 center, SweepRequest request,
            double size, double thickness, Stack<Action> undo)
        {
            var name = UniqueName(SketchBaseName, SketchNames());
            var sketch = _part.Sketches.AddSketch(null, plane, name);
            undo.Push(() => sketch.Delete());

            double cx, cy;
            sketch.MapFromWorldToSketch(_part.GeometryFactory.CreatePoint(center.X, center.Y, center.Z), out cx, out cy);

            sketch.BeginChange();
            try
            {
                var figures = sketch.Figures;
                var outer = size / 2.0;
                AddProfile(figures, request.Shape, cx, cy, outer);
                if (request.Hollow)
                    AddProfile(figures, request.Shape, cx, cy, outer - thickness);
            }
            finally
            {
                sketch.EndChange();
            }

            return sketch;
        }

        /// <summary>A circle of radius <paramref name="half"/>, or a square of half-side <paramref name="half"/>.</summary>
        private static void AddProfile(IADSketchFigures figures, ProfileShape shape, double cx, double cy, double half)
        {
            if (shape == ProfileShape.Circle)
                figures.AddCircle(cx, cy, half);
            else
                figures.AddRectangle(cx - half, cy - half, cx + half, cy + half);
        }

        // ---- Helpers ------------------------------------------------------------

        private static Vec3 SketchToWorld(IADSketch sketch, double u, double v)
        {
            IADPoint point;
            sketch.MapFromSketchToWorld(u, v, out point);
            return ToVec(point);
        }

        private static Vec3 ToVec(IADPoint p) => new Vec3(p.X, p.Y, p.Z);
        private static Vec3 ToVec(IAD3DSketchPoint p) => new Vec3(p.X, p.Y, p.Z);
        private static Vec3 ToVec(IADVector v) => new Vec3(v.X, v.Y, v.Z);

        private static bool IsReference(IADSketchFigure figure)
        {
            try { return figure.IsReference; }
            catch (Exception) { return false; }
        }

        private static bool IsReference(IAD3DSketchFigure figure)
        {
            try { return figure.IsReference; }
            catch (Exception) { return false; }
        }

        private static bool IsConsumed(IADSketch sketch)
        {
            try { return sketch.IsConsumed; }
            catch (Exception) { return false; }
        }

        private IEnumerable<string> FeatureNames()
        {
            foreach (IADPartFeature feature in _part.Features) yield return feature.Name;
        }

        private IEnumerable<string> PlaneNames()
        {
            foreach (IADDesignPlane plane in _part.DesignPlanes) yield return plane.Name;
        }

        private IEnumerable<string> SketchNames()
        {
            foreach (IADSketch sketch in _part.Sketches) yield return sketch.Name;
        }

        /// <summary><paramref name="baseName"/>, or "baseName 2", "baseName 3", … if taken.</summary>
        private static string UniqueName(string baseName, IEnumerable<string> existing)
        {
            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var name in existing) taken.Add(name);
            }
            catch (Exception)
            {
                // Names are cosmetic; on failure let Alibre name the object instead.
                return null;
            }

            if (!taken.Contains(baseName)) return baseName;
            for (var i = 2;; i++)
            {
                var candidate = baseName + " " + i;
                if (!taken.Contains(candidate)) return candidate;
            }
        }
    }
}
