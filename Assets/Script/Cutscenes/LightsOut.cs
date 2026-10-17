using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class LightsOut : MonoBehaviour
{
    [SerializeField] private float startDelay = 0f;

    [Tooltip("Urutan durasi: mati, nyala, mati, nyala, dst.")]
    [SerializeField] private float[] flickerPattern = { 0.05f, 0.12f, 0.08f, 0.06f, 0.15f, 0.30f };

    [Tooltip("Lama fade menuju mati total di akhir (detik). 0 = langsung mati.")]
    [SerializeField] private float finalFadeDuration = 0.6f;

    private CanvasGroup group;
    private bool played;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f; // awalnya lampu nyala
    }

    public void Play()
    {
        Debug.Log("LightsOut.Play() terpanggil");
        if (played) return;
        played = true;
        StartCoroutine(Run());
    }

    // Klik kanan judul komponen di Inspector lalu pilih "Test Play" (saat game Play)
    [ContextMenu("Test Play")]
    private void TestPlay() { Play(); }

    private IEnumerator Run()
    {
        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        bool dark = false;
        foreach (float d in flickerPattern)
        {
            dark = !dark;
            group.alpha = dark ? 1f : 0f;
            yield return new WaitForSecondsRealtime(d);
        }

        // Fade ke lampu mati permanen
        float start = group.alpha;
        float t = 0f;
        while (t < finalFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, 1f, t / finalFadeDuration);
            yield return null;
        }
        group.alpha = 1f;
    }
}