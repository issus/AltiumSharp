namespace OriginalCircuit.Altium.Rendering.Gltf.Geometry;

/// <summary>
/// Triangulates a simple polygon, optionally with holes, into a triangle-index list. This is a
/// managed implementation of the well-known ear-clipping algorithm with hole bridging and a
/// z-order acceleration structure for large rings (the "earcut" approach). It is robust for the
/// concave, multiply-connected outlines that arise from board edges, copper regions and pours.
/// </summary>
internal static class Triangulator
{
    /// <summary>
    /// Triangulates <paramref name="outer"/> (with optional <paramref name="holes"/>) and returns a
    /// flat list of triangle indices. Indices address a virtual vertex list formed by concatenating
    /// the outer ring followed by each hole ring, in order — the caller resolves them back to points.
    /// </summary>
    public static List<int> Triangulate(IReadOnlyList<Vec2> outer, IReadOnlyList<IReadOnlyList<Vec2>>? holes = null)
    {
        var pts = new List<Vec2>(outer);
        var holeIndices = new List<int>();
        if (holes is not null)
        {
            foreach (var h in holes)
            {
                if (h.Count < 3) continue;
                holeIndices.Add(pts.Count);
                pts.AddRange(h);
            }
        }

        var triangles = new List<int>();
        if (pts.Count < 3) return triangles;

        // Normalise to a local origin. The ear-clip's area / point-in-triangle tests subtract like-magnitude
        // terms, so a polygon far from the origin (e.g. an outer board on a big panel, ~tens of mm off
        // centre) loses precision and mis-triangulates — leaving a spanning triangle or a dropped region for
        // that one instance. Shifting to the polygon's min corner restores precision; the returned indices
        // are unchanged, so the caller still resolves them against the original (un-shifted) points.
        double ox = double.MaxValue, oy = double.MaxValue;
        foreach (var p in pts) { if (p.X < ox) ox = p.X; if (p.Y < oy) oy = p.Y; }
        for (int i = 0; i < pts.Count; i++) pts[i] = new Vec2(pts[i].X - ox, pts[i].Y - oy);

        int outerLen = outer.Count;
        Node? outerNode = BuildLinkedList(pts, 0, outerLen, clockwise: true);
        if (outerNode is null || outerNode.Next == outerNode.Prev) return triangles;

        if (holeIndices.Count > 0)
            outerNode = EliminateHoles(pts, holeIndices, outerNode);

        double minX = 0, minY = 0, invSize = 0;
        bool hashing = pts.Count > 80;
        if (hashing)
        {
            double maxX, maxY;
            minX = maxX = pts[0].X;
            minY = maxY = pts[0].Y;
            for (int i = 1; i < pts.Count; i++)
            {
                if (pts[i].X < minX) minX = pts[i].X;
                if (pts[i].Y < minY) minY = pts[i].Y;
                if (pts[i].X > maxX) maxX = pts[i].X;
                if (pts[i].Y > maxY) maxY = pts[i].Y;
            }
            invSize = Math.Max(maxX - minX, maxY - minY);
            invSize = invSize != 0 ? 32767.0 / invSize : 0;
        }

        EarcutLinked(outerNode, triangles, minX, minY, invSize, 0);
        return triangles;
    }

    private sealed class Node(int i, double x, double y)
    {
        public readonly int I = i;
        public readonly double X = x;
        public readonly double Y = y;
        public Node Prev = null!;
        public Node Next = null!;
        public double Z = 0;
        public Node? PrevZ;
        public Node? NextZ;
        public bool Steiner;
    }

    // Creates a circular doubly-linked list from a ring [start, end), forcing the requested winding.
    private static Node? BuildLinkedList(List<Vec2> pts, int start, int end, bool clockwise)
    {
        Node? last = null;
        if (clockwise == SignedArea(pts, start, end) > 0)
        {
            for (int i = start; i < end; i++) last = InsertNode(i, pts[i], last);
        }
        else
        {
            for (int i = end - 1; i >= start; i--) last = InsertNode(i, pts[i], last);
        }

        if (last is not null && Equals(last, last.Next))
        {
            RemoveNode(last);
            last = last.Next;
        }
        return last;
    }

    // Removes duplicate/colinear points; returns a (possibly different) surviving node.
    private static Node? FilterPoints(Node? start, Node? end)
    {
        if (start is null) return start;
        end ??= start;

        Node p = start;
        bool again;
        do
        {
            again = false;
            if (!p.Steiner && (Equals(p, p.Next) || Area(p.Prev, p, p.Next) == 0))
            {
                RemoveNode(p);
                p = end = p.Prev;
                if (p == p.Next) break;
                again = true;
            }
            else
            {
                p = p.Next;
            }
        } while (again || p != end);

        return end;
    }

    private static void EarcutLinked(Node? ear, List<int> triangles, double minX, double minY, double invSize, int pass)
    {
        if (ear is null) return;

        if (pass == 0 && invSize != 0) IndexCurve(ear, minX, minY, invSize);

        Node? stop = ear;
        while (ear!.Prev != ear.Next)
        {
            Node prev = ear.Prev;
            Node next = ear.Next;

            bool isEar = invSize != 0 ? IsEarHashed(ear, minX, minY, invSize) : IsEar(ear);
            if (isEar)
            {
                triangles.Add(prev.I);
                triangles.Add(ear.I);
                triangles.Add(next.I);

                RemoveNode(ear);
                ear = next.Next;
                stop = next.Next;
                continue;
            }

            ear = next;
            if (ear == stop)
            {
                // No ear found this round: try harder, then bail.
                if (pass == 0)
                    EarcutLinked(FilterPoints(ear, null), triangles, minX, minY, invSize, 1);
                else if (pass == 1)
                {
                    ear = CureLocalIntersections(FilterPoints(ear, null)!, triangles);
                    EarcutLinked(ear, triangles, minX, minY, invSize, 2);
                }
                else if (pass == 2)
                    SplitEarcut(ear, triangles, minX, minY, invSize);
                break;
            }
        }
    }

    private static bool IsEar(Node ear)
    {
        Node a = ear.Prev, b = ear, c = ear.Next;
        if (Area(a, b, c) >= 0) return false; // reflex

        Node p = ear.Next.Next;
        while (p != ear.Prev)
        {
            if (PointInTriangle(a.X, a.Y, b.X, b.Y, c.X, c.Y, p.X, p.Y) && Area(p.Prev, p, p.Next) >= 0)
                return false;
            p = p.Next;
        }
        return true;
    }

    private static bool IsEarHashed(Node ear, double minX, double minY, double invSize)
    {
        Node a = ear.Prev, b = ear, c = ear.Next;
        if (Area(a, b, c) >= 0) return false;

        double minTX = Math.Min(a.X, Math.Min(b.X, c.X));
        double minTY = Math.Min(a.Y, Math.Min(b.Y, c.Y));
        double maxTX = Math.Max(a.X, Math.Max(b.X, c.X));
        double maxTY = Math.Max(a.Y, Math.Max(b.Y, c.Y));

        double minZ = ZOrder(minTX, minTY, minX, minY, invSize);
        double maxZ = ZOrder(maxTX, maxTY, minX, minY, invSize);

        Node? p = ear.PrevZ;
        Node? n = ear.NextZ;

        while (p is not null && p.Z >= minZ && n is not null && n.Z <= maxZ)
        {
            if (p != ear.Prev && p != ear.Next &&
                PointInTriangle(a.X, a.Y, b.X, b.Y, c.X, c.Y, p.X, p.Y) && Area(p.Prev, p, p.Next) >= 0)
                return false;
            p = p.PrevZ;

            if (n != ear.Prev && n != ear.Next &&
                PointInTriangle(a.X, a.Y, b.X, b.Y, c.X, c.Y, n.X, n.Y) && Area(n.Prev, n, n.Next) >= 0)
                return false;
            n = n.NextZ;
        }

        while (p is not null && p.Z >= minZ)
        {
            if (p != ear.Prev && p != ear.Next &&
                PointInTriangle(a.X, a.Y, b.X, b.Y, c.X, c.Y, p.X, p.Y) && Area(p.Prev, p, p.Next) >= 0)
                return false;
            p = p.PrevZ;
        }

        while (n is not null && n.Z <= maxZ)
        {
            if (n != ear.Prev && n != ear.Next &&
                PointInTriangle(a.X, a.Y, b.X, b.Y, c.X, c.Y, n.X, n.Y) && Area(n.Prev, n, n.Next) >= 0)
                return false;
            n = n.NextZ;
        }

        return true;
    }

    private static Node CureLocalIntersections(Node start, List<int> triangles)
    {
        Node p = start;
        do
        {
            Node a = p.Prev, b = p.Next.Next;
            if (!Equals(a, b) && Intersects(a, p, p.Next, b) && LocallyInside(a, b) && LocallyInside(b, a))
            {
                triangles.Add(a.I);
                triangles.Add(p.I);
                triangles.Add(b.I);
                RemoveNode(p);
                RemoveNode(p.Next);
                p = start = b;
            }
            p = p.Next;
        } while (p != start);

        return FilterPoints(p, null)!;
    }

    private static void SplitEarcut(Node start, List<int> triangles, double minX, double minY, double invSize)
    {
        Node a = start;
        do
        {
            Node b = a.Next.Next;
            while (b != a.Prev)
            {
                if (a.I != b.I && IsValidDiagonal(a, b))
                {
                    Node c = SplitPolygon(a, b);
                    EarcutLinked(FilterPoints(a, a.Next), triangles, minX, minY, invSize, 0);
                    EarcutLinked(FilterPoints(c, c.Next), triangles, minX, minY, invSize, 0);
                    return;
                }
                b = b.Next;
            }
            a = a.Next;
        } while (a != start);
    }

    private static Node EliminateHoles(List<Vec2> pts, List<int> holeIndices, Node outerNode)
    {
        var queue = new List<Node>();
        for (int i = 0; i < holeIndices.Count; i++)
        {
            int start = holeIndices[i];
            int end = i < holeIndices.Count - 1 ? holeIndices[i + 1] : pts.Count;
            Node? list = BuildLinkedList(pts, start, end, clockwise: false);
            if (list is not null)
            {
                if (list == list.Next) list.Steiner = true;
                queue.Add(GetLeftmost(list));
            }
        }

        queue.Sort((a, b) => a.X.CompareTo(b.X));

        foreach (var hole in queue)
            outerNode = EliminateHole(hole, outerNode);

        return outerNode;
    }

    private static Node EliminateHole(Node hole, Node outerNode)
    {
        Node? bridge = FindHoleBridge(hole, outerNode);
        if (bridge is null) return outerNode;

        Node bridgeReverse = SplitPolygon(bridge, hole);
        FilterPoints(bridgeReverse, bridgeReverse.Next);
        return FilterPoints(bridge, bridge.Next)!;
    }

    private static Node? FindHoleBridge(Node hole, Node outerNode)
    {
        Node p = outerNode;
        double hx = hole.X, hy = hole.Y;
        double qx = double.NegativeInfinity;
        Node? m = null;

        // Find the edge whose intersection with the ray hx->+inf is closest to the hole point.
        do
        {
            if (hy <= p.Y && hy >= p.Next.Y && p.Next.Y != p.Y)
            {
                double x = p.X + ((hy - p.Y) * (p.Next.X - p.X) / (p.Next.Y - p.Y));
                if (x <= hx && x > qx)
                {
                    qx = x;
                    m = p.X < p.Next.X ? p : p.Next;
                    if (x == hx) return m; // hole touches the outer ring at a vertex
                }
            }
            p = p.Next;
        } while (p != outerNode);

        if (m is null) return null;

        // Look for a reflex outer vertex inside the triangle (hole, m, intersection) closer in angle.
        Node stop = m;
        double mx = m.X, my = m.Y;
        double tanMin = double.PositiveInfinity;
        p = m;
        do
        {
            if (hx >= p.X && p.X >= mx && hx != p.X &&
                PointInTriangle(hy < my ? hx : qx, hy, mx, my, hy < my ? qx : hx, hy, p.X, p.Y))
            {
                double tan = Math.Abs(hy - p.Y) / (hx - p.X);
                if (LocallyInside(p, hole) && (tan < tanMin || (tan == tanMin && (p.X > m.X || (p.X == m.X && SectorContainsSector(m, p))))))
                {
                    m = p;
                    tanMin = tan;
                }
            }
            p = p.Next;
        } while (p != stop);

        return m;
    }

    private static bool SectorContainsSector(Node m, Node p)
        => Area(m.Prev, m, p.Prev) < 0 && Area(p.Next, m, m.Next) < 0;

    private static void IndexCurve(Node start, double minX, double minY, double invSize)
    {
        Node p = start;
        do
        {
            if (p.Z == 0) p.Z = ZOrder(p.X, p.Y, minX, minY, invSize);
            p.PrevZ = p.Prev;
            p.NextZ = p.Next;
            p = p.Next;
        } while (p != start);

        p.PrevZ!.NextZ = null;
        p.PrevZ = null;
        SortLinked(p);
    }

    // Simon Tatham's merge sort for circular doubly-linked lists, ordering by z-order value.
    private static void SortLinked(Node? list)
    {
        int inSize = 1;
        int numMerges;
        do
        {
            Node? p = list;
            list = null;
            Node? tail = null;
            numMerges = 0;

            while (p is not null)
            {
                numMerges++;
                Node? q = p;
                int pSize = 0;
                for (int i = 0; i < inSize; i++)
                {
                    pSize++;
                    q = q!.NextZ;
                    if (q is null) break;
                }
                int qSize = inSize;

                while (pSize > 0 || (qSize > 0 && q is not null))
                {
                    Node e;
                    if (pSize != 0 && (qSize == 0 || q is null || p!.Z <= q.Z))
                    {
                        e = p!;
                        p = p.NextZ;
                        pSize--;
                    }
                    else
                    {
                        e = q!;
                        q = q.NextZ;
                        qSize--;
                    }

                    if (tail is not null) tail.NextZ = e;
                    else list = e;

                    e.PrevZ = tail;
                    tail = e;
                }
                p = q;
            }
            tail!.NextZ = null;
            inSize *= 2;
        } while (numMerges > 1);
    }

    private static double ZOrder(double x, double y, double minX, double minY, double invSize)
    {
        long lx = (long)((x - minX) * invSize);
        long ly = (long)((y - minY) * invSize);

        lx = (lx | (lx << 8)) & 0x00FF00FF;
        lx = (lx | (lx << 4)) & 0x0F0F0F0F;
        lx = (lx | (lx << 2)) & 0x33333333;
        lx = (lx | (lx << 1)) & 0x55555555;

        ly = (ly | (ly << 8)) & 0x00FF00FF;
        ly = (ly | (ly << 4)) & 0x0F0F0F0F;
        ly = (ly | (ly << 2)) & 0x33333333;
        ly = (ly | (ly << 1)) & 0x55555555;

        return lx | (ly << 1);
    }

    private static Node GetLeftmost(Node start)
    {
        Node p = start, leftmost = start;
        do
        {
            if (p.X < leftmost.X || (p.X == leftmost.X && p.Y < leftmost.Y)) leftmost = p;
            p = p.Next;
        } while (p != start);
        return leftmost;
    }

    private static bool IsValidDiagonal(Node a, Node b)
        => a.Next.I != b.I && a.Prev.I != b.I && !IntersectsPolygon(a, b)
           && ((LocallyInside(a, b) && LocallyInside(b, a) && MiddleInside(a, b)
                && (Area(a.Prev, a, b.Prev) != 0 || Area(a, b.Prev, b) != 0))
               || (Equals(a, b) && Area(a.Prev, a, a.Next) > 0 && Area(b.Prev, b, b.Next) > 0));

    private static double Area(Node p, Node q, Node r)
        => ((q.Y - p.Y) * (r.X - q.X)) - ((q.X - p.X) * (r.Y - q.Y));

    private static bool Equals(Node p1, Node p2) => p1.X == p2.X && p1.Y == p2.Y;

    private static bool Intersects(Node p1, Node q1, Node p2, Node q2)
    {
        int o1 = Sign(Area(p1, q1, p2));
        int o2 = Sign(Area(p1, q1, q2));
        int o3 = Sign(Area(p2, q2, p1));
        int o4 = Sign(Area(p2, q2, q1));

        if (o1 != o2 && o3 != o4) return true;
        if (o1 == 0 && OnSegment(p1, p2, q1)) return true;
        if (o2 == 0 && OnSegment(p1, q2, q1)) return true;
        if (o3 == 0 && OnSegment(p2, p1, q2)) return true;
        if (o4 == 0 && OnSegment(p2, q1, q2)) return true;
        return false;
    }

    private static bool OnSegment(Node p, Node q, Node r)
        => q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X)
           && q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y);

    private static int Sign(double num) => num > 0 ? 1 : num < 0 ? -1 : 0;

    private static bool IntersectsPolygon(Node a, Node b)
    {
        Node p = a;
        do
        {
            if (p.I != a.I && p.Next.I != a.I && p.I != b.I && p.Next.I != b.I && Intersects(p, p.Next, a, b))
                return true;
            p = p.Next;
        } while (p != a);
        return false;
    }

    private static bool LocallyInside(Node a, Node b)
        => Area(a.Prev, a, a.Next) < 0
            ? Area(a, b, a.Next) >= 0 && Area(a, a.Prev, b) >= 0
            : Area(a, b, a.Prev) < 0 || Area(a, a.Next, b) < 0;

    private static bool MiddleInside(Node a, Node b)
    {
        Node p = a;
        bool inside = false;
        double px = (a.X + b.X) / 2, py = (a.Y + b.Y) / 2;
        do
        {
            if (((p.Y > py) != (p.Next.Y > py)) && p.Next.Y != p.Y &&
                (px < ((p.Next.X - p.X) * (py - p.Y) / (p.Next.Y - p.Y)) + p.X))
                inside = !inside;
            p = p.Next;
        } while (p != a);
        return inside;
    }

    private static bool PointInTriangle(double ax, double ay, double bx, double by, double cx, double cy, double px, double py)
        => (cx - px) * (ay - py) - (ax - px) * (cy - py) >= 0
           && (ax - px) * (by - py) - (bx - px) * (ay - py) >= 0
           && (bx - px) * (cy - py) - (cx - px) * (by - py) >= 0;

    // Splits a polygon into two, returning the new node belonging to the second loop.
    private static Node SplitPolygon(Node a, Node b)
    {
        var a2 = new Node(a.I, a.X, a.Y);
        var b2 = new Node(b.I, b.X, b.Y);
        Node an = a.Next, bp = b.Prev;

        a.Next = b;
        b.Prev = a;
        a2.Next = an;
        an.Prev = a2;
        b2.Next = a2;
        a2.Prev = b2;
        bp.Next = b2;
        b2.Prev = bp;

        return b2;
    }

    private static Node InsertNode(int i, Vec2 pt, Node? last)
    {
        var p = new Node(i, pt.X, pt.Y);
        if (last is null)
        {
            p.Prev = p;
            p.Next = p;
        }
        else
        {
            p.Next = last.Next;
            p.Prev = last;
            last.Next.Prev = p;
            last.Next = p;
        }
        return p;
    }

    private static void RemoveNode(Node p)
    {
        p.Next.Prev = p.Prev;
        p.Prev.Next = p.Next;
        if (p.PrevZ is not null) p.PrevZ.NextZ = p.NextZ;
        if (p.NextZ is not null) p.NextZ.PrevZ = p.PrevZ;
    }

    private static double SignedArea(List<Vec2> pts, int start, int end)
    {
        double sum = 0;
        for (int i = start, j = end - 1; i < end; j = i++)
            sum += (pts[j].X - pts[i].X) * (pts[i].Y + pts[j].Y);
        return sum;
    }
}
