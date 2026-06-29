using System.Collections.Generic;
using UnityEngine;

public class PuzzleGeneratorTest : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 5;
    public int height = 5;
    public int minLength = 2;
    public int maxLength = 4;

    [Tooltip("List of valid node coordinates (x,y) for the puzzle shape")]
    public List<Vector2Int> maskCoordinates;

    //void Start()
    //{
    //    // Convert maskCoordinates into HashSet<(int,int)>
    //    var mask = new HashSet<(int, int)>();
    //    foreach (var coord in maskCoordinates)
    //    {
    //        mask.Add((coord.x, coord.y));
    //    }

    //    // Create grid and generator
    //    var grid = new Grid(width, height, mask);
    //    var generator = new PuzzleGenerator(grid, minLength, maxLength);

    //    // Generate puzzle
    //    var arrows = generator.Generate();

    //    // Print results to Unity Console
    //    Debug.Log("Generated Puzzle:");
    //    Debug.Log("Arrows:" + arrows.Count);
    //    int index = 1;
    //    foreach (var arrow in arrows)
    //    {
    //        string pathStr = "";
    //        foreach (var node in arrow.Path)
    //        {
    //            pathStr += $"({node.X},{node.Y}) ";
    //        }

    //        Debug.Log($"Arrow {index} [{arrow.Direction}] Path: {pathStr}");

    //        if (arrow.Dependencies.Count > 0)
    //        {
    //            string deps = "";
    //            foreach (var dep in arrow.Dependencies)
    //            {
    //                int depIndex = arrows.IndexOf(dep) + 1;
    //                deps += depIndex + " ";
    //            }
    //            Debug.Log($"   Depends on: {deps}");
    //        }

    //        index++;
    //    }
    //}
}
