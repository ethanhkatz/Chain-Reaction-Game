using UnityEngine;

public class ChainBridge : MonoBehaviour
{
    public Transform playerTarget;
    public Transform ballTarget;
    
    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
        }
    }

    void LateUpdate()
    {
        if (playerTarget != null && ballTarget != null && lineRenderer != null)
        {
            lineRenderer.SetPosition(0, playerTarget.position);
            lineRenderer.SetPosition(1, ballTarget.position);
        }
    }
}
