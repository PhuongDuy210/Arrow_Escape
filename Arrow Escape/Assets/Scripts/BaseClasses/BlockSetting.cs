using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class BlockSetting
{
    public int id;
    public List<Cell> cells;
    public List<string> escapeDirections;
    public int maxLength;

    public bool IsInBlock( int x, int y)
    {
        if (cells == null) return false;

        // Check if any cell in the block matches the coordinate
        return cells.Any(c => c.x == x && c.y == y);
    }

}