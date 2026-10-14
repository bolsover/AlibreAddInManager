using System;

namespace AdvancedSweepGenerator
{
    /// <summary>Minimal 3D vector for path/plane maths, in model units (cm).</summary>
    internal struct Vec3
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        public Vec3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

        public Vec3 Normalized()
        {
            var length = Length;
            return length > 0 ? this / length : this;
        }

        public static double Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        public static Vec3 Cross(Vec3 a, Vec3 b) =>
            new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

        /// <summary>A unit vector perpendicular to <paramref name="v"/>.</summary>
        public static Vec3 AnyPerpendicular(Vec3 v)
        {
            // Cross with the world axis least aligned with v, so the result is never degenerate.
            var ax = Math.Abs(v.X);
            var ay = Math.Abs(v.Y);
            var az = Math.Abs(v.Z);
            var axis = ax <= ay && ax <= az ? new Vec3(1, 0, 0)
                : ay <= az ? new Vec3(0, 1, 0)
                : new Vec3(0, 0, 1);
            return Cross(v, axis).Normalized();
        }

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator -(Vec3 a) => new Vec3(-a.X, -a.Y, -a.Z);
        public static Vec3 operator *(Vec3 a, double s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public static Vec3 operator /(Vec3 a, double s) => new Vec3(a.X / s, a.Y / s, a.Z / s);
    }
}
