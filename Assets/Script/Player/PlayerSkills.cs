using System.Collections;
using System.Collections.Generic;
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
        [Tooltip("true = laser mengenai musuh (IHittable) di layer mana pun, jadi tidak terpengaruh perubahan layer saat game berjalan. false = hanya layer di Laser Hit Mask.")]
        public bool hitAllLayers = true;
        public bool debugLaser = false;         // true: tulis log + gambar kotak hitbox laser di Scene view

        [Header("Hit (Laser) - Charge-up")]
        public float chargeTime = 0.2f;                  // jeda sebelum laser keluar
        public bool lockMovementWhileCharging = false;   // true = player berhenti saat charge
        public Transform chargeEffect;                   // opsional: sprite/partikel bola energi
        public float chargeEffectOffset = 0f;            // geser bola ke depan dari titik keluar laser (0 = tepat di titik keluar)
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
        public Transform shieldPivot;           // pivot shield, akan dipindah ke ArrowPivot otomatis saat aktif
        public Transform shieldBody;            // opsional: objek sprite shield (child shieldPivot), digeser sesuai shieldDistance
        public float shieldDistance = 0.9f;     // jarak shield dari ArrowPivot (satuan lokal), lihat lingkaran cyan di Scene view
        public bool shieldAutoCenter = true;    // true = titik tengah GAMBAR shield dipaksa tepat di garis panah, apa pun pivot sprite-nya

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
        private AimIndicator aim; // sumber titik & arah bidik (sama dengan panah)
        private SkillSounds sounds; // opsional: sound effect skill
        private SpriteRenderer shieldRenderer;
        private float lastDashTime = -99f;
        private bool laserActive; // true selama charge DAN saat laser menyala

        private void Awake()
        {
            energy = GetComponent<PlayerEnergy>();
            rb = GetComponent<Rigidbody2D>();
            controller = GetComponent<TopDownCharacterController>();
            cam = Camera.main;
            aim = GetComponent<AimIndicator>();
            sounds = GetComponent<SkillSounds>();
            if (shieldBody != null) shieldRenderer = shieldBody.GetComponentInChildren<SpriteRenderer>(true); // true: objek shield awalnya nonaktif
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
        // Titik asal bidikan/laser: sama dengan ArrowPivot kalau ada AimIndicator
        private Vector2 AimOrigin => aim != null ? aim.Origin : (Vector2)transform.position;

        // Titik keluar laser (di panah)
        private Vector2 MuzzlePosition() => aim != null ? aim.GetMuzzlePosition() : AimOrigin;

        private Vector2 AimDirection()
        {
            if (aim != null) return aim.ComputeDirection();

            // Cadangan kalau AimIndicator tidak dipasang
            Vector3 sp = Input.mousePosition;
            sp.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
            Vector2 d = (Vector2)cam.ScreenToWorldPoint(sp) - (Vector2)transform.position;
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
            Vector2 origin = MuzzlePosition();
            Vector2 muzzleOffset = origin - (Vector2)transform.position; // supaya pangkal laser ikut player yang bergerak

            // Damage diberikan sekali saat laser keluar
            ApplyLaserDamage(origin, dir);
            if (sounds != null) sounds.PlayLaser(); // suara laser, tepat saat laser keluar

            // ----- 3. Animasi visual laser -----
            EnableLaserVisuals(true);
            float elapsed = 0f;
            while (elapsed < laserDuration)
            {
                float k = Mathf.Clamp01(elapsed / laserDuration);
                float w = laserWidth * laserWidthCurve.Evaluate(k);

                // Pangkal laser mengikuti player yang bergerak, arahnya tetap
                Vector2 o = (Vector2)transform.position + muzzleOffset;
                SetLaser(laserLine, o, o + dir * laserRange, w);
                SetLaser(laserCore, o, o + dir * laserRange, w * coreWidthRatio);

                elapsed += Time.deltaTime;
                yield return null;
            }

            HideLaserVisuals();
            laserActive = false;
        }

        // Hitbox laser = persegi panjang sepanjang laser. Mengenai SEMUA collider yang bersentuhan,
        // termasuk musuh yang menumpuk atau yang sudah menempel di titik keluar laser.
        private void ApplyLaserDamage(Vector2 origin, Vector2 dir)
        {
            Vector2 center = origin + dir * (laserRange * 0.5f);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            int mask = hitAllLayers ? -1 : laserHitMask.value; // -1 = semua layer
            Collider2D[] cols = Physics2D.OverlapBoxAll(center, new Vector2(laserRange, laserWidth), angle, mask);

            var alreadyHit = new HashSet<IHittable>(); // satu musuh cukup kena sekali walau punya banyak collider
            foreach (var col in cols)
            {
                var target = col.GetComponentInParent<IHittable>();
                if (target != null && alreadyHit.Add(target)) target.OnLaserHit();

                if (debugLaser)
                    Debug.Log($"[Laser] kena {col.name} (layer {LayerMask.LayerToName(col.gameObject.layer)}), IHittable? {target != null}");
            }

            if (debugLaser)
            {
                Debug.Log($"[Laser] collider terkena: {cols.Length}, musuh dihancurkan: {alreadyHit.Count}, mask = {laserHitMask.value}");
                // Daftar SEMUA collider di jalur laser (tanpa filter mask), supaya kelihatan siapa yang terlewat dan kenapa
                Collider2D[] all = Physics2D.OverlapBoxAll(center, new Vector2(laserRange, laserWidth), angle);
                foreach (var c in all)
                {
                    int layer = c.gameObject.layer;
                    bool inMask = ((1 << layer) & laserHitMask.value) != 0;
                    Debug.Log($"[Laser-debug] {c.name} | layer {layer} ({LayerMask.LayerToName(layer)}) | masuk mask? {inMask}");
                }

                Vector2 along = dir * (laserRange * 0.5f);
                Vector2 side = new Vector2(-dir.y, dir.x) * (laserWidth * 0.5f);
                Debug.DrawLine(center - along - side, center + along - side, Color.red, 1f);
                Debug.DrawLine(center + along - side, center + along + side, Color.red, 1f);
                Debug.DrawLine(center + along + side, center - along + side, Color.red, 1f);
                Debug.DrawLine(center - along + side, center - along - side, Color.red, 1f);
            }
        }

        private void UpdateChargeEffect(float k)
        {
            if (chargeEffect == null) return;
            // Bola energi di titik keluar laser (di panah), membesar selama charge
            chargeEffect.position = MuzzlePosition() + AimDirection() * chargeEffectOffset;
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

                // Shield berputar mengelilingi ArrowPivot, searah panah
                Vector2 aimDir = AimDirection();
                float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
                shieldPivot.position = AimOrigin;
                shieldPivot.rotation = Quaternion.Euler(0f, 0f, angle);
                PlaceShieldBody();

                // Energi terkuras sesuai lama pemakaian
                if (!energy.Drain(shieldDrainPerSecond * Time.deltaTime))
                    SetShield(false);
            }
        }

        // Letakkan shield di orbit panah. Kalau shieldAutoCenter aktif, titik tengah gambar
        // (bukan titik pivot sprite) yang ditaruh tepat di garis panah.
        private void PlaceShieldBody()
        {
            if (shieldBody == null) return;

            Vector3 target = new Vector3(shieldDistance, 0f, 0f);
            shieldBody.localPosition = target;

            if (!shieldAutoCenter || shieldRenderer == null || shieldRenderer.sprite == null) return;

            Vector3 worldCenter = shieldRenderer.transform.TransformPoint(shieldRenderer.sprite.bounds.center);
            Vector3 localOffset = shieldPivot.InverseTransformVector(worldCenter - shieldBody.position);
            shieldBody.localPosition = target - new Vector3(localOffset.x, localOffset.y, 0f);
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

        // Lingkaran jarak shield (cyan), muncul di Scene view saat Player dipilih
        private void OnDrawGizmosSelected()
        {
            if (shieldPivot == null) return;

            var a = aim != null ? aim : GetComponent<AimIndicator>();
            Vector2 origin = a != null ? a.Origin : (Vector2)transform.position;
            float worldRadius = shieldDistance * Mathf.Abs(shieldPivot.lossyScale.x);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, worldRadius);
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