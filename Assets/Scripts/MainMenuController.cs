using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Tooltip("Logo that gently bobs and pulses.")]
    [SerializeField] private RectTransform logo;
    [Tooltip("Canvas group faded in on start.")]
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeDuration = 1.2f;

    private Vector2 logoPos;
    private Vector3 logoScale;
    private float t;

    void Start()
    {
        Time.timeScale = 1f;
        if (logo != null) { logoPos = logo.anchoredPosition; logoScale = logo.localScale; }
        if (fadeGroup != null) fadeGroup.alpha = 0f;
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        if (fadeGroup != null) fadeGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeDuration));
        if (logo != null)
        {
            logo.anchoredPosition = logoPos + new Vector2(0f, 7f * Mathf.Sin(t * 1.6f));
            logo.localScale = logoScale * (1f + 0.012f * Mathf.Sin(t * 3.2f));
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(1);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game Activated");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif !UNITY_WEBGL
        Application.Quit();
#endif
    }
}
