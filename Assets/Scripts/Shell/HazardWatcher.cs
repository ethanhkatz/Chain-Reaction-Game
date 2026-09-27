using System.Collections.Generic;
using UnityEngine;

// Watches stalactites and pops the player's "!" the moment one starts to fall.
public class HazardWatcher : MonoBehaviour
{
    readonly List<Rigidbody2D> bodies = new List<Rigidbody2D>();
    readonly List<bool> falling = new List<bool>();

    void Start()
    {
        foreach (var s in FindObjectsByType<StalactiteController>(FindObjectsInactive.Exclude))
        {
            var rb = s.GetComponent<Rigidbody2D>();
            if (rb == null) continue;
            bodies.Add(rb);
            falling.Add(false);
        }
        if (bodies.Count == 0) enabled = false;
    }

    void Update()
    {
        for (int i = 0; i < bodies.Count; i++)
        {
            var rb = bodies[i];
            if (rb == null || falling[i]) continue;
            // StalactiteController.Start forces Kinematic; switching to Dynamic means it was triggered.
            if (rb.bodyType == RigidbodyType2D.Dynamic && Time.timeSinceLevelLoad > 0.2f)
            {
                falling[i] = true;
                PlayerShellFx.Alert();
            }
        }
    }
}
