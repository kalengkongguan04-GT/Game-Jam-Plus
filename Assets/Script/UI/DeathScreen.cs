using System.Collections;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    // Layar kematian: memudar ke hitam, menampilkan tulisan "DEAD", lalu memudar kembali saat respawn.
    // Pasang di Canvas khusus (GameObject-nya harus tetap aktif). PlayerDeath yang memanggilnya.
    // Semua animasi memakai waktu asli (unscaled), jadi tidak terpengaruh Time.timeScale.
    public class DeathScreen : MonoBehaviour
    {
        [Header("Referensi UI")]
        public CanvasGroup overlay;      // panel hitam layar penuh
        public CanvasGroup textGroup;    // objek berisi tulisan "DEAD"

        [Header("Durasi (detik)")]
        public float fadeToBlackDuration = 0.5f;    // layar menjadi hitam
        public float textFadeInDuration = 0.3f;     // tulisan DEAD muncul
        public float holdDuration = 1.2f;           // lama tulisan DEAD terlihat sebelum respawn
        public float fadeFromBlackDuration = 0.5f;  // layar terang kembali setelah respawn

        private Coroutine outRoutine;

        private void Awake()
        {
            SetAlpha(overlay, 0f);
            SetAlpha(textGroup, 0f);
        }

        // Fade ke hitam, tampilkan DEAD, tunggu. Dipanggil PlayerDeath dengan "yield return".
        public IEnumerator PlayIn()
        {
            if (outRoutine != null)
            {
                StopCoroutine(outRoutine);
                outRoutine = null;
            }

            yield return Fade(overlay, overlay != null ? overlay.alpha : 0f, 1f, fadeToBlackDuration);
            yield return Fade(textGroup, textGroup != null ? textGroup.alpha : 0f, 1f, textFadeInDuration);
            yield return WaitUnscaled(holdDuration);
        }

        // Layar mulai terang lagi. Tidak ditunggu, supaya player bisa langsung bergerak.
        public void PlayOut()
        {
            if (outRoutine != null) StopCoroutine(outRoutine);
            outRoutine = StartCoroutine(OutRoutine());
        }

        private IEnumerator OutRoutine()
        {
            SetAlpha(textGroup, 0f); // tulisan hilang langsung, layar hitam memudar
            yield return Fade(overlay, overlay != null ? overlay.alpha : 0f, 0f, fadeFromBlackDuration);
            outRoutine = null;
        }

        private IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;

            if (duration <= 0f)
            {
                group.alpha = to;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }

        private IEnumerator WaitUnscaled(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static void SetAlpha(CanvasGroup group, float a)
        {
            if (group != null) group.alpha = a;
        }
    }
}