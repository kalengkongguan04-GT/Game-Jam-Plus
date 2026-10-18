using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Cainos.PixelArtTopDown_Basic
{
    // Jebakan laser di dunia. Pasang di objek dengan Collider2D (dibuat Is Trigger otomatis,
    // jadi player tetap bisa berjalan menembusnya secara fisik, efeknya diatur di script ini).
    //
    // Player mati kalau menyentuh laser yang menyala, KECUALI:
    //   - Dash Can Pass    : player sedang dash (i-frame)
    //   - Shield Can Pass  : player sedang menahan shield (arah shield tidak berpengaruh)
    // Kedua opsi bisa diatur per jebakan, jadi tiap laser bisa menuntut cara melewati yang berbeda.
    [RequireComponent(typeof(Collider2D))]
    public class LaserHazard : MonoBehaviour
    {
        [Header("Cara melewati (pilih salah satu, keduanya, atau tidak sama sekali)")]
        public bool dashCanPass = true;
        public bool shieldCanPass = false;

        [Header("Siklus laser")]
        public bool alwaysOn = true;
        public float startDelay = 0f;          // (siklus) jeda awal, berguna supaya beberapa laser tidak sinkron
        public float onDuration = 2f;          // (siklus) lama laser menyala
        public float offDuration = 2f;         // (siklus) lama laser mati
        public float warningDuration = 0.6f;   // (siklus) peringatan sebelum menyala

        [Header("Visual")]
        public GameObject laserVisual;         // objek laser (sprite / LineRenderer), dinyalakan saat aktif
        public GameObject warningVisual;       // opsional: tampil saat peringatan sebelum menyala
        public SpriteRenderer[] tintRenderers; // opsional: diwarnai sesuai aturan lewat, supaya player tahu caranya
        public Color deadlyColor = new Color(1f, 0.15f, 0.15f);      // tidak bisa dilewati
        public Color dashOnlyColor = new Color(0.2f, 0.9f, 1f);      // hanya dash
        public Color shieldOnlyColor = new Color(1f, 0.9f, 0.2f);    // hanya shield
        public Color bothColor = new Color(0.4f, 1f, 0.4f);          // dash atau shield

        [Header("Opsional - aksi tambahan saat player kena")]
        public UnityEvent onPlayerHit;         // contoh: SFX laser / efek visual

        public bool IsActive { get; private set; }

        private Collider2D hazardCollider;
        private ContactFilter2D filter;
        private readonly List<Collider2D> results = new List<Collider2D>();

        private void Awake()
        {
            hazardCollider = GetComponent<Collider2D>();
            hazardCollider.isTrigger = true;
            filter = new ContactFilter2D { useTriggers = true }; // tanpa filter layer
        }

        private void Start()
        {
            ApplyTint();

            if (alwaysOn) SetLaser(true);
            else StartCoroutine(CycleRoutine());
        }

        private void OnValidate()
        {
            ApplyTint();
        }

        // ---------- Siklus nyala/mati ----------
        private IEnumerator CycleRoutine()
        {
            SetLaser(false);
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

            while (true)
            {
                if (warningDuration > 0f)
                {
                    if (warningVisual != null) warningVisual.SetActive(true);
                    yield return new WaitForSeconds(warningDuration);
                    if (warningVisual != null) warningVisual.SetActive(false);
                }

                SetLaser(true);
                yield return new WaitForSeconds(onDuration);

                SetLaser(false);
                yield return new WaitForSeconds(offDuration);
            }
        }

        private void SetLaser(bool on)
        {
            IsActive = on;
            if (laserVisual != null) laserVisual.SetActive(on);
        }

        // ---------- Deteksi player ----------
        // Dicek tiap frame (bukan lewat OnTriggerStay2D) supaya tetap bekerja walaupun player diam
        // di dalam laser dan Rigidbody-nya sudah "tidur", dan supaya player yang selesai dash
        // atau melepas shield di dalam laser langsung kena.
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
                c.a = r.color.a; // transparansi asli sprite dipertahankan
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