using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    [SerializeField] private AudioClip music;

    private void Start()
    {
        if (AudioManager.Instance != null && music != null)
            AudioManager.Instance.PlayMusic(music);
    }
}