using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PuzzleGenerator : MonoBehaviour
{
    private Grid grid;
    private List<Arrow> arrows;
    private System.Random rng;

    private PuzzleSetting settings;
    private int minLength = 1;

    [SerializeField]
    private GameObject myArrowPrefab;

    private HashSet<Node> failedStarts;

    private static readonly Direction[] allDirections = (Direction[])Enum.GetValues(typeof(Direction));

    public List<Arrow> GeneratePuzzle(Grid grid, PuzzleSetting settings)
    {
        rng = new System.Random();

        this.settings = settings;

        this.grid = grid;

        // Generate puzzle
        List<Arrow> allArrows = new List<Arrow>();
        //allArrows = Generate();
        foreach (BlockSetting block in settings.blocks)
        {
            arrows = GenerateBlockPuzzle(block);
            allArrows.AddRange(arrows);
        }

        // Center the grid
        var tileSize = .2f;
        float gridWidth = grid.Width * tileSize;
        float gridHeight = grid.Height * tileSize;

        float offsetX = (grid.Width % 2 == 0) ? gridWidth * 0.5f - tileSize * 0.5f : gridWidth * 0.5f;
        float offsetY = (grid.Height % 2 == 0) ? gridHeight * 0.5f - tileSize * 0.5f : gridHeight * 0.5f;

        Vector3 gridOffset = new Vector3(offsetX, offsetY, 0f);

        GameObject gridParent = new GameObject("Grids");
        foreach (var arrow in allArrows)
        {
            GameObject parent = new GameObject("Arrow");
            GameObject go = Instantiate(myArrowPrefab, parent.transform);
            var renderer = go.GetComponent<ArrowRenderer>();
            renderer.tileSize = tileSize; // match your grid size
            renderer.RenderArrow(arrow, gridOffset);
            parent.transform.SetParent(gridParent.transform);
        }

        return allArrows;
    }

    //private List<Arrow> Generate()
    //{
    //    const int maxAttempts = 10000;
    //    int attempts = 0;

    //    int unsolvableRetry = 0;
    //    int repairFailedRetry = 0;
    //    int startNodeRetry = 0;

    //    while (attempts < maxAttempts)
    //    {
    //        attempts++;
    //        var arrowList = new List<Arrow>();
    //        failedStarts = new HashSet<Node>();

    //        // Build arrows until all nodes are claimed
    //        while (grid.HasUnclaimedNodes())
    //        {
    //            // Get a "safe" node, if doesn't exist get a random node that is not in failedNode list
    //            Node start = GetSafeStartNode() ?? GetRandomUnclaimedNode(rng);
    //            if (start == null)
    //            {
    //                // No viable start left → scrap this grid attempt
    //                attempts++;
    //                arrowList.Clear();
    //                grid.ResetClaims();
    //                failedStarts.Clear();

    //                startNodeRetry++;
    //                continue;
    //            }

    //            BlockSetting currentBlock = null;
    //            foreach (BlockSetting block in settings.blocks)
    //            {
    //                if (block.cells.Any(c => c.x == start.X && c.y == start.Y))
    //                {
    //                    currentBlock = block;
    //                    break;
    //                }
    //            }
    //            Arrow arrow = null;

    //            // Local retry loop for this node/block
    //            int retries = 0;
    //            while (arrow == null && retries < 20)
    //            {
    //                arrow = TryPlaceArrow(start, currentBlock);
    //                retries++;
    //            }
    //            if (arrow != null)
    //            {
    //                arrowList.Add(arrow);
    //                foreach (var node in arrow.Path)
    //                {
    //                    node.IsClaimed = true;
    //                    node.Owner = arrow;
    //                }
    //            }
    //            else
    //            {
    //                failedStarts.Add(start);
    //            }
    //        }

    //        // Repair step
    //        if (!RepairIsolatedArrows(arrowList, grid))
    //        {
    //            arrowList.Clear();
    //            grid.ResetClaims();
    //            repairFailedRetry++;
    //            continue; // retry
    //        }

    //        // Build dependencies
    //        BuildDependencies(arrowList);

    //        // Edge case for when arrows pointing at each other
    //        DetectTipCycles(arrowList);

    //        // Topological sort
    //        if (!TopologicalSort(arrowList))
    //        {
    //            arrowList.Clear();
    //            grid.ResetClaims();
    //            unsolvableRetry++;
    //            continue; // retry
    //        }

    //        // Success
    //        Debug.Log($"Success after {attempts} attemps with {arrowList.Count} arrows");
    //        Debug.Log($"{unsolvableRetry} failed attemps due to unsolvable");
    //        Debug.Log($"{repairFailedRetry} failed attemps due to repair failed");
    //        Debug.Log($"{startNodeRetry} failed attemps due to starting node failed");
    //        return arrowList;
    //    }
    //    Debug.Log($"{unsolvableRetry} failed attemps due to unsolvable");
    //    Debug.Log($"{repairFailedRetry} failed attemps due to repair failed");
    //    Debug.Log($"{startNodeRetry} failed attemps due to starting node failed");

    //    Debug.LogError("Generation failed after max attempts");
    //    return new List<Arrow>();
    //}

    private List<Arrow> GenerateBlockPuzzle(BlockSetting block)
    {
        const int maxAttempts = 20000;
        int attempts = 0;

        int unsolvableRetry = 0;
        int repairFailedRetry = 0;
        int startNodeRetry = 0;

        while (attempts < maxAttempts)
        {
            attempts++;
            var arrowList = new List<Arrow>();
            failedStarts = new HashSet<Node>();

            // Only consider nodes inside this block
            while (grid.HasUnclaimedNodes(block))
            {
                Node start = GetSafeStartNode(block) ?? GetRandomUnclaimedNode(block);
                if (start == null) break;

                Arrow arrow = null;
                int retries = 0;
                while (arrow == null && retries < 20)
                {
                    arrow = TryPlaceArrow(start, block);
                    retries++;
                }

                if (arrow != null)
                {
                    arrowList.Add(arrow);
                    foreach (var node in arrow.Path)
                    {
                        node.IsClaimed = true;
                        node.Owner = arrow;
                    }
                }
                else
                {
                    failedStarts.Add(start);
                }
            }
            
            // Transform unclaimed node into isolated arrows (1-node arrow)
            CleanupUnclaimedNodes(arrowList, block);

            // Try to repair isolated arrows
            if (!RepairIsolatedArrows(arrowList, block))
            {
                arrowList.Clear();
                grid.ResetClaims();
                repairFailedRetry++;
                continue; // retry
            }

            BuildDependencies(arrowList);

            // Edge case for when arrows pointing at each other
            if (!DetectIndirectMutualBlocks(arrowList))
            {
                arrowList.Clear();
                grid.ResetClaims();
                unsolvableRetry++;
                continue; // retry
            }

            if (!TopologicalSort(arrowList))
            {
                arrowList.Clear();
                grid.ResetClaims();
                unsolvableRetry++;
                continue; // retry
            }

            // Success
            Debug.Log($"Success after {attempts} attemps with {arrowList.Count} arrows");
            Debug.Log($"{unsolvableRetry} failed attemps due to unsolvable");
            Debug.Log($"{repairFailedRetry} failed attemps due to repair failed");
            Debug.Log($"{startNodeRetry} failed attemps due to starting node failed");
            return arrowList; // success
        }

        Debug.Log($"{unsolvableRetry} failed attemps due to unsolvable");
        Debug.Log($"{repairFailedRetry} failed attemps due to repair failed");
        Debug.Log($"{startNodeRetry} failed attemps due to starting node failed");

        Debug.LogError($"Block puzzle generation failed after {maxAttempts} attempts");
        return new List<Arrow>();
    }

    private Node GetSafeStartNode()
    {
        foreach (var node in grid.AllNodes)
        {
            if (!node.IsClaimed && IsSafeToClaim(node) && !failedStarts.Contains(node))
                return node;
        }
        return null;
    }

    private Node GetRandomUnclaimedNode(System.Random rng)
    {
        // Reuse a buffer list to avoid repeated allocations
        List<Node> unclaimed = new List<Node>();

        foreach (var n in grid.AllNodes)
        {
            if (!n.IsClaimed && !failedStarts.Contains(n))
                unclaimed.Add(n);
        }

        if (unclaimed.Count == 0)
            return null;

        return unclaimed[rng.Next(unclaimed.Count)];
    }

    private Node GetSafeStartNode(BlockSetting block)
    {
        foreach (var cell in block.cells)
        {
            var node = grid.GetNode(cell.x, cell.y);
            if (!node.IsClaimed && IsSafeToClaim(node) && !failedStarts.Contains(node))
                return node;
        }
        return null;
    }

    private Node GetRandomUnclaimedNode(BlockSetting block)
    {
        // Reuse a buffer list to avoid repeated allocations
        List<Node> unclaimed = new List<Node>();

        foreach (var cell in block.cells)
        {
            var n = grid.GetNode(cell.x, cell.y);
            if (!n.IsClaimed && !failedStarts.Contains(n))
                unclaimed.Add(n);
        }

        if (unclaimed.Count == 0)
            return null;

        return unclaimed[rng.Next(unclaimed.Count)];
    }

    private Arrow TryPlaceArrow(Node start, BlockSetting block)
    {
        Arrow arrow = new Arrow();
        Node current = start;
        arrow.Path.Add(current);

        Direction lastDir = Direction.None;

        while (arrow.Path.Count < block.maxLength)
        {
            List<Direction> available = new List<Direction>();
            foreach (Direction dir in allDirections)
            {
                if (dir == Direction.None || dir == Helper.Opposite(lastDir))
                    continue;

                var (dx, dy) = Helper.ToVector(dir);
                Node next = grid.GetNode(current.X + dx, current.Y + dy);
                if (next != null && !next.IsClaimed && 
                    !arrow.Path.Contains(next) && 
                    IsSafeToClaim(next) &&
                    block.cells.Any(c => c.x == next.X && c.y == next.Y)
                    )
                {
                    available.Add(dir);
                }
            }

            if (available.Count == 0) break;

            Direction chosen = available[rng.Next(available.Count)];
            var (cx, cy) = Helper.ToVector(chosen);
            Node nextNode = grid.GetNode(current.X + cx, current.Y + cy);

            if (nextNode != null && !nextNode.IsClaimed && !arrow.Path.Contains(nextNode) && IsSafeToClaim(nextNode))
            {
                current.Dir = chosen;
                arrow.Path.Add(nextNode);
                current = nextNode;
                lastDir = chosen;
            }
        }

        if (arrow.Path.Count >= minLength)
        {
            Node tip = arrow.Path.Last();

            if (arrow.Path.Count >= 2)
            {
                Node beforeTip = arrow.Path[arrow.Path.Count - 2];

                int dx = tip.X - beforeTip.X;
                int dy = tip.Y - beforeTip.Y;

                Direction tipDir = Helper.ToDirection(dx, dy);
                tip.Dir = tipDir;
            }
            else
            {
                tip.Dir = lastDir;
            }

            if (!block.escapeDirections.Contains(tip.Dir.ToString().ToLower()))
            {
                // Allow 1 node long arrow to go through since it might be fixed later anyway
                if (arrow.Path.Count == 1)
                {
                    return arrow;
                }

                arrow.Path.Clear();
                return null; // scrap arrow, retry only this arrow
            }

            foreach (var node in arrow.Path)
                node.Owner = arrow;
            return arrow;
        }

        if (arrow.Path.Count == 1)
        {
            return arrow;
        }

        arrow.Path.Clear();
        return null;
    }

    private bool RepairIsolatedArrows(List<Arrow> arrows, Grid grid)
    {
        var oneNodeArrows = arrows.Where(a => a.Path.Count == 1).ToList();

        foreach (var isolated in oneNodeArrows)
        {
            Node isoNode = isolated.Path[0];

            var neighbors = GetNeighbors(isoNode, grid)
                .Where(n => n.Owner != null)
                .Select(n => n.Owner)
                .Distinct()
                .ToList();

            bool merged = false;

            foreach (var neighbor in neighbors)
            {
                Node tail = neighbor.Path.First();
                Node head = neighbor.Path.Last();

                if (IsAdjacent(isoNode, tail))
                {
                    // Merge as new tail
                    neighbor.Path.Insert(0, isoNode);
                    isoNode.Owner = neighbor;
                    foreach (var node in neighbor.Path) node.Owner = neighbor;
                    arrows.Remove(isolated);
                    merged = true;
                    break;
                }
                else if (IsAdjacent(isoNode, head))
                {
                    // Tentative merge as new head
                    neighbor.Path.Add(isoNode);
                    isoNode.Owner = neighbor;

                    Node beforeHead = neighbor.Path[neighbor.Path.Count - 2];
                    Node newHead = neighbor.Path.Last();

                    int dx = newHead.X - beforeHead.X;
                    int dy = newHead.Y - beforeHead.Y;
                    Direction newDir = Helper.ToDirection(dx, dy);

                    BlockSetting currentBlock = null;
                    foreach (BlockSetting block in settings.blocks)
                    {
                        if (block.cells.Any(c => c.x == newHead.X && c.y == newHead.Y))
                        {
                            currentBlock = block;
                            break;
                        }
                    }
                    if (currentBlock.escapeDirections.Contains(newDir.ToString().ToLower()))
                    {
                        // Valid merge → accept
                        newHead.Dir = newDir;
                        foreach (var node in neighbor.Path) node.Owner = neighbor;
                        arrows.Remove(isolated);
                        merged = true;
                        break;
                    }
                    else
                    {
                        // Invalid merge → undo and try next neighbor
                        neighbor.Path.Remove(newHead);
                        isoNode.Owner = null;
                    }
                }
            }

            if (!merged)
            {
                // Could not merge → trigger regeneration
                return false;
            }
        }

        return true;
    }

    private bool RepairIsolatedArrows(List<Arrow> arrows, BlockSetting currentBlock)
    {
        var oneNodeArrows = arrows.Where(a => a.Path.Count == 1).ToList();

        foreach (var isolated in oneNodeArrows)
        {
            Node isoNode = isolated.Path[0];

            var neighbors = GetNeighbors(isoNode, currentBlock)
                .Where(n => n.Owner != null)
                .Select(n => n.Owner)
                .Distinct()
                .ToList();

            bool merged = false;

            foreach (var neighbor in neighbors)
            {
                Node tail = neighbor.Path.First();
                Node head = neighbor.Path.Last();

                if (IsAdjacent(isoNode, tail))
                {
                    // Merge as new tail
                    neighbor.Path.Insert(0, isoNode);
                    isoNode.Owner = neighbor;
                    foreach (var node in neighbor.Path) node.Owner = neighbor;
                    arrows.Remove(isolated);
                    merged = true;
                    break;
                }
                else if (IsAdjacent(isoNode, head))
                {
                    // Tentative merge as new head
                    neighbor.Path.Add(isoNode);
                    isoNode.Owner = neighbor;

                    Node beforeHead = neighbor.Path[neighbor.Path.Count - 2];
                    Node newHead = neighbor.Path.Last();

                    int dx = newHead.X - beforeHead.X;
                    int dy = newHead.Y - beforeHead.Y;
                    Direction newDir = Helper.ToDirection(dx, dy);

                    if (currentBlock.escapeDirections.Contains(newDir.ToString().ToLower()))
                    {
                        // Valid merge → accept
                        newHead.Dir = newDir;
                        foreach (var node in neighbor.Path) node.Owner = neighbor;
                        arrows.Remove(isolated);
                        merged = true;
                        break;
                    }
                    else
                    {
                        // Invalid merge → undo and try next neighbor
                        neighbor.Path.Remove(newHead);
                        isoNode.Owner = null;
                    }
                }
            }

            if (!merged)
            {
                // Could not merge → trigger regeneration
                return false;
            }
        }

        return true;
    }
    private bool IsAdjacent(Node a, Node b)
    {
        return (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y)) == 1;
    }

    bool HasFreeNeighbor(Node node)
    {
        var neighbors = GetNeighbors(node, grid);
        foreach (var neighbor in neighbors)
        {
            if (neighbor != null && !neighbor.IsClaimed)
                return true;
        }
        return false;
    }

    private bool IsSafeToClaim(Node candidate)
    {
        // Candidate itself must have at least one free neighbor
        if (!HasFreeNeighbor(candidate)) return false;

        var neighbors = GetNeighbors(candidate, grid);
        foreach (var neighbor in neighbors)
        {
            if (neighbor != null && !neighbor.IsClaimed)
            {
                int freeCount = CountFreeNeighbors(neighbor);

                if (freeCount == 0)
                {
                    // Truly isolated → bad
                    return false;
                }
                else if (freeCount == 1)
                {
                    // Check if the only free neighbor is the candidate itself
                    bool onlyCandidate = true;
                    foreach (var nn in GetNeighbors(neighbor, grid))
                    {
                        if (nn != null && !nn.IsClaimed && nn != candidate)
                        {
                            onlyCandidate = false;
                            break;
                        }
                    }

                    if (!onlyCandidate)
                    {
                        // Neighbor would be isolated from everyone except candidate → bad
                        return false;
                    }
                    // If onlyCandidate == true, that’s fine → they can form a 2‑node arrow
                }
            }
        }
        return true;
    }

    public int CountFreeNeighbors(Node node)
    {
        int count = 0;
        var neighbors = GetNeighbors(node, grid);
        foreach (var neighbor in neighbors)
        {
            if (neighbor != null && !neighbor.IsClaimed)
            {
                count++;
            }
        }

        return count;
    }

    private IEnumerable<Node> GetNeighbors(Node node, Grid grid)
    {
        var dirs = new (int dx, int dy)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
        foreach (var (dx, dy) in dirs)
        {
            Node n = grid.GetNode(node.X + dx, node.Y + dy);
            if (n != null) yield return n;
        }
    }

    private IEnumerable<Node> GetNeighbors(Node node, BlockSetting block)
    {
        var dirs = new (int dx, int dy)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
        foreach (var (dx, dy) in dirs)
        {
            Node n = grid.GetNode(node.X + dx, node.Y + dy);
            if (n != null && block.IsInBlock(n.X, n.Y)) yield return n;
        }
    }

    private bool NoAvailableMoves(Node node)
    {
        foreach (Direction dir in Enum.GetValues(typeof(Direction)))
        {
            if (dir == Direction.None) 
                continue;
            var (dx, dy) = Helper.ToVector(dir);
            Node next = grid.GetNode(node.X + dx, node.Y + dy);
            if (next != null && !next.IsClaimed)
                return false;
        }
        return true;
    }

    private bool TopologicalSort(List<Arrow> arrows)
    {
        // Build indegree and adjacency list
        Dictionary<Arrow, int> indegree = arrows.ToDictionary(a => a, a => 0);
        Dictionary<Arrow, List<Arrow>> adj = arrows.ToDictionary(a => a, a => new List<Arrow>());

        foreach (var arrow in arrows)
        {
            foreach (var dep in arrow.Dependencies)
            {
                if (!indegree.ContainsKey(dep)) continue;

                // dep must be resolved before arrow
                indegree[arrow]++;
                adj[dep].Add(arrow);
            }
        }

        Queue<Arrow> q = new Queue<Arrow>(indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        int count = 0;

        while (q.Count > 0)
        {
            Arrow current = q.Dequeue();
            count++;

            foreach (var neighbor in adj[current])
            {
                indegree[neighbor]--;
                if (indegree[neighbor] == 0)
                    q.Enqueue(neighbor);
            }
        }

        return count == arrows.Count;
    }

    private void BuildDependencies(List<Arrow> arrows)
    {
        foreach (var arrow in arrows)
        {
            arrow.Dependencies.Clear();
            HashSet<Arrow> seen = new HashSet<Arrow>();

            Node tip = arrow.Path.Last();
            var (dx, dy) = Helper.ToVector(tip.Dir);

            int x = tip.X + dx;
            int y = tip.Y + dy;

            while (true)
            {
                Node target = grid.GetNode(x, y);
                if (target == null) break;

                if (target.Owner != null && target.Owner != arrow)
                {
                    Arrow other = target.Owner;
                    if (!seen.Contains(other))
                    {
                        arrow.Dependencies.Add(other);
                        seen.Add(other);

                        Node otherTip = other.Path.Last();
                        if (target == otherTip)
                        {
                            // mutual block
                            other.Dependencies.Add(arrow);
                        }
                    }
                }

                if (target.Owner == arrow && target != tip)
                {
                    // self‑cycle only if tip truly points into its own body
                    arrow.Dependencies.Add(arrow);
                }

                x += dx;
                y += dy;
            }
        }
    }

    private bool DetectTipCycles(List<Arrow> arrows)
    {
        foreach (var arrow in arrows)
        {
            Node tip = arrow.Path.Last();
            var (dx, dy) = Helper.ToVector(tip.Dir);

            int x = tip.X + dx;
            int y = tip.Y + dy;

            while (true)
            {
                Node target = grid.GetNode(x, y);
                if (target == null) break; // out of bounds

                if (target.Owner != null && target.Owner != arrow)
                {
                    Arrow other = target.Owner;
                    Node otherTip = other.Path.Last();

                    if (target == otherTip)
                    {
                        // Check if they are facing each other
                        if (tip.Dir == Helper.Opposite(otherTip.Dir))
                        {
                            // Two tips directly opposing each other → unsolvable
                            return false;
                        }
                    }

                    // If it's a body, keep scanning forward
                }

                x += dx;
                y += dy;
            }
        }

        return true; // no tip-to-tip conflict found
    }

    private bool DetectIndirectMutualBlocks(List<Arrow> arrows)
    {
        foreach (var arrow in arrows)
        {
            Node tip = arrow.Path.Last();
            var (dx, dy) = Helper.ToVector(tip.Dir);

            int x = tip.X + dx;
            int y = tip.Y + dy;

            while (true)
            {
                Node target = grid.GetNode(x, y);
                if (target == null) break; // out of bounds

                if (target.Owner != null && target.Owner != arrow)
                {
                    Arrow other = target.Owner;

                    // Case 1: tip-to-tip (already handled elsewhere)
                    Node otherTip = other.Path.Last();
                    if (target == otherTip && tip.Dir == Helper.Opposite(otherTip.Dir))
                    {
                        return false; // unsolvable
                    }

                    // Case 2: tip points into another arrow’s body,
                    // and that arrow’s tip points back into this arrow’s body
                    if (target != otherTip)
                    {
                        Node otherTipNode = other.Path.Last();
                        var (odx, ody) = Helper.ToVector(otherTipNode.Dir);

                        int ox = otherTipNode.X + odx;
                        int oy = otherTipNode.Y + ody;

                        // Walk forward from other’s tip
                        while (true)
                        {
                            Node t2 = grid.GetNode(ox, oy);
                            if (t2 == null) break;

                            if (t2.Owner == arrow)
                            {
                                // Other’s tip ray eventually hits arrow’s body
                                // → mutual block
                                return false;
                            }

                            ox += odx;
                            oy += ody;
                        }
                    }

                    //break; // stop after first arrow encountered
                }

                x += dx;
                y += dy;
            }
        }

        return true; // no indirect mutual blocks found
    }

    private void CleanupUnclaimedNodes(List<Arrow> arrows, BlockSetting block)
    {
        foreach (var cell in block.cells)
        {
            Node node = grid.GetNode(cell.x, cell.y);
            if (node != null && !node.IsClaimed)
            {
                // Create a 1-node arrow for this leftover node
                Arrow arrow = new Arrow();
                arrow.Path.Add(node);

                // Assign a default direction (optional: pick one that matches escapeDirections)
                if (block.escapeDirections.Count > 0)
                {
                    string dirStr = block.escapeDirections[0];
                    Direction dir = (Direction)Enum.Parse(typeof(Direction), dirStr, true);
                    node.Dir = dir;
                }
                else
                {
                    node.Dir = Direction.None;
                }

                node.Owner = arrow;
                node.IsClaimed = true;

                arrows.Add(arrow);
            }
        }
    }

}