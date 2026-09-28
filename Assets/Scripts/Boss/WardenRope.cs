using UnityEngine;

// Rope/cable drawn between a BreakableSupport and the thing it holds; snaps (hides) when the support breaks.
public class WardenRope : MonoBehaviour
{
    public BreakableSupport support;
    public WardenStriker payload;   // swung into target when the cable snaps
    public Transform target;
    public float arc = 1.2f, time = 0.75f;
    bool snapped;

    void Update()
    {
        if (snapped) return;
        if (support == null || support.IsBroken)
        {
            snapped = true;
            if (payload != null && target != null) payload.Launch(target, arc, time);
            gameObject.SetActive(false);
        }
    }
}
