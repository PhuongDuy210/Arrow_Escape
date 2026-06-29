using UnityEngine;

public class ClickDetector : MonoBehaviour
{
    private ArrowRenderer arrowRenderer;
    private Arrow arrowData;

    /// <summary>
    /// Assign the ArrowRenderer this tip belongs to.
    /// </summary>
    public void Init(ArrowRenderer renderer, Arrow arrow)
    {
        arrowRenderer = renderer;
        arrowData = arrow;
    }

    // For mouse clicks (works in editor and desktop builds)
    private void OnMouseDown()
    {
        if (arrowRenderer != null && arrowData != null)
        {
            GameEventHandler.TryMoveArrow(arrowData, arrowRenderer);
        }
    }

    // For mobile/touch input using Unity’s EventSystem
    public void OnTap()
    {
        if (arrowRenderer != null && arrowData != null)
        {
            GameEventHandler.TryMoveArrow(arrowData, arrowRenderer);
        }
    }
}
