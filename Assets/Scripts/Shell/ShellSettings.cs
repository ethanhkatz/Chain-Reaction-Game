using UnityEngine;

// Player-facing accessibility settings, persisted in PlayerPrefs.
public static class ShellSettings
{
    public static float GameSpeed
    {
        get => PlayerPrefs.GetFloat("shell.speed", 1f);
        set => PlayerPrefs.SetFloat("shell.speed", Mathf.Clamp(value, 0.5f, 1f));
    }

    public static bool HighContrast
    {
        get => PlayerPrefs.GetInt("shell.contrast", 0) == 1;
        set => PlayerPrefs.SetInt("shell.contrast", value ? 1 : 0);
    }

    // The time scale gameplay should run at right now (0 while GameManager has frozen the level).
    public static float PlayTimeScale => GameManager.Frozen ? 0f : GameSpeed;
}
