using UnityEngine;
using UnityEngine.SceneManagement;

// Ending screen: poster drifts in slightly while the canvas fades; button returns to the title.
public class EndingController : MonoBehaviour
{
    public RectTransform poster;
    float t;

    void Start() => Time.timeScale = 1f;

    void Update()
    {
        t += Time.unscaledDeltaTime;
        if (poster != null)
        {
            float s = Mathf.Lerp(1.04f, 1f, Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 2.5f)));
            poster.localScale = Vector3.one * (s + 0.006f * Mathf.Sin(t * 0.9f));
        }
    }

    public void ReturnToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
