using System;

namespace OriginalCircuit.AltiumSharp.BasicTypes
{
    public readonly struct CoordPoint : IEquatable<CoordPoint>
    {
        public static readonly CoordPoint Zero = new CoordPoint();

        public Coord X { get; }
        public Coord Y { get; }
        public CoordPoint(Coord x, Coord y) => (X, Y) = (x, y);
        public void Deconstruct(out Coord x, out Coord y) => (x, y) = (X, Y);

        public static CoordPoint FromMils(double milsX, double milsY) =>
            new CoordPoint(Coord.FromMils(milsX), Coord.FromMils(milsY));
        public static CoordPoint FromMMs(double mmsX, double mmsY) =>
            new CoordPoint(Coord.FromMMs(mmsX), Coord.FromMMs(mmsY));

        public override string ToString() => $"X:{X} Y:{Y}";
        public string ToString(Unit unit) => $"X:{X.ToString(unit)} Y:{Y.ToString(unit)}";
        public string ToString(Unit unit, Coord grid) => $"X:{X.ToString(unit, grid)} Y:{Y.ToString(unit, grid)}";

        #region 'boilerplate'
        public override bool Equals(object obj) => obj is CoordPoint other && Equals(other);
        public bool Equals(CoordPoint other) => X == other.X && Y == other.Y;
        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();
        public static bool operator ==(CoordPoint left, CoordPoint right) => left.Equals(right);
        public static bool operator !=(CoordPoint left, CoordPoint right) => !(left == right);
        #endregion
    }
}
