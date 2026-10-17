using System.Collections;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    // Objek yang bisa kena laser (musuh, switch, dll) implementasikan interface ini.
    public interface IHittable
    {
        void OnLaserHit();
    }

    [RequireComponent(typeof(PlayerEnergy))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerSkills : MonoBehaviour
    {
        [Header("Input")]
        public int hitMouseButton = 0;          // klik kiri
        public int defMouseButton = 1;          // klik kanan (tahan)
        public KeyCode dashKey = KeyCode.Space;

        [Header("Hit (Laser) - Dasar")]
        public float hitCostPercent = 75f;      // % dari max energi
        public float laserRange = 12f;
        public float laserWidth = 0.8f;         // lebar hitbox sekaligus lebar maksimum visual
        public float laserDuration = 0.25f;
        public LayerMask laserHitMask;          // layer musuh/dinding (JANGAN centang layer Player)

        [Header("Hit (Laser) - Charge-up")]
        public float chargeTime = 0.2f;                  // jeda sebelum laser keluar
        public bool lockMovementWhileCharging = false;   // true = player berhenti saat charge
        public Transform chargeEffect;                   // opsional: sprite/partikel bola energi
        public float chargeEffectDistance = 0.8f;        // jarak bola dari pusat player, searah cursor
        public float chargeEffectMaxScale = 0.6f;        // ukuran bola saat charge penuh

        [Header("Hit (Laser) - Visual")]
        public LineRenderer laserLine;          // lapisan luar (glow), lebih tebal dan transparan
        public LineRenderer laserCore;          // opsional: inti putih tipis
        [Range(0.05f, 1f)] public float coreWidthRatio = 0.3f; // lebar inti dibanding lapisan luar
        [Tooltip("Sumbu X = progres laser (0..1), sumbu Y = pengali lebar. Default: melebar cepat, bertahan, lalu mengecil.")]
        public AnimationCurve laserWidthCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.12f, 1f),
            new Keyframe(0.6f, 1f),
            new Keyframe(1f, 0f));

        [Header("Def (Shield)")]
        public float shieldDrainPerSecond = 25f;
        public float shieldMinEnergyToStart = 10f;
        public Transform shieldPivot;           // pivot di tengah player, child "Shield" digeser keluar di sumbu X lokal

        [Header("Dash")]
        public float dashCostPercent = 33f;
        public float dashDistance = 4f;
        public float dashDuration = 0.12f;
        public float dashCooldown = 0.15f;

        public bool IsShielding { get; private set; }
        public bool IsDashing { get; private set; }
        public bool IsCharging { get; private set; }

        private PlayerEnergy energy;
        private Rigidbody2D rb;
        private TopDownCharacterController controller;
        private Camera cam;
        private float lastDashTime = -99f;
        private bool laserActive; // true selama charge DAN saat laser menyala

        private void Awake()
        {
            energy = GetComponent<PlayerEnergy>();
            rb = GetComponent<Rigidbody2D>();
            controller = GetComponent<TopDownCharacterController>();
            cam = Camera.main;
            SetShield(false);
            HideLaserVisuals();
            if (chargeEffect != null) chargeEffect.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (IsDashing) return;

            HandleShield();

            // Tidak boleh nembak/dash sambil menahan shield
            if (IsShielding) return;

            if (Input.GetMouseButtonDown(hitMouseButton)) TryHit();
            if (Input.GetKeyDown(dashKey) && !IsCharging) TryDash(); // dash tidak bisa saat charge, supaya energi tidak terbuang
        }

        // ---------- Util ----------
        private Vector2 AimDirection()
        {
            Vector3 m = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 d = (Vector2)m - (Vector2)transform.position;
            return d.sqrMagnitude < 0.0001f ? Vector2.down : d.normalized;
        }

        // ---------- HIT ----------
        private void TryHit()
        {
            if (laserActive) return;
            float cost = energy.maxEnergy * hitCostPercent / 100f;
            if (!energy.TryConsume(cost)) return; // energi dipotong di awal charge
            StartCoroutine(HitRoutine());
        }

        private IEnumerator HitRoutine()
        {
            laserActive = true;

            // ----- 1. Charge-up -----
            IsCharging = true;
            if (lockMovementWhileCharging && controller != null)
            {
                controller.movementLocked = true;
                rb.velocity = Vector2.zero;
            }

            if (chargeEffect != null) chargeEffect.gameObject.SetActive(true);

            float t = 0f;
            while (t < chargeTime)
            {
                t += Time.deltaTime;
                UpdateChargeEffect(Mathf.Clamp01(t / chargeTime));
                yield return null;
            }

            if (chargeEffect != null) chargeEffect.gameObject.SetActive(false);
            if (lockMovementWhileCharging && controller != null) controller.movementLocked = false;
            IsCharging = false;

            // ----- 2. Tembak (arah diambil saat laser keluar, bukan saat klik) -----
            Vector2 dir = AimDirection();
            Vector2 origin = transform.position;

            // Damage diberikan sekali saat laser keluar
            RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, laserWidth * 0.5f, dir, laserRange, laserHitMask);
            foreach (var h in hits)
            {
                var target = h.collider.GetComponentInParent<IHittable>();
                if (target != null) target.OnLaserHit();
            }

            // ----- 3. Animasi visual laser -----
            EnableLaserVisuals(true);
            float elapsed = 0f;
            while (elapsed < laserDuration)
            {
                float k = Mathf.Clamp01(elapsed / laserDuration);
                float w = laserWidth * laserWidthCurve.Evaluate(k);

                // Pangkal laser mengikuti player yang bergerak, arahnya tetap
                Vector2 o = transform.position;
                SetLaser(laserLine, o, o + dir * laserRange, w);
                SetLaser(laserCore, o, o + dir * laserRange, w * coreWidthRatio);

                elapsed += Time.deltaTime;
                yield return null;
            }

            HideLaserVisuals();
            laserActive = false;
        }

        private void UpdateChargeEffect(float k)
        {
            if (chargeEffect == null) return;
            // Bola energi di depan player (searah cursor) yang membesar selama charge
            chargeEffect.position = (Vector2)transform.position + AimDirection() * chargeEffectDistance;
            float eased = k * k; // pelan di awal, cepat di akhir
            chargeEffect.localScale = Vector3.one * (chargeEffectMaxScale * eased);
        }

        private void SetLaser(LineRenderer lr, Vector2 from, Vector2 to, float width)
        {
            if (lr == null) return;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.startWidth = lr.endWidth = width;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
        }

        private void EnableLaserVisuals(bool on)
        {
            if (laserLine != null) laserLine.enabled = on;
            if (laserCore != null) laserCore.enabled = on;
        }

        private void HideLaserVisuals() => EnableLaserVisuals(false);

        // ---------- DEF ----------
        private void HandleShield()
        {
            // Shield tidak bisa dinyalakan saat charge/menembak
            bool wantShield = !laserActive && Input.GetMouseButton(defMouseButton);

            if (wantShield && !IsShielding && energy.Has(shieldMinEnergyToStart))
                SetShield(true);

            if (IsShielding)
            {
                if (!wantShield)
                {
                    SetShield(false);
                    return;
                }

                // Arahkan shield ke cursor
                Vector2 aim = AimDirection();
                float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                shieldPivot.rotation = Quaternion.Euler(0f, 0f, angle);

                // Energi terkuras sesuai lama pemakaian
                if (!energy.Drain(shieldDrainPerSecond * Time.deltaTime))
                    SetShield(false);
            }
        }

        private void SetShield(bool on)
        {
            IsShielding = on;
            energy.regenPaused = on; // regen berhenti selama shield aktif
            if (shieldPivot != null) shieldPivot.gameObject.SetActive(on);
        }

        // Dipanggil PlayerDeath saat mati: hentikan semua skill yang sedang berjalan
        public void ResetState()
        {
            StopAllCoroutines();
            SetShield(false);
            HideLaserVisuals();
            if (chargeEffect != null) chargeEffect.gameObject.SetActive(false);
            laserActive = false;
            IsCharging = false;
            IsDashing = false;
            if (controller != null) controller.movementLocked = false;
        }

        // ---------- DASH ----------
        private void TryDash()
        {
            if (Time.time < lastDashTime + dashDuration + dashCooldown) return;
            float cost = energy.maxEnergy * dashCostPercent / 100f;
            if (!energy.TryConsume(cost)) return;
            StartCoroutine(DoDash(AimDirection()));
        }

        private IEnumerator DoDash(Vector2 dir)
        {
            IsDashing = true;
            lastDashTime = Time.time;
            if (controller != null) controller.movementLocked = true;

            rb.velocity = dir * (dashDistance / dashDuration);
            yield return new WaitForSeconds(dashDuration);

            rb.velocity = Vector2.zero;
            if (controller != null) controller.movementLocked = false;
            IsDashing = false;
        }
    }
}