using UnityEngine;
using UnityEngine.UI;

namespace SugiHandi.UI
{
    public class SettingsMenu : MonoBehaviour
    {
        [Header("UI Elements")]
        public Slider volumeSlider;
        public Button backButton;
        
        [Header("References")]
        public MainMenu mainMenu;

        private void Start()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            if (volumeSlider != null)
            {
                // Initialize slider value based on PlayerPrefs or default to 1
                volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
        }

        private void OnVolumeChanged(float value)
        {
            // Save settings locally
            PlayerPrefs.SetFloat("MasterVolume", value);
            PlayerPrefs.Save();
            
            // In a real implementation, you'd update the AudioMixer here
            Debug.Log("Volume changed to: " + value);
        }

        private void OnBackClicked()
        {
            if (mainMenu != null)
            {
                mainMenu.ShowMainMenu();
            }
            else
            {
                // Fallback in case mainMenu reference isn't set
                gameObject.SetActive(false);
            }
        }
    }
}
