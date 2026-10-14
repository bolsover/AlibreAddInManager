using System;
using System.Globalization;
using AlibreX;

namespace CustomPropertyExplorer.Model
{
    /// <summary>
    /// The part or assembly workspace a command was invoked in, plus the unit
    /// conversions for showing its values. The API works in model units —
    /// centimetres and radians — which are converted to the workspace's display
    /// units only here, at the UI boundary.
    /// </summary>
    internal sealed class DesignContext
    {
        private DesignContext(string sessionIdentifier, IADDesignSession design, bool isAssembly)
        {
            SessionIdentifier = sessionIdentifier;
            Design = design;
            IsAssembly = isAssembly;
            Part = design as IADPartSession;
            Assembly = design as IADAssemblySession;
        }

        public string SessionIdentifier { get; }
        public IADDesignSession Design { get; }
        public IADPartSession Part { get; }
        public IADAssemblySession Assembly { get; }
        public bool IsAssembly { get; }

        public string Name
        {
            get
            {
                try { return Design.Name; }
                catch (Exception) { return "(unknown)"; }
            }
        }

        /// <summary>
        /// Resolves the workspace a call is about. The Home window passes an empty
        /// identifier and Sessions.Item throws for unknown ones, so both yield null.
        /// Part and Sheet Metal sessions both implement IADPartSession.
        /// </summary>
        public static DesignContext TryResolve(IADRoot root, string sessionIdentifier)
        {
            if (root == null || string.IsNullOrEmpty(sessionIdentifier)) return null;
            try
            {
                var session = root.Sessions.Item(sessionIdentifier);
                if (session == null) return null;
                switch (session.SessionType)
                {
                    case ADObjectSubType.AD_PART:
                    case ADObjectSubType.AD_SHEET_METAL:
                        var part = session as IADPartSession;
                        return part == null ? null : new DesignContext(sessionIdentifier, part, false);
                    case ADObjectSubType.AD_ASSEMBLY:
                        var assembly = session as IADAssemblySession;
                        return assembly == null ? null : new DesignContext(sessionIdentifier, assembly, true);
                    default:
                        return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>True while the session is still open in Alibre.</summary>
        public bool IsOpen(IADRoot root)
        {
            try { return root.Sessions.Item(SessionIdentifier) != null; }
            catch (Exception) { return false; }
        }

        // --- Units -----------------------------------------------------------------

        private ADUnits LengthUnits
        {
            get
            {
                try { return Design.DesignProperties.LengthDisplayUnits; }
                catch (Exception) { return ADUnits.AD_MILLIMETERS; }
            }
        }

        private ADUnits AngleUnits
        {
            get
            {
                try { return Design.DesignProperties.AngleDisplayUnits; }
                catch (Exception) { return ADUnits.AD_DEGREES; }
            }
        }

        /// <summary>Display units per centimetre, and the suffix to print.</summary>
        private static void LengthScale(ADUnits units, out double perCm, out string suffix)
        {
            switch (units)
            {
                case ADUnits.AD_MILLIMETERS: perCm = 10.0; suffix = "mm"; break;
                case ADUnits.AD_METERS: perCm = 0.01; suffix = "m"; break;
                case ADUnits.AD_INCHES:
                case ADUnits.AD_FEET_INCHES: perCm = 1.0 / 2.54; suffix = "in"; break;
                case ADUnits.AD_FEET: perCm = 1.0 / 30.48; suffix = "ft"; break;
                default: perCm = 1.0; suffix = "cm"; break;
            }
        }

        public static string Number(double value) => value.ToString("0.####", CultureInfo.CurrentCulture);

        /// <summary>A length given in centimetres, in display units.</summary>
        public string Length(double cm)
        {
            double perCm; string suffix;
            LengthScale(LengthUnits, out perCm, out suffix);
            return Number(cm * perCm) + " " + suffix;
        }

        public string Area(double squareCm)
        {
            double perCm; string suffix;
            LengthScale(LengthUnits, out perCm, out suffix);
            return Number(squareCm * perCm * perCm) + " " + suffix + "²";
        }

        public string Volume(double cubicCm)
        {
            double perCm; string suffix;
            LengthScale(LengthUnits, out perCm, out suffix);
            return Number(cubicCm * perCm * perCm * perCm) + " " + suffix + "³";
        }

        /// <summary>An angle given in radians, in display units.</summary>
        public string Angle(double radians)
        {
            return AngleUnits == ADUnits.AD_RADIANS
                ? Number(radians) + " rad"
                : Number(radians * 180.0 / Math.PI) + "°";
        }

        public string Point(double xCm, double yCm, double zCm)
        {
            return "(" + Length(xCm) + ", " + Length(yCm) + ", " + Length(zCm) + ")";
        }

        public string Point(double xCm, double yCm)
        {
            return "(" + Length(xCm) + ", " + Length(yCm) + ")";
        }

        public string Point(IADPoint p) => p == null ? "None" : Point(p.X, p.Y, p.Z);

        /// <summary>A direction; unitless, so no conversion.</summary>
        public static string Vector(IADVector v)
        {
            return v == null ? "None" : "(" + Number(v.X) + ", " + Number(v.Y) + ", " + Number(v.Z) + ")";
        }

        /// <summary>
        /// A parameter value in model units, shown in the parameter's own units.
        /// Counts and scales are unitless.
        /// </summary>
        public string ParameterValue(IADParameter parameter)
        {
            var value = parameter.Value;
            switch (parameter.ParameterType)
            {
                case ADParameterType.AD_DISTANCE:
                    double perCm; string suffix;
                    LengthScale(parameter.Units, out perCm, out suffix);
                    return Number(value * perCm) + " " + suffix;
                case ADParameterType.AD_ANGLE:
                    return parameter.Units == ADUnits.AD_RADIANS
                        ? Number(value) + " rad"
                        : Number(value * 180.0 / Math.PI) + "°";
                default:
                    return Number(value);
            }
        }

        public static string UnitName(ADUnits units)
        {
            switch (units)
            {
                case ADUnits.AD_UNITLESS: return "Unitless";
                case ADUnits.AD_DEGREES: return "Degrees";
                case ADUnits.AD_DEGREES_MINUTES: return "Degrees, minutes";
                case ADUnits.AD_DEGREES_MINUTES_SECONDS: return "Degrees, minutes, seconds";
                case ADUnits.AD_RADIANS: return "Radians";
                case ADUnits.AD_MILLIMETERS: return "Millimeters";
                case ADUnits.AD_CENTIMETERS: return "Centimeters";
                case ADUnits.AD_METERS: return "Meters";
                case ADUnits.AD_INCHES: return "Inches";
                case ADUnits.AD_FEET: return "Feet";
                case ADUnits.AD_FEET_INCHES: return "Feet, inches";
                case ADUnits.AD_KILOGRAMS: return "Kilograms";
                case ADUnits.AD_GRAMS: return "Grams";
                case ADUnits.AD_POUNDMASS: return "Pound-mass";
                default: return units.ToString();
            }
        }
    }
}
