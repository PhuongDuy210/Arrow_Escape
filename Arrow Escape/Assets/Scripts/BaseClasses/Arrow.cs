using System.Collections.Generic;
using System.Linq;

public class Arrow
{
    public List<Node> Path { get; }       // nodes claimed by this arrow
    public HashSet<Arrow> Dependencies { get; }   // arrows blocking this one

    public Arrow()
    {
        Path = new List<Node>();
        Dependencies = new HashSet<Arrow>();
    }

    public Direction CurrentDirection => Path.LastOrDefault()?.Dir ?? Direction.None;
}