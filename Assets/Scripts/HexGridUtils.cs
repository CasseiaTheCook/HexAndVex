using UnityEngine;

public static class HexGridUtils
{
    public static readonly Vector3Int[] OddOffsets = {
        new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0),
        new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0)
    };

    public static readonly Vector3Int[] EvenOffsets = {
        new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0),
        new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0)
    };

    public static Vector3Int[] GetOffsets(Vector3Int cell)
    {
        return (cell.y % 2 != 0) ? EvenOffsets : OddOffsets;
    }

    public static bool IsNeighbor(Vector3Int cell1, Vector3Int cell2)
    {
        Vector3Int[] offsets = GetOffsets(cell1);
        foreach (var off in offsets)
            if (cell1 + off == cell2) return true;
        return false;
    }

    public static Vector3Int OffsetToCube(Vector3Int o)
    {
        int x = o.x - (o.y - (o.y & 1)) / 2;
        int z = o.y;
        int y = -x - z;
        return new Vector3Int(x, y, z);
    }

    public static float DistanceCube(Vector3Int a, Vector3Int b)
    {
        Vector3Int ac = OffsetToCube(a);
        Vector3Int bc = OffsetToCube(b);
        return (Mathf.Abs(ac.x - bc.x) + Mathf.Abs(ac.y - bc.y) + Mathf.Abs(ac.z - bc.z)) * 0.5f;
    }

    public static Vector3Int GetRawOppositeCell(Vector3Int centerCell, Vector3Int awayFromCell)
    {
        Vector3Int[] offsets = GetOffsets(centerCell);
        for (int i = 0; i < 6; i++)
            if (centerCell + offsets[i] == awayFromCell)
                return centerCell + offsets[(i + 3) % 6];
        return centerCell;
    }
}
