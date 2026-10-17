using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class AuroraFade : MonoBehaviour
{
    [Tooltip("Lama fade ke aurora terang (detik)")]
    [SerializeField] private float fadeDuration = 2f;

    [Tooltip("Tunggu sebelum fade mulai (detik)")]
    [SerializeField] private float startDelay = 0f;

    private CanvasGroup group;
    private bool played;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f; // awalnya aurora redup (overlay tersembunyi)
    }

    // Dipanggil dari On Step Start di ComicPage
    public void Play()
    {
        if (played) return;
        played = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            k = k * k * (3f - 2f * k); // smoothstep: mulai dan akhir lebih halus
            group.alpha = k;
            yield return null;
        }
        group.alpha = 1f;
    }
}