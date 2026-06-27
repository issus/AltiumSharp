namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>
/// A disjoint-set (union-find) structure with path compression and union by rank. Elements are dense
/// integer ids in <c>[0, count)</c>; the solver assigns one id per connectable element.
/// </summary>
internal sealed class UnionFind
{
    private readonly int[] _parent;
    private readonly int[] _rank;

    public UnionFind(int count)
    {
        _parent = new int[count];
        _rank = new int[count];
        for (var i = 0; i < count; i++)
            _parent[i] = i;
    }

    /// <summary>The number of elements.</summary>
    public int Count => _parent.Length;

    /// <summary>Returns the representative (root) of <paramref name="x"/>'s set, compressing the path.</summary>
    public int Find(int x)
    {
        var root = x;
        while (_parent[root] != root)
            root = _parent[root];
        // Path compression.
        while (_parent[x] != root)
        {
            var next = _parent[x];
            _parent[x] = root;
            x = next;
        }
        return root;
    }

    /// <summary>Merges the sets containing <paramref name="a"/> and <paramref name="b"/>.</summary>
    public void Union(int a, int b)
    {
        var ra = Find(a);
        var rb = Find(b);
        if (ra == rb)
            return;
        if (_rank[ra] < _rank[rb])
            (ra, rb) = (rb, ra);
        _parent[rb] = ra;
        if (_rank[ra] == _rank[rb])
            _rank[ra]++;
    }

    /// <summary>Whether two elements are in the same set.</summary>
    public bool Connected(int a, int b) => Find(a) == Find(b);
}
