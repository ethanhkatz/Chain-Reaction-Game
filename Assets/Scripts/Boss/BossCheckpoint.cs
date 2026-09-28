using UnityEngine;
using UnityEngine.SceneManagement;

// Trigger at the Warden's arena door. Once the player has reached it, dying in this level restarts here
// instead of at the level start (the flag is dropped when the level is cleared or another scene loads).
[RequireComponent(typeof(Collider2D))]
public class BossCheckpoint : MonoBehaviour
{
    public static bool Reached;
    static string reachedScene;
    static bool hooked;

    public Vector2 spawn;
    public Vector2 ballOffset = new Vector2(-2.5f, -0.3f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Reached = false;
        reachedScene = null;
        hooked = false;
    }

    void Awake()
    {
        if (!hooked)
        {
            hooked = true;
            SceneManager.sceneLoaded += (s, m) => { if (s.name != reachedScene) Reached = false; };
            GameManager.LevelClearRaised += () => Reached = false;
        }
        if (Reached && reachedScene == gameObject.scene.name) Relocate();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Reached || other.GetComponentInParent<PlayerController>() == null) return;
        Reached = true;
        reachedScene = gameObject.scene.name;
        Debug.Log("JAM: boss checkpoint reached");
    }

    void Relocate()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;
        var ball = GameObject.FindWithTag("Ball");
        Vector3 from = player.transform.position;
        Vector3 to = new Vector3(spawn.x, spawn.y, from.z);
        player.transform.position = to;
        if (player.TryGetComponent(out Rigidbody2D prb)) prb.position = to;
        if (ball != null)
        {
            Vector3 bp = to + (Vector3)ballOffset;
            ball.transform.position = bp;
            if (ball.TryGetComponent(out Rigidbody2D brb)) brb.position = bp;
        }
        Vector3 delta = to - from;
        foreach (var vcam in FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsSortMode.None))
        {
            vcam.transform.position += delta;
            vcam.OnTargetObjectWarped(player.transform, delta);
        }
        var cam = Camera.main;
        if (cam != null) cam.transform.position += delta;
        Debug.Log("JAM: restarting at boss checkpoint");
    }
}
