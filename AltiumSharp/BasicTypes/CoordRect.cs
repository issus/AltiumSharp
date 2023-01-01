using System;
using System.Collections.Generic;
using System.Linq;

namespace OriginalCircuit.AltiumSharp.BasicTypes
{
    public readonly struct CoordRect : IEquatable<CoordRect>
    {
        public static readonly CoordRect Empty = new CoordRect();
        public CoordPoint Location1 { get; }
        public CoordPoint Location2 { get; }
        public Coord Width => Location2.X - Location1.X;
        public Coord Height => Location2.Y - Location1.Y;
        public bool IsEmpty => Width == 0 && Height == 0;
        public CoordPoint Center =>
            new CoordPoint((Location1.X + Location2.X) / 2, (Location1.Y + Location2.Y) / 2);

        public CoordRect(CoordPoint loc1, CoordPoint loc2)
        {
            Location1 = new CoordPoint(Math.Min(loc1.X, loc2.X), Math.Min(loc1.Y, loc2.Y));
            Location2 = new CoordPoint(Math.Max(loc1.X, loc2.X), Math.Max(loc1.Y, loc2.Y));
        }

        public CoordRect(Coord x, Coord y, Coord w, Coord h) :
            this(new CoordPoint(x, y), new CoordPoint(x + w, y + h))
        {
        }

        public void Deconstruct(out CoordPoint location1, out CoordPoint location2) =>
            (location1, location2) = (Location1, Location2);

        public void Deconstruct(out Coord x, out Coord y, out Coord w, out Coord h) =>
            (x, y, w, h) = (Location1.X, Location1.Y, Width, Height);

        public bool Contains(in CoordPoint point) =>
            Location1.X <= point.X && point.X <= Location2.X &&
            Location1.Y <= point.Y && point.Y <= Location2.Y;

        public bool Intersects(in CoordRect other) =>
            Location1.X <= other.Location2.X && Location2.X >= other.Location1.X &&
            Location1.Y <= other.Location2.Y && Location2.Y >= other.Location1.Y;

        public CoordPoint[] GetPoints()
        {
            return new[] { Location1, new CoordPoint(Location2.X, Location1.Y),
                           Location2, new CoordPoint(Location1.X, Location2.Y) };
        }

        public CoordPoint[] RotatedPoints(CoordPoint anchorPoint, double rotationDegrees)
        {
            var points = GetPoints();
            return Utils.RotatePoints(ref points, anchorPoint, rotationDegrees);
        }

        public override string ToString() => $"({Location1} {Location2})";

        public static CoordRect FromRotatedRect(in CoordRect coordRect, in CoordPoint anchorPoint, double rotationDegrees)
        {
            var points = coordRect.RotatedPoints(anchorPoint, rotationDegrees);
            return new CoordRect(new CoordPoint(points.Min(p => p.X), points.Min(p => p.Y)),
                                 new CoordPoint(points.Max(p => p.X), points.Max(p => p.Y)));
        }

        public static CoordRect Union(in CoordRect left, in CoordRect right)
        {
            if (left.IsEmpty)
            {
                return right;
            }
            else if (right.IsEmpty)
            {
                return left;
            }
            else
            {
                return new CoordRect(
                    new CoordPoint(Math.Min(left.Location1.X, right.Location1.X),
                                   Math.Min(left.Location1.Y, right.Location1.Y)),
                    new CoordPoint(Math.Max(left.Location2.X, right.Location2.X),
                                   Math.Max(left.Location2.Y, right.Location2.Y)));
            }
        }

        public static CoordRect Union(IEnumerable<CoordRect> collection)
        {
            return collection.Aggregate(Empty, (acc, rect) => Union(acc, rect));
        }

        #region 'boilerplate'
        public override bool Equals(object obj) => obj is CoordRect other && Equals(other);
        public bool Equals(CoordRect other) => Location1 == other.Location1 && Location2 == other.Location2;
        public override int GetHashCode() => Location1.GetHashCode() ^ Location2.GetHashCode();
        public static bool operator ==(CoordRect left, CoordRect right) => left.Equals(right);
        public static bool operator !=(CoordRect left, CoordRect right) => !(left == right);
        #endregion
    }
}
