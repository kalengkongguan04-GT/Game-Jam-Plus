using UnityEngine;

public static class GameSettings
{
    private const string VolumeKey = "volume";
    private const string FullscreenKey = "fullscreen";

    public static float Volume
    {
        get => PlayerPrefs.GetFloat(VolumeKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(VolumeKey, value);
            AudioListener.volume = value;
        }
    }

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        set
        {
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            Screen.fullScreen = value;
        }
    }

    public static void Save() => PlayerPrefs.Save();

    // Otomatis jalan sekali saat game dibuka, di scene mana pun
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyOnStartup()
    {
        AudioListener.volume = Volume;
        Screen.fullScreen = Fullscreen;
    }
}