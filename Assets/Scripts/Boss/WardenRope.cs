using UnityEngine;

// Rope/cable drawn between a BreakableSupport and the thing it holds; snaps (hides) when the support breaks.
public class WardenRope : MonoBehaviour
{
    public BreakableSupport support;
    bool snapped;

    void Update()
    {
        if (snapped) return;
        if (support == null || support.IsBroken)
        {
            snapped = true;
            gameObject.SetActive(false);
        }
    }
}
