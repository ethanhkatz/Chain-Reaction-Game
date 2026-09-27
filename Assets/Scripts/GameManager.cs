using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    //Singleton pattern
    public static GameManager instance { get; private set; }

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject levelClearPanel;

    // Raised when the level ends either way; the shell (audio, pause menu) listens to these.
    public static event Action GameOverRaised;
    public static event Action LevelClearRaised;

    // True while Game Over / Level Clear has frozen time, so other systems don't unfreeze it.
    public static bool Frozen { get; private set; }

    private void Awake()
    {
        instance = this;
        Frozen = false;
    }

    public void GameOver()
    {
        if (Frozen) return;
        //Freeze the game
        Frozen = true;
        Time.timeScale = 0f;
        GameOverRaised?.Invoke();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void LevelClear()
    {
        if (Frozen) return;
        Frozen = true;
        Time.timeScale = 0f;
        LevelClearRaised?.Invoke();

        if (levelClearPanel != null)
        {
            levelClearPanel.SetActive(true);
        }
    }
    public void AdvanceLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void RestartScene()
    {
        //Reset time scale
        Time.timeScale = 1f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        if (levelClearPanel != null)
        {
            levelClearPanel.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
