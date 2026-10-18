using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string startSceneName = "Comic";
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;

    private bool starting;

    private void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        float vol = PlayerPrefs.GetFloat("volume", 1f);
        bool full = PlayerPrefs.GetInt("fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        AudioListener.volume = vol;
        Screen.fullScreen = full;

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(vol);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(full);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) CloseAll();
    }

    public void StartGame()
    {
        if (starting) return;
        starting = true;
        if (SceneFader.Instance != null)
            SceneFader.Instance.LoadScene(startSceneName);
        else
            SceneManager.LoadScene(startSceneName);
    }

    public void OpenSettings()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OpenCredits()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void CloseAll()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetVolume(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat("volume", v);
    }

    private void SetFullscreen(bool on)
    {
        Screen.fullScreen = on;
        PlayerPrefs.SetInt("fullscreen", on ? 1 : 0);
    }
}