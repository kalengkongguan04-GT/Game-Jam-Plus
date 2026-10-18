using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene tujuan saat Start (cutscene pertama)")]
    [SerializeField] private string startSceneName = "Comic";

    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    private bool starting;

    private void Start()
    {
        CloseAll();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) CloseAll();
    }

    public void StartGame()
    {
        if (starting) return;
        starting = true;

        // Hentikan musik menu
        if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();

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
}