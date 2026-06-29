using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    private PuzzleGenerator generator;
    private MovementManager movementManager;
    
    private int level = 4;
    private PuzzleSetting settings;
    private Grid grid;

    private List<Arrow> arrows;

    private GameMode gameMode = GameMode.Zen;

    [SerializeField]
    private TMP_Text lvlText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        generator = gameObject.GetComponent<PuzzleGenerator>();
        movementManager = gameObject.GetComponent<MovementManager>();

        lvlText.text = "LEVEL " + level.ToString();

        settings = LevelLoader.LoadSettings(level.ToString());
        grid = new Grid(settings);

        arrows = generator.GeneratePuzzle(grid, settings);
    }

    private void OnEnable()
    {
        GameEventHandler.OnArrowChosen += TryMoveArrow;
    }

    private void OnDisable()
    {
        GameEventHandler.OnArrowChosen -= TryMoveArrow;
    }

    private void TryMoveArrow(Arrow arrowData, ArrowRenderer arrowRenderer) 
    {
        if (movementManager.IsMoving) return;

        if (gameMode == GameMode.Puzzle)
        {
            if (IsPathClear(arrowData))
            {
                // Path is clear → move until off-screen
                Debug.Log("Path is cleared. Moving arrow");
                StartCoroutine(movementManager.MoveArrowUntilOffscreen(arrowData, arrowRenderer, grid));
            }
            else
            {
                // Path blocked → calculate max steps
                int maxSteps = CalculateMaxSteps(arrowData);
                if (maxSteps > 0)
                {
                    StartCoroutine(movementManager.MoveArrowSteps(arrowData, arrowRenderer, grid, maxSteps));
                }
                else
                {
                    Debug.Log("Arrow path blocked, refusing to move.");
                    arrowRenderer.TriggerShake();
                }
            }
        }
        else if (gameMode == GameMode.Zen)
        {
            if (IsPathClear(arrowData))
            {
                // Path is clear → move until off-screen
                StartCoroutine(movementManager.MoveArrowUntilOffscreen(arrowData, arrowRenderer, grid));
            }
            else
            {
                Debug.Log("Arrow path blocked, refusing to move.");
                arrowRenderer.TriggerShake();
            }
        }
    }

    private int CalculateMaxSteps(Arrow arrow)
    {
        Node tip = arrow.Path.Last();
        var (dx, dy) = Helper.ToVector(tip.Dir);

        int steps = 0;

        while (true)
        {
            int nextX = tip.X + dx;
            int nextY = tip.Y + dy;

            // Out of bounds → stop
            if (nextX < 0 || nextX >= grid.Width || nextY < 0 || nextY >= grid.Height)
                break;

            // Collision → stop
            if (grid.Nodes.TryGetValue((nextX, nextY), out Node nextNode))
            {
                if (arrows.Any(a => a.Path.Contains(nextNode)))
                    break;
            }

            // Advance tip
            tip = new Node(nextX, nextY, tip.Dir);
            steps++;
        }

        return steps;
    }

    private bool IsPathClear(Arrow arrow)
    {
        Node tip = arrow.Path.Last();
        var (dx, dy) = Helper.ToVector(tip.Dir);

        while (true)
        {
            int nextX = tip.X + dx;
            int nextY = tip.Y + dy;

            // Out of bounds → path is clear (arrow would leave grid eventually)
            if (nextX < 0 || nextX >= grid.Width || nextY < 0 || nextY >= grid.Height)
                return true;

            // Collision → path blocked
            if (grid.Nodes.TryGetValue((nextX, nextY), out Node nextNode))
            {
                if (arrows.Any(a => a.Path.Contains(nextNode)))
                    return false;
            }

            // Advance tip
            tip = new Node(nextX, nextY, tip.Dir);
        }
    }
}
