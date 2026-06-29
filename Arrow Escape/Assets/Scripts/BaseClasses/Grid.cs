using System;
using System.Collections.Generic;

public class Grid
{
    public int Width { get; }
    public int Height { get; }
    public Dictionary<(int x, int y), Node> Nodes { get; }
    public List<Node> AllNodes { get; private set; }

    public Grid(PuzzleSetting settings)
    {
        Width = settings.gridSize[0];
        Height = settings.gridSize[1];
        Nodes = new Dictionary<(int x, int y), Node>();
        AllNodes = new List<Node>();

        // Build nodes only for coordinates defined in blocks
        foreach (var block in settings.blocks)
        {
            foreach (var cell in block.cells)
            {
                int x = cell.x;
                int y = cell.y;

                var node = new Node(x, y);
                Nodes[(x, y)] = node;
                AllNodes.Add(node);
            }
        }
    }

    public bool IsValidNode(int x, int y)
    {
        return Nodes.ContainsKey((x, y));
    }

    public Node GetNode(int x, int y)
    {
        Node node;
        return Nodes.TryGetValue((x, y), out node) ? node : null;
    }

    public bool HasUnclaimedNodes()
    {
        foreach (var node in Nodes.Values)
        {
            if (!node.IsClaimed)
                return true;
        }
        return false;
    }

    public void ResetClaims()
    {
        foreach (var node in Nodes.Values)
        {
            node.IsClaimed = false;
            node.Owner = null;
        }
    }

    public bool HasUnclaimedNodes(BlockSetting block)
    {
        foreach (var cell in block.cells)
        {
            var node = GetNode(cell.x, cell.y);
            if (!node.IsClaimed)
                return true;
        }
        return false;
    }

    public void ResetClaims(BlockSetting block)
    {
        foreach (var cell in block.cells)
        {
            var node = GetNode(cell.x, cell.y);
            node.IsClaimed = false;
            node.Owner = null;
        }
    }
}
