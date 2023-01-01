using System;

namespace OriginalCircuit.AltiumSharp.BasicTypes
{
    public readonly struct CoordPoint3D : IEquatable<CoordPoint3D>
    {
        public static readonly CoordPoint3D Zero = new CoordPoint3D();

        public Coord X { get; }
        public Coord Y { get; }
        public Coord Z { get; }
        public CoordPoint3D(Coord x, Coord y, Coord z) => (X, Y, Z) = (x, y, z);
        public void Deconstruct(out Coord x, out Coord y, out Coord z) => (x, y, z) = (X, Y, Z);

        public static CoordPoint3D FromMils(double milsX, double milsY, double milsZ) =>
            new CoordPoint3D(Coord.FromMils(milsX), Coord.FromMils(milsY), Coord.FromMils(milsZ));
        public static CoordPoint3D FromMMs(double mmsX, double mmsY, double mmsZ) =>
            new CoordPoint3D(Coord.FromMMs(mmsX), Coord.FromMMs(mmsY), Coord.FromMMs(mmsZ));

        public override string ToString() => $"X:{X} Y:{Y} Z:{Z}";
        public string ToString(Unit unit) => $"X:{X.ToString(unit)} Y:{Y.ToString(unit)} Z:{Z.ToString(unit)}";
        public string ToString(Unit unit, Coord grid) => $"X:{X.ToString(unit, grid)} Y:{Y.ToString(unit, grid)} Z:{Z.ToString(unit, grid)}";

        #region 'boilerplate'
        public override bool Equals(object obj) => obj is CoordPoint3D other && Equals(other);
        public bool Equals(CoordPoint3D other) => X == other.X && Y == other.Y && Z == other.Z;
        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        public static bool operator ==(CoordPoint3D left, CoordPoint3D right) => left.Equals(right);
        public static bool operator !=(CoordPoint3D left, CoordPoint3D right) => !(left == right);
        #endregion
    }
}
