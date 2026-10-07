using UnityEngine;
using UnityEngine.SceneManagement;

public class WinManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject winPanel;

    [Header("Clean Up Settings")]
    [SerializeField] private GameObject[] uiElementsToHide;

    [Header("Player Detection Settings")]
    [SerializeField] private string playerTag = "Player";

    private bool isWin = false;

    private void Start()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Force the cursor to stay visible when the win screen is active
        if (isWin)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is the player
        if (other.CompareTag(playerTag))
        {
            HandleWin();
        }
    }

    private void HandleWin()
    {
        isWin = true;

        ToggleUIElements(false);

        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        // Force cursor settings immediately
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Freeze game time
        Time.timeScale = 0f;

        Debug.Log("Player has won the game!");
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
        isWin = false;
        Time.timeScale = 1f;
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