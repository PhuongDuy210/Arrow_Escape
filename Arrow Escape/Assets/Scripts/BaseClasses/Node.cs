public class Node
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsClaimed { get; set; }   // whether this node belongs to an Arrow
    public Arrow Owner { get; set; }      // reference to the Arrow that owns it

    public Direction Dir { get; set; } = Direction.None;

    public Node(int x, int y)
    {
        X = x;
        Y = y;
        IsClaimed = false;
        Owner = null;
    }

    public Node(int x, int y, Direction dir)
    {
        X = x;
        Y = y;
        IsClaimed = false;
        Owner = null;
        Dir = dir;
    }
}