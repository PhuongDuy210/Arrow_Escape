using UnityEngine;

public class Helper
{
    public static (int dx, int dy) ToVector(Direction dir)
    {
        return dir switch
        {
            Direction.Up => (0, 1),
            Direction.Down => (0, -1),
            Direction.Left => (-1, 0),
            Direction.Right => (1, 0),
            _ => (0, 0)
        };
    }

    public static Direction Opposite(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => Direction.None
        };
    }

    public static Direction ToDirection(int dx, int dy)
    {
        if (dx == 1 && dy == 0) return Direction.Right;
        if (dx == -1 && dy == 0) return Direction.Left;
        if (dx == 0 && dy == 1) return Direction.Up;
        if (dx == 0 && dy == -1) return Direction.Down;

        return Direction.None; // fallback if not a valid unit step
    }
}
