using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ArrowRenderer : MonoBehaviour
{
    private LineRenderer lineRenderer;

    [Header("Visual Settings")]
    public float tileSize = 1f;
    public float lineWidth = 0.1f;
    public Material lineMaterial;
    public GameObject arrowTipPrefab;

    private Arrow arrowData;
    private GameObject tipInstance;
    private Vector3 gridOffset;

    [Header("Movement Settings")]
    public float moveSpeed = 2f; // units per second
    public bool isMoving = false;

    [Header("Shake Settings")]
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private List<Vector3> originalLinePositions = new List<Vector3>();

    public float shakeDuration = 0.3f;
    public float shakeMagnitude = 0.0005f;
    public float shakeFrequency = 25f;

    // Store the path as world positions
    private List<Vector3> pathPoints = new List<Vector3>();

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
    }

    /// <summary>
    /// Render the arrow path using LineRenderer.
    /// </summary>
    public void RenderArrow(Arrow arrow, Vector3 gridOffset)
    {
        if (arrow == null || arrow.Path.Count == 0) return;

        arrowData = arrow;
        this.gridOffset = gridOffset;

        if (lineMaterial != null)
            lineRenderer.material = lineMaterial;

        pathPoints.Clear();
        foreach (var node in arrow.Path)
            pathPoints.Add(NodeCenter(node));

        List<Vector3> points = new List<Vector3>();

        if (arrow.Path.Count == 1)
        {
            // Single-tile arrow: short stub inside the tile
            Node tip = arrow.Path[0];
            Vector3 center = NodeCenter(tip);
            var (dx, dy) = Helper.ToVector(tip.Dir);

            Vector3 start = center - new Vector3(dx, dy, 0f) * (tileSize * 0.25f);
            Vector3 end = center + new Vector3(dx, dy, 0f) * (tileSize * 0.25f);

            points.Add(start);
            points.Add(end);
        }
        else
        {
            // Multi-tile arrow: center-to-center, but stop at edge of last tile
            for (int i = 0; i < arrow.Path.Count; i++)
            {
                Node node = arrow.Path[i];
                Vector3 center = NodeCenter(node);

                if (i == arrow.Path.Count - 1)
                {
                    // Last node → offset toward edge in tip direction
                    var (dx, dy) = Helper.ToVector(node.Dir);
                    Vector3 offset = new Vector3(dx, dy, 0f) * (tileSize * 0.5f);
                    points.Add(center - offset); // stop at edge
                }
                else
                {
                    points.Add(center);
                }
            }
        }

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());

        AttachTip();
    }

    /// <summary>
    /// Converts a grid node into the center of its tile.
    /// </summary>
    private Vector3 NodeCenter(Node node)
    {
        Vector3 pos = new Vector3(node.X * tileSize, node.Y * tileSize, 0f);

        return pos - gridOffset;
    }

    public Vector3 TailPosition()
    {
        return  lineRenderer.GetPosition(0);
    }

    private void AttachTip()
    {
        Vector3 tipPos = lineRenderer.GetPosition(lineRenderer.positionCount - 1);
        tipInstance = Instantiate(arrowTipPrefab, tipPos, Quaternion.identity);

        // Attach click detector and give it a reference
        ClickDetector detector = tipInstance.AddComponent<ClickDetector>();
        detector.Init(this, arrowData);

        // Get the sprite size in world units
        SpriteRenderer sr = tipInstance.GetComponent<SpriteRenderer>();
        Vector2 spriteSize = sr.bounds.size;

        // Direction offset (use the longer edge of the rectangle as the arrow length)
        Direction dir = arrowData.Path.Last().Dir;
        Vector3 offset = Vector3.zero;

        switch (dir)
        {
            case Direction.Up:
                tipInstance.transform.rotation = Quaternion.Euler(0, 0, 90);
                offset = new Vector3(0f, spriteSize.x * 0.5f, 0f); // use height
                break;
            case Direction.Down:
                tipInstance.transform.rotation = Quaternion.Euler(0, 0, -90);
                offset = new Vector3(0f, -spriteSize.x * 0.5f, 0f);
                break;
            case Direction.Left:
                tipInstance.transform.rotation = Quaternion.Euler(0, 0, 180);
                offset = new Vector3(-spriteSize.x * 0.5f, 0f, 0f); // use width
                break;
            case Direction.Right:
                tipInstance.transform.rotation = Quaternion.Euler(0, 0, 0);
                offset = new Vector3(spriteSize.x * 0.5f, 0f, 0f);
                break;
        }

        // Apply offset so the tip extends the line
        tipInstance.transform.position += offset;

        // Parent it to the line's parent so it moves with updates
        tipInstance.transform.SetParent(lineRenderer.transform.parent.transform);
    }

    public IEnumerator MoveOneStep()
    {
        if (pathPoints.Count <= 1) yield break;

        // Calculate next targets
        var (dx, dy) = Helper.ToVector(arrowData.Path.Last().Dir);
        Vector3 tipTarget = tipInstance.transform.position + new Vector3(dx, dy, 0f) * tileSize;
        Vector3 tailTarget;
        if (pathPoints.Count > 2)
        {
            // Normal case: tail moves toward next corner
            tailTarget = pathPoints[1];
        }
        else
        {
            // Only two points left: tail should chase the tip with the same pace
            tailTarget = pathPoints[0] + new Vector3(dx, dy, 0f) * tileSize;
        }

        // Animate both ends simultaneously
        while ((tipInstance.transform.position - tipTarget).sqrMagnitude > 0.001f ||
               (pathPoints[0] - tailTarget).sqrMagnitude > 0.001f)
        {
            // Tip forward
            tipInstance.transform.position = Vector3.MoveTowards(
                tipInstance.transform.position,
                tipTarget,
                moveSpeed * Time.deltaTime
            );
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, tipInstance.transform.position);

            // Tail forward
            pathPoints[0] = Vector3.MoveTowards(
                pathPoints[0],
                tailTarget,
                moveSpeed * Time.deltaTime
            );
            lineRenderer.SetPosition(0, pathPoints[0]);

            yield return null;
        }

        // Snap both ends to final positions
        tipInstance.transform.position = tipTarget;

        lineRenderer.SetPosition(lineRenderer.positionCount - 1, tipTarget);

        pathPoints[0] = tailTarget;
        lineRenderer.SetPosition(0, tailTarget);

        // Consume corner visually
        if (pathPoints.Count > 2)   // keep at least tail + tip
        {
            pathPoints.RemoveAt(0);
            lineRenderer.positionCount = pathPoints.Count;
            pathPoints[pathPoints.Count - 1] = tipInstance.transform.position;
            lineRenderer.SetPositions(pathPoints.ToArray());
        }
        else
        {
            // Just update positions without removing
            lineRenderer.SetPosition(0, pathPoints[0]);
            lineRenderer.SetPosition(1, pathPoints[1]);
        }
    }

    public void ClearArrow()
    {
        Destroy(tipInstance.transform.parent.gameObject);
    }

    private void UpdateArrowOriginalPosition()
    {
        originalPosition = tipInstance.transform.localPosition;
        originalRotation = tipInstance.transform.localRotation;

        originalLinePositions.Clear();
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            originalLinePositions.Add(lineRenderer.GetPosition(i));
        }
    }

    public void TriggerShake()
    {
        StopAllCoroutines(); // cancel any ongoing shake
        StartCoroutine(ShakeCoroutine());
    }

    private IEnumerator ShakeCoroutine()
    {
        float timer = 0f;

        // Get the original position before shaking
        UpdateArrowOriginalPosition();

        while (timer < shakeDuration)
        {
            float offsetX = Mathf.Sin(Time.time * shakeFrequency) * shakeMagnitude;
            float offsetY = Mathf.Cos(Time.time * shakeFrequency) * shakeMagnitude;

            // Apply to the whole arrow transform
            tipInstance.transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0f);

            // If you want the LineRenderer to wiggle too:
            if (lineRenderer != null)
            {
                for (int i = 0; i < lineRenderer.positionCount; i++)
                {
                    lineRenderer.SetPosition(i, originalLinePositions[i] + new Vector3(offsetX, offsetY, 0f));
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Reset back to original
        tipInstance.transform.localPosition = originalPosition;
        tipInstance.transform.localRotation = originalRotation;

        if (lineRenderer != null)
        {
            for (int i = 0; i < lineRenderer.positionCount; i++)
            {
                lineRenderer.SetPosition(i, originalLinePositions[i]);
            }
        }
    }
}
