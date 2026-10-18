using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Musik latar")]
    [SerializeField] private AudioClip menuMusic;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.6f;

    [Header("Efek suara UI")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip panelSound;   // BARU: suara tiap panel cutscene
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        // Hanya boleh ada satu AudioManager
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        PlayMusic(menuMusic);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic() { musicSource.Stop(); }

    public void PlayClick() { Play(clickSound); }
    public void PlayHover() { Play(hoverSound); }
    public void PlayPanel() { Play(panelSound); }          // BARU
    public void PlaySfx(AudioClip clip) { Play(clip); }    // BARU

    private void Play(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume);
    }
}