using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Cainos.PixelArtTopDown_Basic
{
    // Layar penutup: fade in panel "Terima kasih", tunggu beberapa detik, lalu kembali ke main menu.
    //
    // PASANG script ini di objek yang SELALU AKTIF (mis. ==UI_Manager==), jangan di objek yang awalnya nonaktif,
    // karena coroutine tidak bisa jalan di objek nonaktif. Panel-nya boleh nonaktif di awal.
    //
    // Cara memicu: hubungkan EndingScreen.Show ke event "On Door Opened" di RoomDoor pintu terakhir,
    // atau pakai EndingTrigger.
    public class EndingScreen : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Panel layar penutup. Harus punya CanvasGroup. Isi dengan Image hitam + teks Terima kasih di dalamnya.")]
        public CanvasGroup panel;

        [Header("Waktu (detik)")]
        public float delayBeforeShow = 1f;   // jeda setelah dipicu (mis. biar pintu terlihat terbuka dulu)
        public float fadeInTime = 1.5f;
        public float showDuration = 4f;      // lama panel tampil sebelum pindah scene

        [Header("Setelah selesai")]
        public bool loadScene = true;
        [Tooltip("Nama scene main menu (harus ada di File > Build Settings > Scenes In Build). Kosong = scene urutan pertama (index 0).")]
        public string mainMenuSceneName = "MainMenu";
        public UnityEvent onEndingFinished;  // dipanggil sebelum pindah scene; bisa dipakai kalau main menu-mu bukan scene terpisah

        [Header("Player")]
        [Tooltip("Matikan kontrol dan buat player kebal selama layar penutup.")]
        public bool freezePlayer = true;

        public bool IsShowing { get; private set; }

        private void Awake()
        {
            if (panel != null)
            {
                panel.alpha = 0f;
                panel.blocksRaycasts = false;
                panel.gameObject.SetActive(false);
            }
        }

        public void Show()
        {
            if (IsShowing) return;
            IsShowing = true;
            StartCoroutine(EndingRoutine());
        }

        private IEnumerator EndingRoutine()
        {
            yield return WaitUnscaled(delayBeforeShow);

            if (freezePlayer) FreezePlayer();

            if (panel != null)
            {
                panel.gameObject.SetActive(true);
                panel.alpha = 0f;
                panel.blocksRaycasts = true;

                float t = 0f;
                while (t < fadeInTime)
                {
                    t += Time.unscaledDeltaTime;
                    panel.alpha = Mathf.Clamp01(t / fadeInTime);
                    yield return null;
                }
                panel.alpha = 1f;
            }

            yield return WaitUnscaled(showDuration);

            onEndingFinished.Invoke();

            if (loadScene)
            {
                Time.timeScale = 1f; // jaga-jaga kalau game sedang di-pause
                LoadMainMenu();
            }
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void FreezePlayer()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;

            var death = p.GetComponentInParent<PlayerDeath>();
            if (death != null) death.godMode = true;

            var skills = p.GetComponentInParent<PlayerSkills>();
            if (skills != null)
            {
                skills.ResetState();
                skills.enabled = false;
            }

            var controller = p.GetComponentInParent<TopDownCharacterController>();
            if (controller != null) controller.movementLocked = true;

            var rb = p.GetComponentInParent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
        }

        private void LoadMainMenu()
        {
            if (string.IsNullOrEmpty(mainMenuSceneName))
            {
                SceneManager.LoadScene(0);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError($"[EndingScreen] Scene \"{mainMenuSceneName}\" tidak ditemukan. " +
                               "Cek ejaan namanya dan pastikan scene sudah ada di File > Build Settings > Scenes In Build.", this);
                return;
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}