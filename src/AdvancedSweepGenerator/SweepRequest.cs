using AlibreX;

namespace AdvancedSweepGenerator
{
    internal enum ProfileShape
    {
        Circle,
        Square
    }

    /// <summary>
    /// What the dialog asks for. Exactly one of <see cref="PathSketch2D"/> and
    /// <see cref="PathSketch3D"/> is used, chosen by <see cref="Use3DPath"/>.
    /// Sizes are in millimetres, as entered; the builder converts to model units.
    /// </summary>
    internal sealed class SweepRequest
    {
        public bool Use3DPath { get; set; }
        public IADSketch PathSketch2D { get; set; }
        public IAD3DSketch PathSketch3D { get; set; }
        public ProfileShape Shape { get; set; }

        /// <summary>Outer diameter (circle) or side length (square), mm.</summary>
        public double SizeMm { get; set; }

        public bool Hollow { get; set; }

        /// <summary>Wall thickness when <see cref="Hollow"/>, mm.</summary>
        public double ThicknessMm { get; set; }
    }
}
