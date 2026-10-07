using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Clean Up Settings")]
    [SerializeField] private GameObject[] uiElementsToHide;

    private bool isGameOver = false;

    private void OnEnable()
    {
        PlayerHealth.OnPlayerDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        PlayerHealth.OnPlayerDeath -= HandlePlayerDeath;
    }

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // If it's game over, force the cursor to stay visible 
        // This overrides camera/movement scripts that try to hide it
        if (isGameOver)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandlePlayerDeath()
    {
        isGameOver = true;

        ToggleUIElements(false);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Force cursor settings immediately
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    private void ToggleUIElements(bool state)
    {
        if (uiElementsToHide == null) return;

        foreach (GameObject element in uiElementsToHide)
        {
            if (element != null)
            {
                element.SetActive(state);
            }
        }
    }

    public void RestartGame()
    {
        isGameOver = false;
        Time.timeScale = 1f;

        // Optional: Re-hide the cursor for gameplay when reloading
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Debug.Log("Application is closing...");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;

        #else
        Application.Quit();

        #endif
    }
}
