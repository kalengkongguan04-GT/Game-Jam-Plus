using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PressAnyKey : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Gameplay";
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private float blinkSpeed = 3f;

    private bool started;

    void Update()
    {
        if (started) return;

        // Teks berkedip
        if (promptText != null)
        {
            float t = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f;
            promptText.alpha = Mathf.Lerp(0.3f, 1f, t);
        }

        if (AnyInputPressed())
            StartGame();
    }

    private bool AnyInputPressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool key = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool touch = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        bool pad = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        return key || mouse || touch || pad;
#else
        return Input.anyKeyDown;
#endif
    }

    public void StartGame()
    {
        if (started) return;
        started = true;
        SceneManager.LoadScene(gameSceneName);
    }
}
