using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;

    private void Awake()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(v => GameSettings.Volume = v);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(f => GameSettings.Fullscreen = f);
    }

    // Setiap panel dibuka, tampilan disamakan dengan nilai tersimpan
    private void OnEnable()
    {
        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(GameSettings.Volume);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
    }

    private void OnDisable() { GameSettings.Save(); }
}