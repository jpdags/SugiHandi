using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SugiHandi.UI
{
    public class MainMenu : MonoBehaviour
    {
        [Header("UI Buttons")]
        public Button startGameButton;
        public Button settingsButton;
        public Button quitButton;

        [Header("Panels")]
        public GameObject mainMenuPanel;
        public GameObject settingsPanel;

        [Header("Settings")]
        public string gameplaySceneName = "DemoScene";

        private void Start()
        {
            // Ensure main menu is visible and settings is hidden on start
            ShowMainMenu();

            if (startGameButton != null)
                startGameButton.onClick.AddListener(StartGame);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(ShowSettings);

            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);
        }

        public void StartGame()
        {
            Debug.Log("Starting game... Loading scene: " + gameplaySceneName);
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void ShowSettings()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void QuitGame()
        {
            Debug.Log("Quitting game...");
            Application.Quit();
        }
    }
}
