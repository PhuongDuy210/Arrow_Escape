using System.Collections;
using System.Linq;
using UnityEngine;

public class MovementManager : MonoBehaviour
{
    public bool IsMoving { get; set; }

    public IEnumerator MoveArrowSteps(Arrow arrow, ArrowRenderer renderer, Grid grid, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            IsMoving = true;
            yield return renderer.MoveOneStep(); // purely visual
            UpdateArrowData(arrow, grid);        // logical update
        }

        IsMoving = false;
    }

    public IEnumerator MoveArrowUntilOffscreen(Arrow arrow, ArrowRenderer renderer, Grid grid)
    {
        //while (IsArrowInsideGrid(arrow, grid) || !IsOffScreen(renderer.TailPosition()))
        while (!IsOffScreen(renderer.TailPosition()))
        {
            IsMoving = true;
            yield return renderer.MoveOneStep();
            UpdateArrowData(arrow, grid);
        }

        // Arrow fully exited
        renderer.ClearArrow();
        IsMoving = false;
    }

    private void UpdateArrowData(Arrow arrow, Grid grid)
    {
        Node oldTail = arrow.Path.First();
        // Only update if the node is in the grid
        if (grid.IsValidNode(oldTail.X, oldTail.Y))
        { 
            grid.Nodes[(oldTail.X, oldTail.Y)].IsClaimed = false;
            grid.Nodes[(oldTail.X, oldTail.Y)].Owner = null;
        }

        Node newTip = CalculateNextTip(arrow);
        arrow.Path.Add(newTip);

        // Only update if the node is in the grid
        if (grid.IsValidNode(newTip.X, newTip.Y))
        {
            grid.Nodes[(newTip.X, newTip.Y)].IsClaimed = true;
        }
        if (grid.IsValidNode(oldTail.X, oldTail.Y))
        {
            grid.Nodes[(oldTail.X, oldTail.Y)].Owner = arrow;
        }

        arrow.Path.RemoveAt(0); // consume tail
    }

    private bool IsArrowInsideGrid(Arrow arrow, Grid grid)
    {
        Node tail = arrow.Path.First();
        return grid.IsValidNode(tail.X, tail.Y);
    }

    private bool IsOffScreen(Vector3 worldPos)
    {
        Camera cam = Camera.main;
        Vector3 viewportPos = cam.WorldToViewportPoint(worldPos);
        return viewportPos.x < 0 || viewportPos.x > 1 ||
               viewportPos.y < 0 || viewportPos.y > 1;
    }

    private Node CalculateNextTip(Arrow arrow)
    {
        Node tip = arrow.Path.Last();
        var (dx, dy) = Helper.ToVector(tip.Dir);
        return new Node(tip.X + dx, tip.Y + dy, tip.Dir);
    }
}
