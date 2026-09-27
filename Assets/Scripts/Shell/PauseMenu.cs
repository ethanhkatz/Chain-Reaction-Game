using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Escape pauses. "AAA Accessibility": game speed slider (0.5x-1x) and a high-contrast toggle.
public class PauseMenu : MonoBehaviour
{
    GameObject root;
    TextMeshProUGUI speedLabel, contrastLabel;
    Slider slider;
    public bool Paused { get; private set; }

    void Start()
    {
        var canvas = ShellUI.MakeCanvas("PauseCanvas", 200);
        canvas.transform.SetParent(transform, false);
        root = canvas.gameObject;

        var dim = ShellUI.Rect("Dim", canvas.transform, Vector2.zero, Vector2.zero, Vector2.zero);
        ShellUI.Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0.03f, 0.03f, 0.04f, 0.82f);

        var panel = ShellUI.Rect("Panel", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 600));
        panel.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.11f, 0.13f, 0.97f);
        var o = panel.gameObject.AddComponent<Outline>();
        o.effectColor = ShellUI.Cyan; o.effectDistance = new Vector2(3, -3);

        ShellUI.Text(panel, "PAUSED", 64, new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(600, 90), ShellUI.Cyan).fontStyle = FontStyles.Bold;

        speedLabel = ShellUI.Text(panel, "", 32, new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(560, 50), Color.white);
        slider = BuildSlider(panel, new Vector2(0, 90));
        slider.minValue = 0.5f; slider.maxValue = 1f;
        slider.value = ShellSettings.GameSpeed;
        slider.onValueChanged.AddListener(v => { ShellSettings.GameSpeed = Mathf.Round(v * 20f) / 20f; RefreshLabels(); });

        var contrast = ShellUI.TextButton(panel, "", new Vector2(0, 0), ToggleContrast);
        contrastLabel = contrast.GetComponentInChildren<TextMeshProUGUI>();
        ShellUI.TextButton(panel, "Resume", new Vector2(0, -95), Resume);
        ShellUI.TextButton(panel, "Restart Level", new Vector2(0, -175), () => { Resume(); GameManager.instance?.RestartScene(); });
        ShellUI.TextButton(panel, "Quit to Title", new Vector2(0, -255), () => { Resume(); Time.timeScale = 1f; SceneManager.LoadScene(0); });

        ShellUI.Text(panel, "Esc to resume", 22, new Vector2(0.5f, 0f), new Vector2(0, -30), new Vector2(560, 40), new Color(1, 1, 1, 0.5f));

        RefreshLabels();
        root.SetActive(false);
        ShellUI.EnsureEventSystem();
    }

    Slider BuildSlider(Transform parent, Vector2 pos)
    {
        var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(440, 28);
        foreach (var img in go.GetComponentsInChildren<Image>())
        {
            if (img.name == "Background") img.color = new Color(0.25f, 0.27f, 0.3f);
            else if (img.name == "Fill") img.color = ShellUI.Cyan;
            else if (img.name == "Handle") { img.color = Color.white; img.rectTransform.sizeDelta = new Vector2(28, 0); }
        }
        return go.GetComponent<Slider>();
    }

    void RefreshLabels()
    {
        speedLabel.text = $"Game Speed  {ShellSettings.GameSpeed:0.00}x";
        contrastLabel.text = "High Contrast: " + (ShellSettings.HighContrast ? "ON" : "OFF");
    }

    void ToggleContrast()
    {
        ShellSettings.HighContrast = !ShellSettings.HighContrast;
        HighContrast.Apply(ShellSettings.HighContrast);
        RefreshLabels();
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null || root == null) return;
        if (k.escapeKey.wasPressedThisFrame || k.pKey.wasPressedThisFrame)
        {
            if (Paused) Resume();
            else if (!GameManager.Frozen) Pause();
        }
    }

    void Pause()
    {
        Paused = true;
        root.SetActive(true);
        Time.timeScale = 0f;
        EventSystem.current?.SetSelectedGameObject(slider.gameObject);
        ShellAudio.Play(ShellAudio.Sfx.Click, 0.6f, 0f);
    }

    void Resume()
    {
        if (!Paused) return;
        Paused = false;
        root.SetActive(false);
        Time.timeScale = ShellSettings.PlayTimeScale;
        EventSystem.current?.SetSelectedGameObject(null);
    }
}
