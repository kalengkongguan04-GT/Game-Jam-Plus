using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseManager : MonoBehaviour
{
    // Dicek dari script lain (player, attack, dll) buat nge-block input saat pause
    public static bool IsPaused { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button resumeButton;
    [SerializeField, FormerlySerializedAs("lobbyButton")] private Button mainMenuButton;

    [Header("Setting")]
    [SerializeField, FormerlySerializedAs("lobbySceneName")] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool pauseAudioToo = false; // true = semua suara ikut berhenti

    private const string VOLUME_KEY = "MasterVolume";

    private void Start()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);

        // Load volume tersimpan
        float saved = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
        volumeSlider.SetValueWithoutNotify(saved);
        AudioListener.volume = saved;

        volumeSlider.onValueChanged.AddListener(SetVolume);
        resumeButton.onClick.AddListener(Resume);
        mainMenuButton.onClick.AddListener(BackToMainMenu);
    }

    private void Update()
    {
        if (EscPressed())
        {
            if (IsPaused) Resume();
            else Pause();
        }
    }

    private bool EscPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    public void Pause()
    {
        IsPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f; // ini yang bener-bener ngefreeze game
        if (pauseAudioToo) AudioListener.pause = true;

        // Munculin kursor kalau game sebelumnya ngelock kursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        IsPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    public void BackToMainMenu()
    {
        // WAJIB dibalikin dulu, kalau nggak scene main menu ikut freeze
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VOLUME_KEY, value);
    }

    // Jaga-jaga kalau objek ini kehancur pas lagi pause
    private void OnDestroy()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}