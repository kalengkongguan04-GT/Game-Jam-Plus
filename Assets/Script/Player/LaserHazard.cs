using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Cainos.PixelArtTopDown_Basic
{
    // Jebakan laser di dunia. Pasang di objek dengan Collider2D (dibuat Is Trigger otomatis).
    //
    // Siklus: mati -> ABA-ABA (cahaya di arah laser, kedip makin cepat) -> laser menyala (mematikan) -> mati -> ulang.
    // Collider hanya membunuh saat laser benar-benar menyala (IsActive), bukan saat aba-aba.
    //
    // Player mati kalau menyentuh laser yang menyala, KECUALI:
    //   - Dash Can Pass    : player sedang dash (i-frame)
    //   - Shield Can Pass  : player sedang menahan shield
    [RequireComponent(typeof(Collider2D))]
    public class LaserHazard : MonoBehaviour
    {
        [Header("Cara melewati (pilih salah satu, keduanya, atau tidak sama sekali)")]
        public bool dashCanPass = true;
        public bool shieldCanPass = false;

        [Header("Siklus laser")]
        public bool alwaysOn = true;           // true = laser selalu menyala, tanpa aba-aba
        public float startDelay = 0f;          // jeda awal, supaya beberapa laser tidak sinkron
        public float onDuration = 2f;          // lama laser menyala
        public float offDuration = 2f;         // lama laser mati
        public float warningDuration = 1.0f;   // lama aba-aba sebelum menyala

        [Header("Aba-aba (cahaya sebelum laser menyala)")]
        [Tooltip("true = cahaya berkedip makin cepat. false = cahaya menyala terus selama aba-aba.")]
        public bool warningBlink = true;
        public float blinkRateStart = 3f;      // kedip per detik di awal aba-aba
        public float blinkRateEnd = 14f;       // kedip per detik di akhir aba-aba
        [Tooltip("Cahaya menyala terang tepat sebelum laser keluar (detik). 0 = tidak ada.")]
        public float solidBeforeFire = 0.1f;

        [Header("Visual")]
        public GameObject laserVisual;         // objek laser (sprite / LineRenderer), dinyalakan saat aktif
        public GameObject warningVisual;       // cahaya aba-aba, taruh di arah/posisi yang sama dengan laser
        public SpriteRenderer[] tintRenderers; // opsional: diwarnai sesuai aturan lewat
        public Color deadlyColor = new Color(1f, 0.15f, 0.15f);
        public Color dashOnlyColor = new Color(0.2f, 0.9f, 1f);
        public Color shieldOnlyColor = new Color(1f, 0.9f, 0.2f);
        public Color bothColor = new Color(0.4f, 1f, 0.4f);

        [Header("Opsional - aksi tambahan")]
        public UnityEvent onWarningStart;      // contoh: SFX "charging"
        public UnityEvent onLaserFire;         // contoh: SFX tembakan laser
        public UnityEvent onPlayerHit;

        public bool IsActive { get; private set; }

        private Collider2D hazardCollider;
        private ContactFilter2D filter;
        private readonly List<Collider2D> results = new List<Collider2D>();

        private void Awake()
        {
            hazardCollider = GetComponent<Collider2D>();
            hazardCollider.isTrigger = true;
            filter = new ContactFilter2D { useTriggers = true };
        }

        private void Start()
        {
            ApplyTint();
            if (warningVisual != null) warningVisual.SetActive(false);

            if (alwaysOn) SetLaser(true);
            else StartCoroutine(CycleRoutine());
        }

        private void OnValidate()
        {
            ApplyTint();
        }

        // ---------- Siklus ----------
        private IEnumerator CycleRoutine()
        {
            SetLaser(false);
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

            while (true)
            {
                yield return WarningRoutine();

                SetLaser(true);
                onLaserFire.Invoke();
                yield return new WaitForSeconds(onDuration);

                SetLaser(false);
                yield return new WaitForSeconds(offDuration);
            }
        }

        private IEnumerator WarningRoutine()
        {
            if (warningDuration <= 0f) yield break;

            onWarningStart.Invoke();

            // Tanpa visual aba-aba atau tanpa kedip: cukup tunggu
            if (warningVisual == null || !warningBlink)
            {
                if (warningVisual != null) warningVisual.SetActive(true);
                yield return new WaitForSeconds(warningDuration);
                if (warningVisual != null) warningVisual.SetActive(false);
                yield break;
            }

            float blinkTime = Mathf.Max(0f, warningDuration - solidBeforeFire);
            float t = 0f;
            bool on = true;

            while (t < blinkTime)
            {
                warningVisual.SetActive(on);

                // Makin dekat waktu menyala, makin cepat kedipnya
                float progress = t / blinkTime;
                float rate = Mathf.Lerp(blinkRateStart, blinkRateEnd, progress);
                float half = 0.5f / Mathf.Max(0.1f, rate);

                yield return new WaitForSeconds(half);
                t += half;
                on = !on;
            }

            // Menyala terang sebentar tepat sebelum laser keluar
            warningVisual.SetActive(true);
            if (solidBeforeFire > 0f) yield return new WaitForSeconds(solidBeforeFire);
            warningVisual.SetActive(false);
        }

        private void SetLaser(bool on)
        {
            IsActive = on;
            if (laserVisual != null) laserVisual.SetActive(on);
        }

        // ---------- Deteksi player ----------
        private void Update()
        {
            if (!IsActive) return;

            hazardCollider.OverlapCollider(filter, results);
            foreach (var col in results)
            {
                if (!col.CompareTag("Player")) continue;

                var death = col.GetComponentInParent<PlayerDeath>();
                if (death == null) continue;

                Vector2 source = hazardCollider.ClosestPoint(col.transform.position);
                bool killed = death.TryHit(source, dashCanPass, shieldCanPass, true);
                if (killed) onPlayerHit.Invoke();
                break;
            }
        }

        // ---------- Pewarnaan sesuai aturan ----------
        private Color RuleColor()
        {
            if (dashCanPass && shieldCanPass) return bothColor;
            if (dashCanPass) return dashOnlyColor;
            if (shieldCanPass) return shieldOnlyColor;
            return deadlyColor;
        }

        private void ApplyTint()
        {
            if (tintRenderers == null) return;

            Color c = RuleColor();
            foreach (var r in tintRenderers)
            {
                if (r == null) continue;
                c.a = r.color.a;
                r.color = c;
            }
        }

        private void OnDrawGizmos()
        {
            var col = GetComponent<Collider2D>();
            if (col == null) return;

            Color c = RuleColor();
            c.a = 0.9f;
            Gizmos.color = c;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}